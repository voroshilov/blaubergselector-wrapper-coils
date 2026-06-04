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

        // Lines 20-41: extended fields returned by DLL (matches developer's reference output).
        // Names taken from the Desktop Application UI.

        // Line 20: m²
        public string ExchangerSurface { get; set; }
        // Line 21: l
        public string InternalVolume { get; set; }
        // Line 22
        public string GeometryEurovent { get; set; }
        // Line 23
        public string CalculationEurovent { get; set; }
        // Line 24: kPa
        public string PressureDropFluidSideOnlyCoil { get; set; }
        // Line 25: kPa
        public string PressureDropFluidSideInletManifold { get; set; }
        // Line 26: kPa
        public string PressureDropFluidSideOutletManifold { get; set; }
        // Line 27: kg
        public string TotalWeight { get; set; }
        // Line 28: kg
        public string TubesWeight { get; set; }
        // Line 29: kg
        public string FinsWeight { get; set; }
        // Line 30: kg
        public string ManifoldsWeight { get; set; }
        // Line 31: kg
        public string FrameWeight { get; set; }
        // Line 32: $
        public string TotalPrice { get; set; }
        // Line 33: $
        public string MaterialsCost { get; set; }
        // Line 34: mm
        public string OverallLength { get; set; }
        // Line 35: mm
        public string OverallHeight { get; set; }
        // Line 36: mm
        public string OverallDepth { get; set; }
        // Line 37
        public string NumberOfTubesPerRow { get; set; }
        // Line 38
        public string CoilModel { get; set; }
        // Line 39: kg/h
        public string QuantityOfProducedWater { get; set; }
        // Line 40: m³
        public string CoilInternalVolume { get; set; }
        // Line 41: Pa
        public string DryPressureDrop { get; set; }

        // Total length of the DLL output array — useful while we're still mapping fields.
        public int OutputArrayLength { get; set; }

        // Warnings collected from the DLL after the call (via HasWarnings + GetWarning).
        public string[] Warnings { get; set; }

        /// <summary>
        /// Maps from the DLL's output array (0-based: doc line 1 = arr[0]).
        /// </summary>
        public static CalculateResponse FromOutputArray(string[] output, int returnCode, string[] warnings = null)
        {
            var resp = new CalculateResponse { ReturnCode = returnCode, Warnings = warnings };

            if (output == null || output.Length == 0)
                return resp;

            resp.OutputArrayLength = output.Length;

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

            // Lines 20-23 are always present in both 38- and 41-field DLL builds.
            resp.ExchangerSurface = Get(output, 19);                    // line 20
            resp.InternalVolume = Get(output, 20);                      // line 21
            resp.GeometryEurovent = Get(output, 21);                    // line 22
            resp.CalculationEurovent = Get(output, 22);                 // line 23

            // Detect DLL build: 41-field build returns 3 manifold pressure drops at indices 23-25;
            // 38-field build skips them and the remaining fields shift left by 3.
            bool hasManifoldPressureDrops = output.Length >= 41;
            int shift = hasManifoldPressureDrops ? 0 : 3;

            if (hasManifoldPressureDrops)
            {
                resp.PressureDropFluidSideOnlyCoil = Get(output, 23);       // line 24
                resp.PressureDropFluidSideInletManifold = Get(output, 24);  // line 25
                resp.PressureDropFluidSideOutletManifold = Get(output, 25); // line 26
            }

            resp.TotalWeight = Get(output, 26 - shift);                 // line 27
            resp.TubesWeight = Get(output, 27 - shift);                 // line 28
            resp.FinsWeight = Get(output, 28 - shift);                  // line 29
            resp.ManifoldsWeight = Get(output, 29 - shift);             // line 30
            resp.FrameWeight = Get(output, 30 - shift);                 // line 31
            resp.TotalPrice = Get(output, 31 - shift);                  // line 32
            resp.MaterialsCost = Get(output, 32 - shift);               // line 33
            resp.OverallLength = Get(output, 33 - shift);               // line 34
            resp.OverallHeight = Get(output, 34 - shift);               // line 35
            resp.OverallDepth = Get(output, 35 - shift);                // line 36
            resp.NumberOfTubesPerRow = Get(output, 36 - shift);         // line 37
            resp.CoilModel = Get(output, 37 - shift);                   // line 38
            resp.QuantityOfProducedWater = Get(output, 38 - shift);     // line 39
            resp.CoilInternalVolume = Get(output, 39 - shift);          // line 40
            resp.DryPressureDrop = Get(output, 40 - shift);             // line 41

            return resp;
        }

        private static string Get(string[] arr, int index)
        {
            return index < arr.Length ? arr[index] : "";
        }
    }
}
