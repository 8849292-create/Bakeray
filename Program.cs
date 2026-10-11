using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Bakeray
{
    internal class Program
    {
        private const int TotalBunsToBake = 60;
        private static readonly Random rdn = new Random();
        private static readonly object rdnLock = new object();

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var showvitrina = new ShowVitrina();
            object counterLock = new object();
            int resersCount = 0;

            var bakers = new List<Thread>();
            var customers = new List<Thread>();

            //----------
            for (int i = 1; i <= 3; i++)
            {
                int bakerId = i;
                Thread t = new Thread(() =>
                {
                    while (true)
                    {
                        int bunId;
                        lock (counterLock)
                        {
                            if (resersCount >= TotalBunsToBake) break;
                            bunId = ++resersCount;
                        }
                        
                        int bakeTime = rdn.Next(200, 601);
                        Thread.Sleep(bakeTime);

                        string type = GetRandomBunType();
                        var bun = new Bun(bunId, type);

                        showvitrina.Put(bun);
                    }
                    Console.WriteLine($"Пекарь испек {bakerId} , уходит");
                });
                bakers.Add(t);
            }
            //------------------------
            for (int i = 1; i <= 5; i++)
            {
                int customerId = i;
                Thread t = new Thread(() =>
                {
                    while (true)
                    {
                        try
                        {
                            
                            var bun = showvitrina.Take();
                            if (bun == null) break;

                            long waitTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - bun.ProducedAtMs;
                            Console.WriteLine($" Покупатель {customerId} купил: {bun}; Лежала на витрине: {waitTime} мс.");
                                                        
                            int consumeTime = rdn.Next(300, 901);
                            Thread.Sleep(consumeTime);
                        }
                        catch (InvalidOperationException)
                        {
                            break;
                        }
                    }
                    Console.WriteLine($" Покупатель {customerId}: пекарня \"CLOSE\", Ухожу");
                });
                customers.Add(t);
            }
            //---------------
            for (int i = 6; i <= 7; i++) 
            {
                int customerId = i;
                Thread t = new Thread(() =>
                {
                    while (true)
                    {
                        Bun bun;                        
                        bool success = showvitrina.TryTake(500, out bun);

                        if (success)
                        {
                            long waitTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - bun.ProducedAtMs;
                            Console.WriteLine($"Нетерпеливый {customerId} все таки купил: {bun}. Лежала: {waitTime} мс");

                            int consumeTime = NextRandom(300, 901);
                            Thread.Sleep(consumeTime);
                        }
                        else
                        {                            
                            lock (showvitrina)
                            {
                                if (showvitrina.GetProducedCount() >= TotalBunsToBake && showvitrina.GetSoldCount() == TotalBunsToBake)
                                {
                                    break; 
                                }
                            }                            
                            Thread.Sleep(NextRandom(100, 300));
                        }
                    }
                    Console.WriteLine($"Нетерпеливый покупатель {customerId}: уходит");
                });
                customers.Add(t);
            }

            //---------
            foreach (var b in bakers) b.Start();
            foreach (var c in customers) c.Start();

            foreach (var b in bakers) b.Join();
            showvitrina.Close();

            foreach (var c in customers) c.Join();
            Console.WriteLine("\n--- РЕЗУЛЬТАТ ---");
            Console.WriteLine($"Испечено булок: {showvitrina.GetProducedCount()}");
            Console.WriteLine($"Продано булок: {showvitrina.GetSoldCount()}");            
        }
        static int NextRandom(int min, int max)
        {
            lock (rdnLock)
            {
                return rdn.Next(min, max);
            }
        }
        static string GetRandomBunType()
        {
            var types = new[] { "с маком", "с корицей", "с вишней" };
            return types[rdn.Next(types.Length)];
        }
    }
}
