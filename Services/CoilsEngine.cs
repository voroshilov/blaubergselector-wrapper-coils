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

        public static string TestDatabaseAccess()
        {
            if (!_initialized || _dll == null) return "DLL not initialized";

            var lines = new List<string>();
            try
            {
                // Use reflection to call GeometriesList with enum value 1 (calcHeating)
                var dllType = _dll.GetType();

                // Get the enum type from the method parameter
                var geoMethod = dllType.GetMethod("GeometriesList", new[] { typeof(int).Assembly.GetType("System.Int32") });
                if (geoMethod == null)
                {
                    // Find the overload that returns List<string>
                    var geoMethods = dllType.GetMethods().Where(m => m.Name == "GeometriesList").ToArray();
                    lines.Add($"GeometriesList overloads: {geoMethods.Length}");
                    foreach (var gm in geoMethods)
                    {
                        var ps = gm.GetParameters();
                        lines.Add($"  {gm.ReturnType.Name} ({string.Join(", ", ps.Select(p => p.ParameterType.FullName))})");

                        // Use the overload with 1 parameter (returns List<string>)
                        if (ps.Length == 1)
                        {
                            var enumType = ps[0].ParameterType;
                            var enumVal = Enum.ToObject(enumType, 1); // 1 = calcHeating
                            var result = gm.Invoke(_dll, new[] { enumVal });
                            var list = result as System.Collections.IList;
                            lines.Add($"  Geometries(Heating): count={list?.Count}, first={(list?.Count > 0 ? list[0] : "none")}");
                        }
                    }
                }

                // Test materials
                var materials = _dll.MaterialsList();
                lines.Add($"Materials: count={materials?.Count}, items=[{string.Join(", ", materials?.Take(5) ?? new List<string>())}]");

                // Test manifolds
                var manifolds = _dll.ManifoldsList();
                lines.Add($"Manifolds: count={manifolds?.Count}, first={manifolds?.FirstOrDefault()}");
            }
            catch (Exception ex)
            {
                lines.Add($"Exception: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    lines.Add($"Inner: {ex.InnerException.Message}");
            }

            return string.Join("\n", lines);
        }

        public static string ListGeometries()
        {
            if (!_initialized || _dll == null) return "DLL not initialized";
            try
            {
                var dllType = _dll.GetType();
                var geoMethods = dllType.GetMethods().Where(m => m.Name == "GeometriesList" && m.GetParameters().Length == 1).First();
                var enumType = geoMethods.GetParameters()[0].ParameterType;
                var enumVal = Enum.ToObject(enumType, 1); // calcHeating
                var result = geoMethods.Invoke(_dll, new[] { enumVal }) as List<string>;
                return string.Join("\n", result ?? new List<string>());
            }
            catch (Exception ex) { return $"Error: {ex.Message}"; }
        }

        public static string BruteForceTest()
        {
            if (!_initialized || _dll == null) return "DLL not initialized";

            var lines = new List<string>();

            // Check environment
            var rootPath = System.Configuration.ConfigurationManager.AppSettings["Coils.RootPath"];
            var hostPath = System.Configuration.ConfigurationManager.AppSettings["UnilabTheBest.Host.Path"];
            var autofac = System.Configuration.ConfigurationManager.AppSettings["Unilab.Autofac.Config"];
            lines.Add($"RootPath={rootPath}, exists={Directory.Exists(rootPath)}");
            lines.Add($"HostPath={hostPath}, exists={File.Exists(hostPath)}");
            lines.Add($"AutofacConfig={autofac}");
            lines.Add($"CurrentDir={Directory.GetCurrentDirectory()}");

            // Check if host exe exists in root path
            if (rootPath != null)
            {
                var hostInRoot = Path.Combine(rootPath, "UnilabTheBest_Host.exe");
                lines.Add($"HostInRoot={hostInRoot}, exists={File.Exists(hostInRoot)}");
                // List key files
                try
                {
                    var files = Directory.GetFiles(rootPath, "*.exe")
                        .Concat(Directory.GetFiles(rootPath, "*.db3"))
                        .Select(Path.GetFileName);
                    lines.Add($"RootFiles: {string.Join(", ", files)}");
                }
                catch (Exception ex) { lines.Add($"ListFiles error: {ex.Message}"); }
            }

            // Build a minimal valid input array (1-based, matching VB.NET)
            // Try both 0-based and 1-based with sizes 40, 50, 51
            var configs = new[] {
                new { Name = "50el_0based", Size = 50, Offset = 0 },
                new { Name = "50el_1based", Size = 50, Offset = 1 },
                new { Name = "51el_1based", Size = 51, Offset = 1 },
                new { Name = "40el_0based", Size = 40, Offset = 0 },
                new { Name = "40el_1based", Size = 40, Offset = 1 },
            };

            var method = _dll.GetType().GetMethod("CalculateFromArray",
                new[] { typeof(string[]), typeof(string[]).MakeByRefType() });

            foreach (var cfg in configs)
            {
                try
                {
                    var arr = new string[cfg.Size];
                    for (int i = 0; i < arr.Length; i++) arr[i] = "";
                    int o = cfg.Offset;

                    arr[0 + o] = "1";            // Calc Modality = Heating
                    arr[1 + o] = "1916 7mm";     // Geometry
                    arr[2 + o] = "Copper";        // Tube material
                    arr[3 + o] = "0.35";          // Tube thickness
                    arr[4 + o] = "Aluminum";      // Fin material
                    arr[5 + o] = "0.12";          // Fin thickness
                    arr[6 + o] = "1200";          // Coil length
                    arr[7 + o] = "800";           // Coil height
                    arr[8 + o] = "2.1";           // Fin pitch
                    arr[9 + o] = "4";             // Rows
                    arr[10 + o] = "20";           // Circuits
                    arr[11 + o] = "0";            // Skipped tubes
                    arr[12 + o] = "A";            // Inlet manifold
                    arr[13 + o] = "A";            // Outlet manifold
                    arr[14 + o] = "";             // Total capacity
                    arr[15 + o] = "10";           // Inlet air temp DB
                    arr[16 + o] = "";             // Inlet air temp WB
                    arr[17 + o] = "80";           // Inlet air RH
                    arr[18 + o] = "";             // Outlet air temp DB
                    arr[19 + o] = "8000";         // Air flow
                    arr[20 + o] = "0";            // Altitude
                    arr[21 + o] = "1";            // Fluid typology
                    arr[22 + o] = "WATER";        // Fluid name
                    arr[23 + o] = "E";            // Air density
                    if (29 + o < cfg.Size) arr[29 + o] = "80";   // Inlet fluid temp
                    if (30 + o < cfg.Size) arr[30 + o] = "70";   // Outlet fluid temp
                    if (31 + o < cfg.Size) arr[31 + o] = "";     // Fluid flow (empty)
                    if (32 + o < cfg.Size) arr[32 + o] = "E";    // Must be "E"

                    var args = new object[] { arr, null };
                    var res = (int)method.Invoke(_dll, args);
                    var output = (string[])args[1];

                    var hasData = false;
                    if (output != null)
                        for (int i = 0; i < output.Length; i++)
                            if (!string.IsNullOrEmpty(output[i])) { hasData = true; break; }

                    lines.Add($"{cfg.Name}: ret={res}, outNull={output == null}, outLen={output?.Length}, hasData={hasData}");
                    if (hasData)
                    {
                        lines.Add($"  OUTPUT: [{string.Join(", ", output.Take(20))}]");
                        break; // Found working config!
                    }
                }
                catch (Exception ex)
                {
                    lines.Add($"{cfg.Name}: EXCEPTION {ex.InnerException?.Message ?? ex.Message}");
                }
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
                // Call via reflection to eliminate any overload resolution issues
                var method = _dll.GetType().GetMethod("CalculateFromArray",
                    new[] { typeof(string[]), typeof(string[]).MakeByRefType() });
                diag += $", MethodFound={method != null}";

                var args = new object[] { input, null };
                var res = (int)method.Invoke(_dll, args);
                var output = (string[])args[1];

                diag += $", RetCode={res}, OutputNull={output == null}, OutputLength={output?.Length}";

                // Check warnings
                int hasWarnings = _dll.HasWarnings;
                diag += $", HasWarnings={hasWarnings}";
                if (hasWarnings > 0)
                    for (int i = 0; i < hasWarnings; i++)
                        diag += $", Warning[{i}]={_dll.GetWarning(i)}";

                return (res, output, diag);
            }
            catch (Exception ex)
            {
                diag += $", Exception={ex.GetType().Name}: {ex.Message}";
                if (ex.InnerException != null)
                    diag += $" | Inner: {ex.InnerException.Message}";
                return (-9998, null, diag);
            }
        }
    }
}