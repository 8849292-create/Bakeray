using System;
using System.Collections.Generic;
using System.Threading;

namespace Bakeray
{
    public class Bun
    {
        public int Id { get; }
        public string Type { get; }
        public long ProducedAtMs { get; }

        public Bun(int id, string type)
        {
            Id = id;
            Type = type;
            ProducedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public override string ToString() => $"Булочка {Id} ({Type})";
    }

    public class ShowVitrina
    {
        private readonly Queue<Bun> queue = new Queue<Bun>();
        private const int Capacity = 5;
        private bool isClosed = false;
        
        private int productCount = 0;
        private int soldCount = 0;
        //private static readonly Random rdn = new Random();

        public void Put(Bun bun)
        {
            lock (this)
            {
                while (queue.Count > Capacity && !isClosed)
                {
                    Console.WriteLine(" Витрина наполнена(5 шт). Ждем свободного места...");
                    Monitor.Wait(this);
                }
                if (isClosed) return;

                queue.Enqueue(bun);
                Interlocked.Increment(ref productCount);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($" (Витрина): Добавлена {bun}" +
                                  $" На витрине: {queue.Count}/{Capacity}");
                Console.ResetColor();

                Monitor.PulseAll(this);
            }
        }
        //-------
        public Bun Take()
        {
            lock (this)
            {
                while (queue.Count == 0 && !isClosed)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(" (Покупатель): Витрина пуста. Жду булочку...");
                    Console.ResetColor();

                    Monitor.Wait(this);
                }
                if (queue.Count == 0)
                {
                    Console.WriteLine("Пекарня закрыта, булок нет");
                    return null;
                }
                var bun = queue.Dequeue();
                Interlocked.Increment(ref soldCount);

                Monitor.PulseAll(this);
                return bun;
            }
        }
        public bool TryTake(int timeoutMs, out Bun bun)
        {
            bun = null;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            lock (this)
            {
                while (queue.Count == 0 && !isClosed)
                {
                    long remaining = timeoutMs - stopwatch.ElapsedMilliseconds;
                    if (remaining <= 0) return false;

                    Console.WriteLine($"(Нетерпеливый покупатель): На витрине пусто, ждем еще {remaining} мс...");
                    Monitor.Wait(this, (int)remaining);
                }

                if (queue.Count == 0) return false;

                bun = queue.Dequeue();
                Interlocked.Increment(ref soldCount);
                Monitor.PulseAll(this);
                return true;                
            }
        }
        //------
        public void Close()
        {
            lock (this)
            {
                isClosed = true;
                Console.WriteLine("\n Пекарня \"CLOSE\", будим всех ожидающих...");
                Monitor.PulseAll(this); 
            }
        }
        public int GetProducedCount() => Interlocked.CompareExchange(ref productCount, 0, 0);
        public int GetSoldCount() => Interlocked.CompareExchange(ref soldCount, 0, 0);
    }
}
