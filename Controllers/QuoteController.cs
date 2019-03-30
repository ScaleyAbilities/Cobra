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
        public async Task<ActionResult<string>> Get(string user, string stockSymbol)
        {
            var quote = await QuoteHelper.GetQuote(user, stockSymbol);
            return Json(quote);
        }
    }
}
