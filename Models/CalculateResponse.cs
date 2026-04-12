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
        /// Maps from the DLL's output array (0-based: doc line 1 = arr[0]).
        /// </summary>
        public static CalculateResponse FromOutputArray(string[] output, int returnCode)
        {
            var resp = new CalculateResponse { ReturnCode = returnCode };

            if (output == null || output.Length == 0)
                return resp;

            resp.TotalCapacity = Get(output, 0);             // line 1
            resp.SensibleCapacity = Get(output, 1);          // line 2
            resp.AirFlow = Get(output, 2);                   // line 3
            resp.FrontalVelocity = Get(output, 3);           // line 4
            resp.InletAirTempDryBulb = Get(output, 4);       // line 5
            resp.OutletAirTempDryBulb = Get(output, 5);      // line 6
            resp.OutletAirTempWetBulb = Get(output, 6);      // line 7
            resp.OutletRelativeHumidity = Get(output, 7);    // line 8
            resp.AirPressureDrop = Get(output, 8);           // line 9
            resp.FluidFlow = Get(output, 9);                 // line 10
            resp.FluidParam1 = Get(output, 10);              // line 11
            resp.FluidParam2 = Get(output, 11);              // line 12
            resp.TotalPressureDropFluidSide = Get(output, 12); // line 13
            resp.FluidVelocityLiquidPhase = Get(output, 13); // line 14
            resp.FluidVelocityGasPhase = Get(output, 14);    // line 15
            resp.InletAirTempWetBulb = Get(output, 15);      // line 16
            resp.InletAirRelativeHumidity = Get(output, 16); // line 17
            resp.InletManifold = Get(output, 17);            // line 18
            resp.OutletManifold = Get(output, 18);           // line 19

            return resp;
        }

        private static string Get(string[] arr, int index)
        {
            return index < arr.Length ? arr[index] : "";
        }
    }
}
