using System;
using Microsoft.AspNetCore.Mvc;

namespace SkyApm.Sample.Backend.Controllers
{
    [Route("api/[controller]")]
    public class ErrorsController :Controller
    {
        public string Get()
        {
            throw new InvalidOperationException("error sample.");
        }
    }
}