using System;
using System.Collections.Generic;
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

        // Identity of the calculation engine, for diagnostics.
        //
        // Unilab stamps every assembly 1.0.0.0 (the compiler default — they never set
        // AssemblyVersion) and the DLL exposes no version API at all, so version numbers
        // are reported nowhere here: they carry no information. What actually identifies
        // a build is the fingerprint (size + last write time + SHA-256) of the engine
        // files, plus the license serial of the installation. Comparing those between
        // two hosts is the only reliable way to tell whether they calculate identically.
        //
        // The set covers the whole activated directory, not just the DLLs: results depend
        // just as much on the geometry/material database Init() is pointed at, which lives
        // under the same root.
        public static object DllVersionInfo()
        {
            return new
            {
                serial = Serial(),
                rootPath = _rootPath,
                // What is ACTUALLY loaded into the process. The CLR resolves managed
                // dependencies from the application's own folder, not from rootPath, so
                // these are normally the Copy Local copies in bin — updating the installed
                // engine under rootPath does NOT affect them until the project is rebuilt.
                // Compare these hashes against the matching entries in `files` to see
                // whether the running engine is the installed one.
                loadedAssemblies = LoadedUnilabAssemblies(),
                files = RootFiles()
            };
        }

        private static object[] LoadedUnilabAssemblies()
        {
            try
            {
                return AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(a => (a.GetName().Name ?? "").StartsWith("Unilab", StringComparison.OrdinalIgnoreCase))
                    .Select(a => a.Location)
                    .Where(location => !string.IsNullOrEmpty(location))
                    .OrderBy(location => location, StringComparer.OrdinalIgnoreCase)
                    // Full path, not a name relative to rootPath: where the file was loaded
                    // from is exactly the point here.
                    .Select(location => FileFingerprint(location, null))
                    .ToArray();
            }
            catch
            {
                return Array.Empty<object>();
            }
        }

        private static string Serial()
        {
            if (!_initialized)
                return null;

            try { return _dll.GetSerial(); }
            catch (Exception ex) { return "<error: " + ex.Message + ">"; }
        }

        // Fingerprints every file under the activated root, recursively. Capped so a
        // pathological directory cannot turn this endpoint into a full-disk hash.
        private const int MaxFingerprintedFiles = 500;

        private static object RootFiles()
        {
            if (string.IsNullOrEmpty(_rootPath) || !Directory.Exists(_rootPath))
                return new { truncated = false, items = Array.Empty<object>() };

            try
            {
                string[] paths = Directory.GetFiles(_rootPath, "*", SearchOption.AllDirectories);
                Array.Sort(paths, StringComparer.OrdinalIgnoreCase);

                bool truncated = paths.Length > MaxFingerprintedFiles;
                var items = paths
                    .Take(MaxFingerprintedFiles)
                    .Select(path => FileFingerprint(path, _rootPath))
                    .ToArray();

                return new { truncated, items };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message, truncated = false, items = Array.Empty<object>() };
            }
        }

        // Identity of a single file, named relative to basePath so nested database files
        // stay distinguishable.
        private static object FileFingerprint(string path, string basePath)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            long size = 0;
            string lastWriteUtc = null;
            try
            {
                var info = new FileInfo(path);
                size = info.Length;
                lastWriteUtc = info.LastWriteTimeUtc.ToString("o");
            }
            catch { /* unreadable */ }

            string name = path;
            if (!string.IsNullOrEmpty(basePath) && path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                name = path.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar);
            if (string.IsNullOrEmpty(name))
                name = Path.GetFileName(path);

            return new
            {
                name,
                size,
                lastWriteUtc,
                sha256 = Sha256(path, size, lastWriteUtc)
            };
        }

        // Short SHA-256 (first 16 hex chars) — enough to tell builds apart, short enough
        // to eyeball and paste into a bug report. Cached per (path, size, last write) so
        // repeat calls don't re-hash a multi-hundred-megabyte database every time.
        private static readonly Dictionary<string, string> _hashCache = new Dictionary<string, string>();

        private static string Sha256(string path, long size, string lastWriteUtc)
        {
            string key = path + "|" + size + "|" + lastWriteUtc;

            lock (_hashCache)
            {
                string cached;
                if (_hashCache.TryGetValue(key, out cached))
                    return cached;
            }

            string hash = ComputeSha256(path);

            lock (_hashCache)
            {
                _hashCache[key] = hash;
            }
            return hash;
        }

        private static string ComputeSha256(string path)
        {
            try
            {
                using (var sha = System.Security.Cryptography.SHA256.Create())
                using (var stream = File.OpenRead(path))
                {
                    byte[] hash = sha.ComputeHash(stream);
                    var sb = new System.Text.StringBuilder(16);
                    for (int i = 0; i < 8; i++)
                        sb.Append(hash[i].ToString("x2"));
                    return sb.ToString();
                }
            }
            catch
            {
                return null;
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

            // No assembly version here: Unilab ships everything as 1.0.0.0. See DllVersionInfo().
            var assemblyLocation = type.Assembly.Location;

            return new
            {
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
