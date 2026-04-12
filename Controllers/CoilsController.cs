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
        public IHttpActionResult Calculate([FromBody] string[] input)
        {
            if (input == null || input.Length == 0)
                return BadRequest("Input array is null or empty");

            var result = CoilsEngine.CalculateFromArray(input);
            return Ok(result);
        }
    }
}