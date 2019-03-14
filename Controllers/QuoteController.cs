using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Cobra.Controllers
{
    [Route("quote")]
    [ApiController]
    public class QuoteController : Controller
    {
        // GET quote/user/stockSymbol
        [HttpGet("{user}/{stockSymbol}")]
        public ActionResult<string> Get(string user, string stockSymbol)
        {
            (var amount, var cryptokey) = QuoteSrvHelper.GetQuote(user, stockSymbol);
            
            return Json( new {amount = amount, cryptokey = cryptokey});
        }
    }
}
