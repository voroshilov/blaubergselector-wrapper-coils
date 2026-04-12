using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using static Unilab.C8DllNet.Public.DllMain;
using Unilab.C8DllNet.Public;
using System.IO;

namespace blaubergselector_wrapper_coils.Services
{
    public class CoilsEngine
    {
        private static readonly object _lock = new object();
        private static DllMain _dll;
        private static bool _initialized;

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

                // Optional but often safer, in case DLL uses relative paths
                Directory.SetCurrentDirectory(rootPath);

                _dll = new DllMain();
                _dll.HideMessages = true;

                if (_dll == null)
                    throw new InvalidOperationException("_dll is null before ActivateForServer");

                if (!Directory.Exists(rootPath))
                    throw new Exception($"Root path does not exist: {rootPath}");

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

        public static string InspectDll()
        {
            if (_dll == null) return "DLL not created";

            var type = _dll.GetType();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.Contains("Calculat"))
                .ToList();

            var lines = new List<string>();
            lines.Add($"Type: {type.FullName}");
            lines.Add($"Assembly: {type.Assembly.FullName}");
            lines.Add($"Calculate* methods found: {methods.Count}");

            foreach (var m in methods)
            {
                var pars = m.GetParameters();
                var parStr = string.Join(", ", pars.Select(p =>
                    $"{(p.IsOut ? "out " : (p.ParameterType.IsByRef ? "ref " : ""))}" +
                    $"{(p.ParameterType.IsByRef ? p.ParameterType.GetElementType().Name : p.ParameterType.Name)} {p.Name}"));
                lines.Add($"  {m.ReturnType.Name} {m.Name}({parStr})");
            }

            // Also list all public methods
            var allMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            lines.Add($"All public methods ({allMethods.Length}):");
            foreach (var m in allMethods)
            {
                var pars = m.GetParameters();
                var parStr = string.Join(", ", pars.Select(p =>
                    $"{(p.IsOut ? "out " : (p.ParameterType.IsByRef ? "ref " : ""))}" +
                    $"{(p.ParameterType.IsByRef ? p.ParameterType.GetElementType().Name : p.ParameterType.Name)} {p.Name}"));
                lines.Add($"  {m.ReturnType.Name} {m.Name}({parStr})");
            }

            return string.Join("\n", lines);
        }

        public static (int returnCode, string[] output, string diagnostics) CalculateFromArray(string[] input)
        {
            if (!_initialized)
                return (-9999, null, "CoilsEngine is not initialized");

            string diag = $"InputLength={input?.Length}, Initialized={_dll.InitializationDone}";

            try
            {
                string[] output = null;
                int res = _dll.CalculateFromArray(input, ref output);
                diag += $", RetCode={res}, OutputNull={output == null}, OutputLength={output?.Length}";

                // Check for warnings
                int hasWarnings = _dll.HasWarnings;
                diag += $", HasWarnings={hasWarnings}";
                if (hasWarnings > 0)
                {
                    for (int i = 0; i < hasWarnings; i++)
                    {
                        var w = _dll.GetWarning(i);
                        diag += $", Warning[{i}]={w}";
                    }
                }

                return (res, output, diag);
            }
            catch (Exception ex)
            {
                diag += $", Exception={ex.GetType().Name}: {ex.Message}";
                return (-9998, null, diag);
            }
        }
    }
}