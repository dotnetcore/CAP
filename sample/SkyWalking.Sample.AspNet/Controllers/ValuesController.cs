using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using SkyWalking.AspNet;

namespace SkyWalking.Sample.AspNet.Controllers
{
    public class ValuesController : ApiController
    {
        public async Task<IHttpActionResult> Get()
        {
//            var httpClient = new HttpClient(new HttpTracingHandler());
//            var values = await httpClient.GetStringAsync("http://localhost:5002/api/values");

            var values = 1;
            return Json(values);
        }
    }
}