using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using SkyApm.Agent.AspNet;

namespace SkyApm.Sample.AspNet.Controllers
{
    public class ValuesController : ApiController
    {
        public async Task<IHttpActionResult> Get()
        {
            var httpClient = new HttpClient(new HttpTracingHandler());
            var values = await httpClient.GetStringAsync("http://localhost:5001/api/values");
            return Json(values);
        }
    }
}