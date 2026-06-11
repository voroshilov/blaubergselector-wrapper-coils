using blaubergselector_wrapper_coils.Models;
using blaubergselector_wrapper_coils.Services;
using System.Web.Http;

namespace blaubergselector_wrapper_coils.Controllers
{
    [RoutePrefix("api/coils")]
    public class CoilsController : ApiController
    {
        // POST api/coils/calculate
        [HttpPost]
        [Route("calculate")]
        public IHttpActionResult Calculate([FromBody] CalculateRequest request)
        {
            if (request == null)
                return BadRequest("Request body is null");

            if (string.IsNullOrEmpty(request.Geometry))
                return BadRequest("Geometry is required");

            if (string.IsNullOrEmpty(request.InletAirTempDryBulb))
                return BadRequest("InletAirTempDryBulb is required");

            string materialError = NormalizeMaterials(request);
            if (materialError != null)
                return MaterialError(materialError);

            string[] inputArray = request.ToInputArray();
            var (returnCode, output, warnings) = CoilsEngine.CalculateFromArray(inputArray);

            if (returnCode != 0)
            {
                return Content(
                    (System.Net.HttpStatusCode)422,
                    new { error = $"Calculation failed with code {returnCode}", returnCode, warnings });
            }

            // The DLL reports some failures (unknown geometry, invalid parameter
            // combinations) as return code 0 with an EMPTY output array. Treat that
            // as an error instead of returning a 200 full of nulls.
            if (IsEmptyOutput(output))
            {
                return Content(
                    (System.Net.HttpStatusCode)422,
                    new
                    {
                        error = "Calculation produced no output (unknown geometry or invalid parameter combination)",
                        returnCode,
                        warnings
                    });
            }

            var response = CalculateResponse.FromOutputArray(output, returnCode, warnings);
            return Ok(response);
        }

        // POST api/coils/calculate/raw
        [HttpPost]
        [Route("calculate/raw")]
        public IHttpActionResult CalculateRaw([FromBody] string[] input)
        {
            if (input == null || input.Length == 0)
                return BadRequest("Input array is null or empty");

            var (returnCode, output, warnings) = CoilsEngine.CalculateFromArray(input);

            return Ok(new { returnCode, output, warnings });
        }

        // POST api/coils/heat-recovery
        [HttpPost]
        [Route("heat-recovery")]
        public IHttpActionResult HeatRecovery([FromBody] HeatRecoveryRequest request)
        {
            if (request == null)
                return BadRequest("Request body is null");

            if (request.SupplyCoil == null || string.IsNullOrEmpty(request.SupplyCoil.Geometry))
                return BadRequest("supply_coil.geometry is required");

            if (request.ExhaustCoil == null || string.IsNullOrEmpty(request.ExhaustCoil.Geometry))
                return BadRequest("exhaust_coil.geometry is required");

            if (string.IsNullOrEmpty(request.SupplyCoil.InletAirTempDryBulb))
                return BadRequest("supply_coil.inlet_air_temp_dry_bulb is required");

            if (string.IsNullOrEmpty(request.ExhaustCoil.InletAirTempDryBulb))
                return BadRequest("exhaust_coil.inlet_air_temp_dry_bulb is required");

            if (request.Fluid == null || string.IsNullOrEmpty(request.Fluid.FluidName))
                return BadRequest("fluid.fluid_name is required");

            if (string.IsNullOrEmpty(request.Fluid.FluidFlow))
                return BadRequest("fluid.fluid_flow is required");

            string materialError = NormalizeMaterials(request.SupplyCoil, "supply_coil")
                ?? NormalizeMaterials(request.ExhaustCoil, "exhaust_coil");
            if (materialError != null)
                return MaterialError(materialError);

            string[] inputArray = request.ToInputArray();
            var (returnCode, output, warnings) = CoilsEngine.HeatRecoveryCalculateFromArray(inputArray);

            if (returnCode != 0)
            {
                return Content(
                    (System.Net.HttpStatusCode)422,
                    new { error = $"Heat recovery calculation failed with code {returnCode}", returnCode, warnings });
            }

            // Same silent-failure mode as /calculate: code 0 with an empty output array.
            if (IsEmptyOutput(output))
            {
                return Content(
                    (System.Net.HttpStatusCode)422,
                    new
                    {
                        error = "Heat recovery calculation produced no output (unknown geometry or invalid parameter combination)",
                        returnCode,
                        warnings
                    });
            }

            var response = HeatRecoveryResponse.FromOutputArray(output, returnCode, warnings);
            return Ok(response);
        }

