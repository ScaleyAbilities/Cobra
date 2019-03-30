using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Cobra
{
    public static class QuoteHelper
    {
        public class Quote
        {
            public decimal amount;
            public string timestamp = null;
            public string cryptokey = null;
        }

        // Quote server port number
        private const int quotePort = 4448;
        // Quote server url
        private static string quoteServer = "quoteserve.seng.uvic.ca";

        private static ConcurrentDictionary<string, Tuple<decimal, DateTime>> quoteCache = new ConcurrentDictionary<string, Tuple<decimal, DateTime>>();

        private static bool usingQuoteSrv = Environment.GetEnvironmentVariable("USING_QUOTE_SRV") == "TRUE" ? true : false;

        private static IPHostEntry ipHostInfo;
        private static IPAddress ipAddress;
        private static IPEndPoint remoteEndPoint;

        static QuoteHelper() {
            ipHostInfo = Dns.GetHostEntry(quoteServer);
            ipAddress = ipHostInfo.AddressList[0];
            remoteEndPoint = new IPEndPoint(ipAddress, quotePort);
        }

        public static async Task<Quote> GetQuote(string username, string stockSymbol) {
            if(!usingQuoteSrv)
                return new Quote() { amount = 100.00m };
            
            Tuple<decimal, DateTime> cachedQuote = null;
            quoteCache.TryGetValue(stockSymbol, out cachedQuote);
            
            if (cachedQuote == null) {
                Console.WriteLine($"!!! Quote cache miss: {stockSymbol}");
                var quote = await GetQuoteFromQuoteServer(username, stockSymbol);
                return quote;
            }

            if (cachedQuote.Item2.AddMinutes(1) <= DateTime.Now) {
                _ = GetQuoteFromQuoteServer(username, stockSymbol);
                quoteCache[stockSymbol] = new Tuple<decimal, DateTime>(quoteCache[stockSymbol].Item1, DateTime.Now);
            }

            Console.WriteLine($"Returned quote from cache: {stockSymbol}");

            return new Quote() {
                amount = cachedQuote.Item1
            };
        }
        
        private static async Task<Quote> GetQuoteFromQuoteServer(string username, string stockSymbol)
        {
            Quote quote;
            
            using (var skt = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                await skt.ConnectAsync(remoteEndPoint);

                var bytes = new byte[1024];
                var msg = Encoding.ASCII.GetBytes($"{stockSymbol},{username}\n");

                var bytesSent = await skt.SendAsync(msg, SocketFlags.None);
                var bytesRecv = await skt.ReceiveAsync(bytes, SocketFlags.None);

                var msgRecv = Encoding.UTF8.GetString(bytes).Replace("\0", string.Empty).Trim();
                var recv = msgRecv.Split(',');

                Console.WriteLine($"Quote Server Message: {msgRecv}");

                var amount = decimal.Parse(recv[0]);
                // var quoteStockSymbol = recv[1];
                // var quoteUserId = recv[2];
                var timestamp = recv[3];
                var cryptokey = recv[4];

                quote = new Quote() {
                    amount = amount,
                    timestamp = timestamp,
                    cryptokey = cryptokey
                };
            }

            quoteCache[stockSymbol] = new Tuple<decimal, DateTime>(quote.amount, DateTime.Now);

            // TODO: LOG QUOTE SHIT

            return quote;
        }
    }
}
