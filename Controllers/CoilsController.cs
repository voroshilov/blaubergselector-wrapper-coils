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

            string[] inputArray = request.ToInputArray();
            var (returnCode, output) = CoilsEngine.CalculateFromArray(inputArray);

            if (returnCode != 0)
            {
                return Content(
                    (System.Net.HttpStatusCode)422,
                    new { error = $"Calculation failed with code {returnCode}", returnCode });
            }

            var response = CalculateResponse.FromOutputArray(output, returnCode);
            return Ok(response);
        }

        // POST api/coils/calculate/raw
        [HttpPost]
        [Route("calculate/raw")]
        public IHttpActionResult CalculateRaw([FromBody] string[] input)
        {
            if (input == null || input.Length == 0)
                return BadRequest("Input array is null or empty");

            var (returnCode, output) = CoilsEngine.CalculateFromArray(input);

            return Ok(new { returnCode, output });
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

            string[] inputArray = request.ToInputArray();
            var (returnCode, output) = CoilsEngine.HeatRecoveryCalculateFromArray(inputArray);

            if (returnCode != 0)
            {
                return Content(
                    (System.Net.HttpStatusCode)422,
                    new { error = $"Heat recovery calculation failed with code {returnCode}", returnCode });
            }

            var response = HeatRecoveryResponse.FromOutputArray(output, returnCode);
            return Ok(response);
        }

        // POST api/coils/heat-recovery/raw
        [HttpPost]
        [Route("heat-recovery/raw")]
        public IHttpActionResult HeatRecoveryRaw([FromBody] string[] input)
        {
            if (input == null || input.Length == 0)
                return BadRequest("Input array is null or empty");

            var (returnCode, output) = CoilsEngine.HeatRecoveryCalculateFromArray(input);

            return Ok(new { returnCode, output });
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
    }
}
