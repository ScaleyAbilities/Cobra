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
        private static ConcurrentDictionary<string, Task<Quote>> requests = new ConcurrentDictionary<string, Task<Quote>>();

        private static bool usingQuoteSrv = Environment.GetEnvironmentVariable("USING_QUOTE_SRV") == "TRUE" ? true : false;

        private static IPHostEntry ipHostInfo;
        private static IPAddress ipAddress;
        private static IPEndPoint remoteEndPoint;

        static QuoteHelper() {
            ipHostInfo = Dns.GetHostEntry(quoteServer);
            ipAddress = ipHostInfo.AddressList[0];
            remoteEndPoint = new IPEndPoint(ipAddress, quotePort);
        }

        public static async Task<Quote> GetQuote(string username, string stockSymbol, string transactionId) {
            if(!usingQuoteSrv)
                return new Quote() { amount = 100.00m };
            
            Tuple<decimal, DateTime> cachedQuote = null;
            quoteCache.TryGetValue(stockSymbol, out cachedQuote);
            
            if (cachedQuote == null || cachedQuote.Item2.AddMinutes(1) <= DateTime.Now) {
                Console.WriteLine($"!!! Quote cache miss: {stockSymbol}");
                Task<Quote> request;
                requests.TryGetValue(stockSymbol, out request);

                if (request == null) {
                    request = requests[stockSymbol] = GetQuoteFromQuoteServer(username, stockSymbol, transactionId);
                }
                    
                var quote = await request;
                requests.TryRemove(stockSymbol, out _);
                return quote;
            }

            Console.WriteLine($"Returned quote from cache: {stockSymbol}");

            return new Quote() {
                amount = cachedQuote.Item1
            };
        }
        
        private static async Task<Quote> GetQuoteFromQuoteServer(string username, string stockSymbol, string transactionId)
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
                var quoteStockSymbol = recv[1];
                var quoteUserId = recv[2];
                var timestamp = recv[3];
                var cryptokey = recv[4];

                LogQuoteServer(transactionId, amount, quoteStockSymbol, quoteUserId, timestamp, cryptokey);

                quote = new Quote() {
                    amount = amount,
                    timestamp = timestamp,
                    cryptokey = cryptokey
                };
            }

            quoteCache[stockSymbol] = new Tuple<decimal, DateTime>(quote.amount, DateTime.Now);

            return quote;
        }

        private static void LogQuoteServer(string transactionId, decimal? amount, string quoteStockSymbol, string quoteUserId, string quoteServerTime, string cryptoKey)
        {
            string logCommand = $"Cobra,{transactionId}{Environment.NewLine}q,{amount},{quoteStockSymbol},{quoteUserId},{quoteServerTime},{cryptoKey}";
            RabbitHelper.PushLogEntry(logCommand);
        }
    }
}
