using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SkyApm.Sample.Backend.Controllers
{
    [Route("api/[controller]")]
    public class DelayController : Controller
    {
        // GET
        [HttpGet("{delay}")]
        public async Task<string> Get(int delay)
        {
            await Task.Delay(delay);
            return $"delay {delay}ms";
        }
    }
}