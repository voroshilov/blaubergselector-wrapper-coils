using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Unilab.C8DllNet.Public;

namespace blaubergselector_wrapper_coils.Services
{
    public class CoilsEngine
    {
        private static readonly object _lock = new object();
        private static DllMain _dll;
        private static bool _initialized;
        private static string _rootPath;
        private static string _dllVersion;

        public static void Init(string rootPath)
        {
            lock (_lock)
            {
                if (_initialized)
                    return;

                if (string.IsNullOrWhiteSpace(rootPath))
                    throw new Exception("rootPath is null or empty");
                if (!Directory.Exists(rootPath))
                    throw new Exception($"Root directory does not exist: {rootPath}");

                Directory.SetCurrentDirectory(rootPath);

                _dll = new DllMain();
                _dll.HideMessages = true;

                var ukeys = Directory.GetFiles(rootPath, "UnilabC8Dll_*.uKeySrv");
                var hasIdentity = File.Exists(Path.Combine(rootPath, "Identity.ini"));

                if (ukeys.Length != 2 || !hasIdentity)
                {
                    throw new Exception(
                        $"Activation files not found correctly. Path = '{rootPath}', " +
                        $"uKeySrv files = {ukeys.Length}, Identity.ini exists = {hasIdentity}");
                }

                string activationMessage = string.Empty;

                try
                {
                    bool activated = _dll.ActivateForServer(rootPath, ref activationMessage);
                    if (!activated)
                        throw new Exception($"ActivateForServer returned false. Message: {activationMessage}");
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        $"ActivateForServer threw exception. rootPath = '{rootPath}', message = '{activationMessage}'",
                        ex);
                }

                int initRes = _dll.Init(
                    rootPath,
                    rootPath,
                    DllMain.EC6Languages.lngEnglish
                );

                if (initRes != 0 && initRes != -1)
                    throw new Exception($"Init failed with code {initRes}");

                _rootPath = rootPath;
                _initialized = true;
            }
        }

        public static void Release()
        {
            lock (_lock)
            {
                if (!_initialized || _dll == null)
                    return;

                _dll.DllRelease();
                _initialized = false;
                _dll = null;
            }
        }

        // Version of the Unilab calculation library that served the request. Resolved once
        // from the loaded Unilab.C8DllNet.Public assembly: the file version (what Unilab
        // actually ships/increments) with the assembly version as a fallback.
        public static string DllVersion
        {
            get { return _dllVersion ?? (_dllVersion = ResolveDllVersion()); }
        }

        private static string ResolveDllVersion()
        {
            var assembly = typeof(DllMain).Assembly;
            string version = assembly.GetName().Version?.ToString();

            try
            {
                string location = assembly.Location;
                if (!string.IsNullOrEmpty(location) && File.Exists(location))
                {
                    string fileVersion = FileVersionInfo.GetVersionInfo(location).FileVersion;
                    if (!string.IsNullOrWhiteSpace(fileVersion))
                        version = fileVersion.Trim();
                }
            }
            catch
            {
                // Fall back to the assembly version.
            }

            return version;
        }

        // Detailed version info for diagnostics: the managed wrapper plus every Unilab
        // native/engine DLL sitting in the activated root directory.
        public static object DllVersionInfo()
        {
            var assembly = typeof(DllMain).Assembly;
            string location = assembly.Location;

            string fileVersion = null;
            string productVersion = null;
            try
            {
                if (!string.IsNullOrEmpty(location) && File.Exists(location))
                {
                    var info = FileVersionInfo.GetVersionInfo(location);
                    fileVersion = info.FileVersion?.Trim();
                    productVersion = info.ProductVersion?.Trim();
                }
            }
            catch
            {
                // Leave the file/product versions null.
            }

            return new
            {
                version = DllVersion,
                assemblyName = assembly.GetName().Name,
                assemblyVersion = assembly.GetName().Version?.ToString(),
                fileVersion,
                productVersion,
                location,
                rootPath = _rootPath,
                libraries = LibraryVersions()
            };
        }

        private static object[] LibraryVersions()
        {
            if (string.IsNullOrEmpty(_rootPath) || !Directory.Exists(_rootPath))
                return Array.Empty<object>();

            try
            {
                return Directory
                    .GetFiles(_rootPath, "Unilab*.dll")
                    .Select(path =>
                    {
                        string version = null;
                        try { version = FileVersionInfo.GetVersionInfo(path).FileVersion?.Trim(); }
                        catch { /* unreadable */ }
                        return (object)new { name = Path.GetFileName(path), fileVersion = version };
                    })
                    .ToArray();
            }
            catch
            {
                return Array.Empty<object>();
            }
        }

        public static (int returnCode, string[] output, string[] warnings) CalculateFromArray(string[] input)
        {
            if (!_initialized)
                throw new InvalidOperationException("CoilsEngine is not initialized");

            string[] output = null;
            int res = _dll.CalculateFromArray(input, ref output);

            return (res, output, CollectWarnings());
        }

        public static (int returnCode, string[] output, string[] warnings) HeatRecoveryCalculateFromArray(string[] input)
        {
            if (!_initialized)
                throw new InvalidOperationException("CoilsEngine is not initialized");

            // Unilab confirmed: the output array must NOT be pre-allocated (no length set).
            // The DLL allocates and fills it itself, provided the input array is 101 elements long.
            string[] output = null;
            int res = _dll.HeatRecovery_CalculateFromArray(input, ref output);

            return (res, output, CollectWarnings());
        }

