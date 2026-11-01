using System;
using System.Threading;

namespace BVN.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            int port = 7777;
            if (args.Length > 0 && int.TryParse(args[0], out int customPort))
            {
                port = customPort;
            }

            Console.Title = $"Bleach vs Naruto Relay Server - Port {port}";
            Console.WriteLine("==================================================");
            Console.WriteLine("   BLEACH VS NARUTO - STANDALONE RELAY SERVER    ");
            Console.WriteLine("   Ho tro 10-20 CCU (5-10 cap solo 1v1 dong thoi)");
            Console.WriteLine("==================================================");

            using var server = new BVNRelayServer();
            server.Start(port);

            Console.WriteLine($"\nMay chu da san sang nhan ket noi tai port: {port}");
            Console.WriteLine("Nhan Ctrl+C de dung may chu.\n");

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            cts.Token.WaitHandle.WaitOne();

            Console.WriteLine("\nDang tat may chu...");
            server.Stop();
            Console.WriteLine("May chu da tat an toan.");
        }
    }
}
