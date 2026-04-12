namespace blaubergselector_wrapper_coils.Models
{
    public class CalculateResponse
    {
        public int ReturnCode { get; set; }

        // Line 1
        public string TotalCapacity { get; set; }
        // Line 2
        public string SensibleCapacity { get; set; }
        // Line 3
        public string AirFlow { get; set; }
        // Line 4
        public string FrontalVelocity { get; set; }
        // Line 5
        public string InletAirTempDryBulb { get; set; }
        // Line 6
        public string OutletAirTempDryBulb { get; set; }
        // Line 7
        public string OutletAirTempWetBulb { get; set; }
        // Line 8
        public string OutletRelativeHumidity { get; set; }
        // Line 9
        public string AirPressureDrop { get; set; }

        // Line 10 - modality dependent
        public string FluidFlow { get; set; }
        // Line 11 - modality dependent
        public string FluidParam1 { get; set; }
        // Line 12 - modality dependent
        public string FluidParam2 { get; set; }
        // Line 13
        public string TotalPressureDropFluidSide { get; set; }

        // Line 14
        public string FluidVelocityLiquidPhase { get; set; }
        // Line 15
        public string FluidVelocityGasPhase { get; set; }
        // Line 16
        public string InletAirTempWetBulb { get; set; }
        // Line 17
        public string InletAirRelativeHumidity { get; set; }
        // Line 18
        public string InletManifold { get; set; }
        // Line 19
        public string OutletManifold { get; set; }

        /// <summary>
        /// Maps from the DLL's 1-based output array (doc lines 1-19 = arr[1]-arr[19]).
        /// Falls back to 0-based if array is small (in case DLL uses 0-based).
        /// </summary>
        public static CalculateResponse FromOutputArray(string[] output, int returnCode)
        {
            var resp = new CalculateResponse { ReturnCode = returnCode };

            if (output == null || output.Length == 0)
                return resp;

            // Detect whether DLL output is 0-based or 1-based:
            // if arr[0] is empty/null and arr.Length > 19, assume 1-based
            int offset = (output.Length > 19 && string.IsNullOrEmpty(output[0])) ? 1 : 0;

            resp.TotalCapacity = Get(output, offset + 0);
            resp.SensibleCapacity = Get(output, offset + 1);
            resp.AirFlow = Get(output, offset + 2);
            resp.FrontalVelocity = Get(output, offset + 3);
            resp.InletAirTempDryBulb = Get(output, offset + 4);
            resp.OutletAirTempDryBulb = Get(output, offset + 5);
            resp.OutletAirTempWetBulb = Get(output, offset + 6);
            resp.OutletRelativeHumidity = Get(output, offset + 7);
            resp.AirPressureDrop = Get(output, offset + 8);
            resp.FluidFlow = Get(output, offset + 9);
            resp.FluidParam1 = Get(output, offset + 10);
            resp.FluidParam2 = Get(output, offset + 11);
            resp.TotalPressureDropFluidSide = Get(output, offset + 12);
            resp.FluidVelocityLiquidPhase = Get(output, offset + 13);
            resp.FluidVelocityGasPhase = Get(output, offset + 14);
            resp.InletAirTempWetBulb = Get(output, offset + 15);
            resp.InletAirRelativeHumidity = Get(output, offset + 16);
            resp.InletManifold = Get(output, offset + 17);
            resp.OutletManifold = Get(output, offset + 18);

            return resp;
        }

        private static string Get(string[] arr, int index)
        {
            return index < arr.Length ? arr[index] : "";
        }
    }
}
