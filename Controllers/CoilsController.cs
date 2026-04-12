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

        // POST api/coils/calculate/raw  — pass raw string[] directly (for debugging)
        [HttpPost]
        [Route("calculate/raw")]
        public IHttpActionResult CalculateRaw([FromBody] string[] input)
        {
            if (input == null || input.Length == 0)
                return BadRequest("Input array is null or empty");

            var (returnCode, output) = CoilsEngine.CalculateFromArray(input);

            return Ok(new { returnCode, output });
        }
    }
}
