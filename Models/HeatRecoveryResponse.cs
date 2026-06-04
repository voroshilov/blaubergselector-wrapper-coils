namespace blaubergselector_wrapper_coils.Models
{
    public class HeatRecoveryResponse
    {
        public int ReturnCode { get; set; }
        public string[] Warnings { get; set; }
        // null = DLL didn't allocate (output was null); 0 = empty array; N = populated
        public int? OutputArrayLength { get; set; }

        public HeatRecoverySummary Summary { get; set; }
        public HeatRecoveryCoilOutput SupplyCoil { get; set; }
        public HeatRecoveryCoilOutput ExhaustCoil { get; set; }

        /// <summary>
        /// Maps from the DLL's output array (0-based: doc line 1 = arr[0]).
        /// Doc layout: lines 1-18 = summary, 19-38 = supply, 39-58 = exhaust.
        /// </summary>
        public static HeatRecoveryResponse FromOutputArray(string[] output, int returnCode, string[] warnings = null)
        {
            var resp = new HeatRecoveryResponse
            {
                ReturnCode = returnCode,
                Warnings = warnings,
                Summary = new HeatRecoverySummary(),
                SupplyCoil = new HeatRecoveryCoilOutput(),
                ExhaustCoil = new HeatRecoveryCoilOutput()
            };

            resp.OutputArrayLength = output?.Length;

            if (output == null || output.Length == 0)
                return resp;

            // Summary: lines 1-17 (line 18 is "Empty" per doc)
            resp.Summary.TotalCapacity                  = Get(output, 0);  // line 1
            resp.Summary.Condensate                     = Get(output, 1);  // line 2
            resp.Summary.Efficiency                     = Get(output, 2);  // line 3
            resp.Summary.InletFluidTemperature          = Get(output, 3);  // line 4
            resp.Summary.OutletFluidTemperature         = Get(output, 4);  // line 5
            resp.Summary.ExternalOutletTempDryBulb      = Get(output, 5);  // line 6
            resp.Summary.ExternalOutletTempWetBulb      = Get(output, 6);  // line 7
            resp.Summary.ExternalOutletRelativeHumidity = Get(output, 7);  // line 8
            resp.Summary.ExternalPressureDrop           = Get(output, 8);  // line 9
            resp.Summary.ExternalCoilFaceVelocity       = Get(output, 9);  // line 10
            resp.Summary.ExpelledOutletTempDryBulb      = Get(output, 10); // line 11
            resp.Summary.ExpelledOutletTempWetBulb      = Get(output, 11); // line 12
            resp.Summary.ExpelledOutletRelativeHumidity = Get(output, 12); // line 13
            resp.Summary.ExpelledPressureDrop           = Get(output, 13); // line 14
            resp.Summary.ExpelledCoilFaceVelocity       = Get(output, 14); // line 15
            resp.Summary.PipePressureDrop               = Get(output, 15); // line 16
            resp.Summary.TotalFluidPressureDrop         = Get(output, 16); // line 17

            // Supply coil: lines 19-37 (line 38 is "Empty")
            FillCoilOutput(resp.SupplyCoil, output, baseIndex: 18);

            // Exhaust coil: lines 39-57 (line 58 is "Empty")
            FillCoilOutput(resp.ExhaustCoil, output, baseIndex: 38);

            return resp;
        }

        private static void FillCoilOutput(HeatRecoveryCoilOutput coil, string[] output, int baseIndex)
        {
            coil.TotalCapacity            = Get(output, baseIndex + 0);
            coil.SensibleCapacity         = Get(output, baseIndex + 1);
            coil.AirFlow                  = Get(output, baseIndex + 2);
            coil.FrontalVelocity          = Get(output, baseIndex + 3);
            coil.InletAirTempDryBulb      = Get(output, baseIndex + 4);
            coil.OutletAirTempDryBulb     = Get(output, baseIndex + 5);
            coil.OutletAirTempWetBulb     = Get(output, baseIndex + 6);
            coil.OutletRelativeHumidity   = Get(output, baseIndex + 7);
            coil.AirPressureDrop          = Get(output, baseIndex + 8);
            coil.FluidFlow                = Get(output, baseIndex + 9);
            coil.InletFluidTemperature    = Get(output, baseIndex + 10);
            coil.OutletFluidTemperature   = Get(output, baseIndex + 11);
            coil.PressureDropFluidSide    = Get(output, baseIndex + 12);
            coil.FluidVelocityLiquidPhase = Get(output, baseIndex + 13);
            coil.FluidVelocityGasPhase    = Get(output, baseIndex + 14);
            coil.InletAirTempWetBulb      = Get(output, baseIndex + 15);
            coil.InletAirRelativeHumidity = Get(output, baseIndex + 16);
            coil.InletManifold            = Get(output, baseIndex + 17);
            coil.OutletManifold           = Get(output, baseIndex + 18);
        }

        private static string Get(string[] arr, int index)
        {
            return index < arr.Length ? arr[index] : "";
        }
    }

    public class HeatRecoverySummary
    {
        public string TotalCapacity { get; set; }                  // kW
        public string Condensate { get; set; }                     // kg/h
        public string Efficiency { get; set; }                     // %
        public string InletFluidTemperature { get; set; }          // °C
        public string OutletFluidTemperature { get; set; }         // °C
        public string ExternalOutletTempDryBulb { get; set; }      // °C
        public string ExternalOutletTempWetBulb { get; set; }      // °C
        public string ExternalOutletRelativeHumidity { get; set; } // %
        public string ExternalPressureDrop { get; set; }           // Pa
        public string ExternalCoilFaceVelocity { get; set; }       // m/s
        public string ExpelledOutletTempDryBulb { get; set; }      // °C
        public string ExpelledOutletTempWetBulb { get; set; }      // °C
        public string ExpelledOutletRelativeHumidity { get; set; } // %
        public string ExpelledPressureDrop { get; set; }           // Pa
        public string ExpelledCoilFaceVelocity { get; set; }       // m/s
        public string PipePressureDrop { get; set; }               // kPa
        public string TotalFluidPressureDrop { get; set; }         // kPa
    }

    public class HeatRecoveryCoilOutput
    {
        public string TotalCapacity { get; set; }            // kW
        public string SensibleCapacity { get; set; }         // kW (when available)
        public string AirFlow { get; set; }                  // m³/h
        public string FrontalVelocity { get; set; }          // m/s
        public string InletAirTempDryBulb { get; set; }      // °C
        public string OutletAirTempDryBulb { get; set; }     // °C
        public string OutletAirTempWetBulb { get; set; }     // °C
        public string OutletRelativeHumidity { get; set; }   // %
        public string AirPressureDrop { get; set; }          // Pa
        public string FluidFlow { get; set; }                // m³/h
        public string InletFluidTemperature { get; set; }    // °C
        public string OutletFluidTemperature { get; set; }   // °C
        public string PressureDropFluidSide { get; set; }    // kPa
        public string FluidVelocityLiquidPhase { get; set; } // m/s
        public string FluidVelocityGasPhase { get; set; }    // m/s
        public string InletAirTempWetBulb { get; set; }      // °C
        public string InletAirRelativeHumidity { get; set; } // %
        public string InletManifold { get; set; }
        public string OutletManifold { get; set; }
    }
}
