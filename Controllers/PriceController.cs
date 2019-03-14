using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Cobra.Controllers
{
    [Route("api/price")]
    [ApiController]
    public class PriceController : Controller
    {
        // GET api/price/user/stockSymbol
        [HttpGet("{user}/{stockSymbol}")]
        public JsonResult Get(string user, string stockSymbol)
        {
            (var amount, var cryptokey) = QuoteSrvHelper.GetQuote(user, stockSymbol);
            
            return Json( new {amount = amount, cryptokey = cryptokey});
        }
    }
}
