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
        /// Maps to the DLL's input array. Doc uses 1-based position labels,
        /// but the .NET array is 0-based: doc pos N = arr[N-1].
        /// </summary>
        public string[] ToInputArray()
        {
            var arr = new string[50]; // exactly 50 elements, indices 0-49
            for (int i = 0; i < arr.Length; i++)
                arr[i] = "";

            arr[0]  = CalculationModality.ToString();  // doc pos 1
            arr[1]  = Geometry ?? "";                   // doc pos 2
            arr[2]  = TubeMaterial ?? "";               // doc pos 3
            arr[3]  = TubeThickness ?? "";              // doc pos 4
            arr[4]  = FinMaterial ?? "";                 // doc pos 5
            arr[5]  = FinThickness ?? "";               // doc pos 6
            arr[6]  = CoilLength ?? "";                  // doc pos 7
            arr[7]  = CoilHeight ?? "";                  // doc pos 8
            arr[8]  = FinPitch ?? "";                    // doc pos 9
            arr[9]  = NumberOfRows ?? "";               // doc pos 10
            arr[10] = NumberOfCircuits ?? "";            // doc pos 11
            arr[11] = NumberOfSkippedTubes ?? "0";      // doc pos 12
            arr[12] = InletManifold ?? "";               // doc pos 13
            arr[13] = OutletManifold ?? "";              // doc pos 14
            arr[14] = TotalCapacity ?? "";               // doc pos 15
            arr[15] = InletAirTempDryBulb ?? "";        // doc pos 16
            arr[16] = InletAirTempWetBulb ?? "";        // doc pos 17
            arr[17] = InletAirRelativeHumidity ?? "";   // doc pos 18
            arr[18] = OutletAirTempDryBulb ?? "";       // doc pos 19
            arr[19] = AirFlow ?? "";                     // doc pos 20
            arr[20] = Altitude ?? "0";                   // doc pos 21
            arr[21] = FluidTypology.ToString();          // doc pos 22
            arr[22] = FluidName ?? "";                   // doc pos 23
            arr[23] = AirDensity ?? "E";                // doc pos 24
            arr[24] = FoulingFactorAirSide ?? "";       // doc pos 25
            arr[25] = FoulingFactorFluidSide ?? "";     // doc pos 26
            arr[27] = SecurityFactor ?? "";              // doc pos 28
            arr[29] = FluidParam1 ?? "";                // doc pos 30
            arr[30] = FluidParam2 ?? "";                // doc pos 31
            arr[31] = FluidParam3 ?? "";                // doc pos 32
            arr[32] = FluidParam4 ?? "E";               // doc pos 33
            arr[36] = ManifoldMaterialId ?? "-1";       // doc pos 37
            arr[37] = ManifoldCouples ?? "";             // doc pos 38

            return arr;
        }
    }
}