        // POST api/coils/heat-recovery/raw
        [HttpPost]
        [Route("heat-recovery/raw")]
        public IHttpActionResult HeatRecoveryRaw([FromBody] string[] input)
        {
            if (input == null || input.Length == 0)
                return BadRequest("Input array is null or empty");

            var (returnCode, output, warnings) = CoilsEngine.HeatRecoveryCalculateFromArray(input);

            return Ok(new { returnCode, output, warnings });
        }

        // POST api/coils/heat-recovery/fluid-flow
        [HttpPost]
        [Route("heat-recovery/fluid-flow")]
        public IHttpActionResult HeatRecoveryFluidFlow([FromBody] HeatRecoveryRequest request)
        {
            if (request == null)
                return BadRequest("Request body is null");

            string[] inputArray = request.ToInputArray();
            double fluidFlow = CoilsEngine.HeatRecoveryCalculateFluidFlow(inputArray);

            return Ok(new { fluidFlow });
        }

        // POST api/coils/heat-recovery/fluid-flow/raw
        [HttpPost]
        [Route("heat-recovery/fluid-flow/raw")]
        public IHttpActionResult HeatRecoveryFluidFlowRaw([FromBody] string[] input)
        {
            if (input == null || input.Length == 0)
                return BadRequest("Input array is null or empty");

            double fluidFlow = CoilsEngine.HeatRecoveryCalculateFluidFlow(input);

            return Ok(new { fluidFlow });
        }

        // GET api/coils/fluids?type=2
        // type: 1=PureLiquid, 2=MixtureLiquid, 3=PureGas, 4=MixtureGas, 5=Refrigerants
        [HttpGet]
        [Route("fluids")]
        public IHttpActionResult Fluids(int type = 2)
        {
            if (type < 1 || type > 5)
                return BadRequest("type must be between 1 and 5");

            var fluids = CoilsEngine.FluidsList(type);
            return Ok(fluids);
        }

        // GET api/coils/geometries?modality=1
        // modality: 1=Heating, 2=Cooling, 3=Condensing, 4=DirectExpansion, 5=Steam, 6=PumpEvaporator
        [HttpGet]
        [Route("geometries")]
        public IHttpActionResult Geometries(int modality = 1)
        {
            if (modality < 1 || modality > 6)
                return BadRequest("modality must be between 1 and 6");

            var geometries = CoilsEngine.GeometriesList(modality);
            return Ok(geometries);
        }

        // GET api/coils/materials
        // Tube/fin materials available in the DLL database (one shared list).
        [HttpGet]
        [Route("materials")]
        public IHttpActionResult Materials()
        {
            var materials = CoilsEngine.MaterialsList();
            return Ok(materials);
        }

        // Canonicalizes the request materials against the DLL list (the DLL silently
        // computes nothing on unknown/miscased materials). Returns an error message
        // when a non-empty material matches nothing, otherwise null.
        private static string NormalizeMaterials(CalculateRequest request)
        {
            string tube = CoilsEngine.NormalizeMaterial(request.TubeMaterial);
            if (tube == null)
                return "Unknown tube_material '" + request.TubeMaterial + "'";
            request.TubeMaterial = tube;

            string fin = CoilsEngine.NormalizeMaterial(request.FinMaterial);
            if (fin == null)
                return "Unknown fin_material '" + request.FinMaterial + "'";
            request.FinMaterial = fin;

            return null;
        }

        private static string NormalizeMaterials(HeatRecoveryCoilInput coil, string name)
        {
            string tube = CoilsEngine.NormalizeMaterial(coil.TubeMaterial);
            if (tube == null)
                return "Unknown " + name + ".tube_material '" + coil.TubeMaterial + "'";
            coil.TubeMaterial = tube;

            string fin = CoilsEngine.NormalizeMaterial(coil.FinMaterial);
            if (fin == null)
                return "Unknown " + name + ".fin_material '" + coil.FinMaterial + "'";
            coil.FinMaterial = fin;

            return null;
        }

        private IHttpActionResult MaterialError(string message)
        {
            return Content(
                (System.Net.HttpStatusCode)422,
                new { error = message, availableMaterials = CoilsEngine.MaterialsList() });
        }

        private static bool IsEmptyOutput(string[] output)
        {
            if (output == null || output.Length == 0)
                return true;
            foreach (string value in output)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return false;
            }
            return true;
        }

        // GET api/coils/inspect
        // Diagnostic: dump all public members of DllMain via reflection.
        [HttpGet]
        [Route("inspect")]
        public IHttpActionResult Inspect()
        {
            return Ok(CoilsEngine.InspectDll());
        }
    }
}
