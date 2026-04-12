namespace blaubergselector_wrapper_coils.Models
{
    public class CalculateRequest
    {
        // Pos 1: 1=Heating, 2=Cooling, 3=Condensing, 4=Direct Expansion, 5=Steam, 6=Pump Evaporator
        public int CalculationModality { get; set; }

        // Pos 2: geometry name from database (e.g. "072S12_C_D")
        public string Geometry { get; set; }

        // Pos 3: tube material in English (e.g. "Copper")
        public string TubeMaterial { get; set; }

        // Pos 4: tube thickness in mm
        public string TubeThickness { get; set; }

        // Pos 5: fin material in English (e.g. "Aluminum")
        public string FinMaterial { get; set; }

        // Pos 6: fin thickness in mm
        public string FinThickness { get; set; }

        // Pos 7: coil length in mm
        public string CoilLength { get; set; }

        // Pos 8: coil height in mm, or "TR:N" for N tubes per row
        public string CoilHeight { get; set; }

        // Pos 9: fin pitch in mm
        public string FinPitch { get; set; }

        // Pos 10: number of rows
        public string NumberOfRows { get; set; }

        // Pos 11: number of circuits
        public string NumberOfCircuits { get; set; }

        // Pos 12: number of skipped tubes
        public string NumberOfSkippedTubes { get; set; } = "0";

        // Pos 13: inlet manifold ("", "A", or specific e.g. "35x1")
        public string InletManifold { get; set; } = "A";

        // Pos 14: outlet manifold ("", "A", or specific e.g. "28x1")
        public string OutletManifold { get; set; } = "A";

        // Pos 15: total requested capacity in kW (optional, "0" or "" to auto-calculate)
        public string TotalCapacity { get; set; } = "";

        // Pos 16: inlet air temperature dry bulb in °C (required)
        public string InletAirTempDryBulb { get; set; }

        // Pos 17: inlet air temperature wet bulb in °C (optional)
        public string InletAirTempWetBulb { get; set; } = "";

        // Pos 18: inlet air relative humidity in % (optional)
        public string InletAirRelativeHumidity { get; set; } = "80";

        // Pos 19: outlet air temperature dry bulb in °C (optional)
        public string OutletAirTempDryBulb { get; set; } = "";

        // Pos 20: air flow in m³/h
        public string AirFlow { get; set; }

        // Pos 21: altitude in m
        public string Altitude { get; set; } = "0";

        // Pos 22: 1=Pure liquid, 2=Mixture liquid, 3=Pure gas, 4=Mixture gas, 5=Refrigerants
        public int FluidTypology { get; set; } = 1;

        // Pos 23: fluid or refrigerant name (e.g. "WATER", "R410A", "ETHYLENE GLYCOL / WATER|30")
        public string FluidName { get; set; }

        // Pos 24: air density - "S" (1.22), "N" (1.29), "E" (effective), "O" (outlet), or numeric
        public string AirDensity { get; set; } = "E";

        // Pos 25: fouling factor air side in m²K/W (optional)
        public string FoulingFactorAirSide { get; set; } = "";

        // Pos 26: fouling factor fluid side in m²K/W (optional)
        public string FoulingFactorFluidSide { get; set; } = "";

        // Pos 28: security factor in % (optional)
        public string SecurityFactor { get; set; } = "";

        // Pos 30-33: modality-dependent fluid parameters
        // Heating/Cooling: inlet fluid temp (°C), outlet fluid temp (°C), fluid flow (m³/h or kg/h), "E"
        // Condensing/DX: evaporating temp, condensing temp, overheating, subcooling
        // Steam: saturation pressure (bar A), saturation temp (°C), overheating, subcooling
        // Pump Evaporator: evaporating temp, recirculation ratio, overheating, subcooling
        public string FluidParam1 { get; set; } = "";
        public string FluidParam2 { get; set; } = "";
        public string FluidParam3 { get; set; } = "";
        public string FluidParam4 { get; set; } = "E";

        // Pos 37: manifold material ID (-1 to disable filter)
        public string ManifoldMaterialId { get; set; } = "-1";

        // Pos 38: number of manifold couples
        public string ManifoldCouples { get; set; } = "";

        /// <summary>
        /// Maps to the DLL's input array: exactly 50 elements, 1-based indexing.
        /// arr[0] is unused. Doc position N = arr[N].
        /// </summary>
        public string[] ToInputArray()
        {
            var arr = new string[50]; // exactly 50 elements (indices 0-49)
            for (int i = 0; i < arr.Length; i++)
                arr[i] = "";

            // arr[0] intentionally left empty (unused by DLL)
            arr[1]  = CalculationModality.ToString();  // pos 1
            arr[2]  = Geometry ?? "";                   // pos 2
            arr[3]  = TubeMaterial ?? "";               // pos 3
            arr[4]  = TubeThickness ?? "";              // pos 4
            arr[5]  = FinMaterial ?? "";                 // pos 5
            arr[6]  = FinThickness ?? "";               // pos 6
            arr[7]  = CoilLength ?? "";                  // pos 7
            arr[8]  = CoilHeight ?? "";                  // pos 8
            arr[9]  = FinPitch ?? "";                    // pos 9
            arr[10] = NumberOfRows ?? "";               // pos 10
            arr[11] = NumberOfCircuits ?? "";            // pos 11
            arr[12] = NumberOfSkippedTubes ?? "0";      // pos 12
            arr[13] = InletManifold ?? "";               // pos 13
            arr[14] = OutletManifold ?? "";              // pos 14
            arr[15] = TotalCapacity ?? "";               // pos 15
            arr[16] = InletAirTempDryBulb ?? "";        // pos 16
            arr[17] = InletAirTempWetBulb ?? "";        // pos 17
            arr[18] = InletAirRelativeHumidity ?? "";   // pos 18
            arr[19] = OutletAirTempDryBulb ?? "";       // pos 19
            arr[20] = AirFlow ?? "";                     // pos 20
            arr[21] = Altitude ?? "0";                   // pos 21
            arr[22] = FluidTypology.ToString();          // pos 22
            arr[23] = FluidName ?? "";                   // pos 23
            arr[24] = AirDensity ?? "E";                // pos 24
            arr[25] = FoulingFactorAirSide ?? "";       // pos 25
            arr[26] = FoulingFactorFluidSide ?? "";     // pos 26
            arr[28] = SecurityFactor ?? "";              // pos 28
            arr[30] = FluidParam1 ?? "";                // pos 30
            arr[31] = FluidParam2 ?? "";                // pos 31
            arr[32] = FluidParam3 ?? "";                // pos 32
            arr[33] = FluidParam4 ?? "E";               // pos 33
            arr[37] = ManifoldMaterialId ?? "-1";       // pos 37
            arr[38] = ManifoldCouples ?? "";             // pos 38

            return arr;
        }
    }
}
