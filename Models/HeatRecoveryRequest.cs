namespace blaubergselector_wrapper_coils.Models
{
    public class HeatRecoveryRequest
    {
        // Pos 1: 1=Winter, 2=Summer
        public int CalculationMode { get; set; } = 1;

        public HeatRecoveryCoilInput SupplyCoil { get; set; } = new HeatRecoveryCoilInput();
        public HeatRecoveryCoilInput ExhaustCoil { get; set; } = new HeatRecoveryCoilInput();
        public HeatRecoveryFluidInput Fluid { get; set; } = new HeatRecoveryFluidInput();

        // Pos 52: altitude in m
        public string Altitude { get; set; } = "0";

        // Pos 53: air density - "S" / "N" / "E" / "O" / numeric
        public string AirDensity { get; set; } = "E";

        /// <summary>
        /// Maps to the DLL's input array: exactly 101 elements, 1-based indexing.
        /// arr[0] is unused. Doc position N = arr[N].
        /// Unilab confirmed the heat recovery input must be 101 elements long.
        /// </summary>
        public string[] ToInputArray()
        {
            var arr = new string[101];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = "";

            arr[1] = CalculationMode.ToString();

            // External (Supply) Coil: pos 2-21
            FillCoil(arr, SupplyCoil ?? new HeatRecoveryCoilInput(), baseIndex: 2);

            // Expelled (Exhaust) Coil: pos 22-41
            FillCoil(arr, ExhaustCoil ?? new HeatRecoveryCoilInput(), baseIndex: 22);

            var fluid = Fluid ?? new HeatRecoveryFluidInput();
            arr[42] = fluid.FluidName ?? "";
            // arr[43], arr[44] intentionally left empty per doc
            arr[45] = fluid.FluidFlow ?? "";
            arr[46] = fluid.PipeThickness ?? "";
            arr[47] = fluid.PipeDiameter ?? "";
            arr[48] = fluid.PipeLength ?? "";
            // arr[49] intentionally left empty per doc

            arr[52] = Altitude ?? "0";
            arr[53] = AirDensity ?? "E";

            return arr;
        }

        private static void FillCoil(string[] arr, HeatRecoveryCoilInput coil, int baseIndex)
        {
            arr[baseIndex + 0]  = coil.Geometry ?? "";              // pos 2 / 22
            arr[baseIndex + 1]  = coil.TubeMaterial ?? "";          // pos 3 / 23
            arr[baseIndex + 2]  = coil.TubeThickness ?? "";         // pos 4 / 24
            arr[baseIndex + 3]  = coil.FinMaterial ?? "";           // pos 5 / 25
            arr[baseIndex + 4]  = coil.FinThickness ?? "";          // pos 6 / 26
            arr[baseIndex + 5]  = coil.CoilLength ?? "";            // pos 7 / 27
            arr[baseIndex + 6]  = coil.CoilHeight ?? "";            // pos 8 / 28
            arr[baseIndex + 7]  = coil.FinPitch ?? "";              // pos 9 / 29
            arr[baseIndex + 8]  = coil.NumberOfRows ?? "";          // pos 10 / 30
            arr[baseIndex + 9]  = coil.NumberOfCircuits ?? "";      // pos 11 / 31
            arr[baseIndex + 10] = coil.NumberOfSkippedTubes ?? "0"; // pos 12 / 32
            arr[baseIndex + 11] = coil.InletManifold ?? "";         // pos 13 / 33
            arr[baseIndex + 12] = coil.OutletManifold ?? "";        // pos 14 / 34
            arr[baseIndex + 13] = coil.ManifoldMaterialId ?? "-1";  // pos 15 / 35
            arr[baseIndex + 14] = coil.ManifoldCouples ?? "";       // pos 16 / 36
            arr[baseIndex + 15] = coil.AirFlow ?? "";               // pos 17 / 37
            arr[baseIndex + 16] = coil.InletAirTempDryBulb ?? "";   // pos 18 / 38
            arr[baseIndex + 17] = coil.InletAirTempWetBulb ?? "";   // pos 19 / 39
            arr[baseIndex + 18] = coil.InletAirRelativeHumidity ?? ""; // pos 20 / 40
            arr[baseIndex + 19] = coil.FoulingFactorAirSide ?? "";  // pos 21 / 41
        }
    }

    public class HeatRecoveryCoilInput
    {
        public string Geometry { get; set; }
        public string TubeMaterial { get; set; }
        public string TubeThickness { get; set; }
        public string FinMaterial { get; set; }
        public string FinThickness { get; set; }
        public string CoilLength { get; set; }
        public string CoilHeight { get; set; }
        public string FinPitch { get; set; }
        public string NumberOfRows { get; set; }
        public string NumberOfCircuits { get; set; }
        public string NumberOfSkippedTubes { get; set; } = "0";
        public string InletManifold { get; set; } = "A";
        public string OutletManifold { get; set; } = "A";
        public string ManifoldMaterialId { get; set; } = "-1";
        public string ManifoldCouples { get; set; } = "";
        public string AirFlow { get; set; }
        public string InletAirTempDryBulb { get; set; }
        public string InletAirTempWetBulb { get; set; } = "";
        public string InletAirRelativeHumidity { get; set; } = "";
        public string FoulingFactorAirSide { get; set; } = "";
    }

    public class HeatRecoveryFluidInput
    {
        // Pos 42: fluid name (English), supports "ETHYLENE GLYCOL / WATER|30" syntax
        public string FluidName { get; set; }

        // Pos 45: fluid flow in m³/h (mandatory per doc)
        public string FluidFlow { get; set; }

        // Pos 46: pipe thickness in mm
        public string PipeThickness { get; set; } = "";

        // Pos 47: pipe diameter in mm
        public string PipeDiameter { get; set; } = "";

        // Pos 48: pipe length in mm
        public string PipeLength { get; set; } = "";
    }
}
