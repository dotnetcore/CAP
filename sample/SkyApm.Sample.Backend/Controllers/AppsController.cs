using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SkyApm.Sample.Backend.Models;

namespace SkyApm.Sample.Backend.Controllers
{
    [Route("api/[controller]")]
    public class AppsController: Controller
    {
        private readonly SampleDbContext _dbContext;

        public AppsController(SampleDbContext sampleDbContext)
        {
            _dbContext = sampleDbContext;
        }

        [HttpGet]
        public IEnumerable<Application> Get()
        {
            return _dbContext.Applications.ToList();
        }

        [HttpGet("{id}")]
        public Application Get(int id)
        {
            return _dbContext.Applications.Find(id);
        }

        [HttpPut]
        public void Put([FromBody]Application application)
        {
            _dbContext.Applications.Add(application);
            _dbContext.SaveChanges();
        }
    }
}