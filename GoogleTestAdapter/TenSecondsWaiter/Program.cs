using System;
using System.Threading;

namespace TenSecondsWaiter
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.Write("Waiting for 10 seconds");

            TimeSpan aSecond = TimeSpan.FromSeconds(1);
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(aSecond);
                Console.Write(".");
            }
            Console.WriteLine("done.");
        }
    }

}