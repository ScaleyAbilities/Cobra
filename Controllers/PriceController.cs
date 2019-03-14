using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Cobra.Controllers
{
    [Route("api/price")]
    [ApiController]
    public class PriceController : ControllerBase
    {
        // GET api/price/stockSymbol
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            // TODO - return stock price
            return "value";
        }
    }
}