        private static string[] CollectWarnings()
        {
            int count = _dll.HasWarnings;
            if (count <= 0)
                return Array.Empty<string>();

            var warnings = new string[count];
            for (int i = 0; i < count; i++)
            {
                try { warnings[i] = _dll.GetWarning(i); }
                catch (Exception ex) { warnings[i] = "<error: " + ex.Message + ">"; }
            }
            return warnings;
        }

        public static double HeatRecoveryCalculateFluidFlow(string[] input)
        {
            if (!_initialized)
                throw new InvalidOperationException("CoilsEngine is not initialized");

            return _dll.HeatRecovery_CalculateFluidFlow(input);
        }

        public static object InspectDll()
        {
            if (!_initialized)
                throw new InvalidOperationException("CoilsEngine is not initialized");

            var type = _dll.GetType();

            var properties = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p =>
                {
                    string value = "<unreadable>";
                    try { value = p.GetValue(_dll)?.ToString() ?? "null"; }
                    catch (Exception ex) { value = "<error: " + ex.Message + ">"; }
                    return new { name = p.Name, type = p.PropertyType.Name, value };
                })
                .OrderBy(p => p.name)
                .ToArray();

            var fields = type
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Select(f =>
                {
                    string value = "<unreadable>";
                    try { value = f.GetValue(_dll)?.ToString() ?? "null"; }
                    catch (Exception ex) { value = "<error: " + ex.Message + ">"; }
                    return new { name = f.Name, type = f.FieldType.Name, value };
                })
                .OrderBy(f => f.name)
                .ToArray();

            var methods = type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(m => new
                {
                    name = m.Name,
                    returnType = m.ReturnType.Name,
                    parameters = m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name).ToArray()
                })
                .OrderBy(m => m.name)
                .ToArray();

            var assemblyVersion = type.Assembly.GetName().Version?.ToString();
            var assemblyLocation = type.Assembly.Location;

            return new
            {
                assemblyVersion,
                assemblyLocation,
                properties,
                fields,
                methods
            };
        }

        public static List<string> FluidsList(int fluidType)
        {
            if (!_initialized)
                throw new InvalidOperationException("CoilsEngine is not initialized");

            return _dll.FluidsList((DllMain.EC6FluidTypes)fluidType);
        }

        public static List<string> GeometriesList(int modality)
        {
            if (!_initialized)
                throw new InvalidOperationException("CoilsEngine is not initialized");

            return _dll.GeometriesList((DllMain.EC6CalcModalities)modality);
        }

        // Manifold descriptions (e.g. "28x1", "35x1") available in the DLL database.
        // These are the valid values for InletManifold/OutletManifold, in addition to
        // the two special values "A" (automatic selection) and "" (no manifold).
        public static List<string> ManifoldsList()
        {
            if (!_initialized)
                throw new InvalidOperationException("CoilsEngine is not initialized");

            if (_manifolds == null)
                _manifolds = _dll.ManifoldsList();
            return _manifolds;
        }

        private static List<string> _manifolds;

        // Canonicalizes a manifold description against the DLL's manifolds list. A bad
        // manifold string makes the DLL either return an empty output (silent failure)
        // or throw a NullReferenceException internally, so we must resolve it up front.
        // The special values "" (no manifold) and "A"/"a" (automatic) pass through.
        // Otherwise we match against the DB entries, accepting both the full canonical
        // form ("28x1 [1 1/8\"]") and the short prefix before " [" ("28x1"), and return
        // the canonical form. Non-empty input with no match returns null.
        public static string NormalizeManifold(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            string trimmed = value.Trim();
            if (string.Equals(trimmed, "A", StringComparison.OrdinalIgnoreCase))
                return "A";

            foreach (string manifold in ManifoldsList())
            {
                if (string.Equals(manifold, trimmed, StringComparison.OrdinalIgnoreCase))
                    return manifold;

                int bracket = manifold.IndexOf('[');
                string prefix = (bracket >= 0 ? manifold.Substring(0, bracket) : manifold).Trim();
                if (string.Equals(prefix, trimmed, StringComparison.OrdinalIgnoreCase))
                    return manifold;
            }
            return null;
        }

        // Single shared list: the DLL does not distinguish tube vs fin materials.
        public static List<string> MaterialsList()
        {
            if (!_initialized)
                throw new InvalidOperationException("CoilsEngine is not initialized");

            if (_materials == null)
                _materials = _dll.MaterialsList();
            return _materials;
        }

        private static List<string> _materials;

        // Canonicalizes a material name against the DLL's own materials list. The DLL is
        // case/spelling sensitive and silently computes nothing on unknown materials, while
        // clients (e.g. Akeneo option codes) send lowercase_with_underscores. Comparison
        // folds both sides to lowercase alphanumerics, so "copper", "stainless_steel" and
        // "Cupro - Nichel" all match regardless of case, underscores, spaces or dashes.
        // Empty input is returned as-is; non-empty input with no match returns null.
        public static string NormalizeMaterial(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            string folded = Fold(value);
            foreach (string material in MaterialsList())
            {
                if (Fold(material) == folded)
                    return material;
            }
            return null;
        }

        private static string Fold(string value)
        {
            var sb = new System.Text.StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }
    }
}
