using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SkyWalking.Sample.Frontend.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        // GET api/values
        [HttpGet]
        public async Task<IEnumerable<string>> Get()
        {
            await new HttpClient().GetAsync("http://localhost:5002/api/values");
            return new string[] { "value1", "value2" };
        } 
    }
}