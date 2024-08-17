/* Attempt 4
Ran in 00:04:31.9084680

real    4m35.743s
user    35m10.645s
sys     0m1.246s
*/

namespace Graveler;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

class Program
{
    public static void Main(string[] args)
    {
        // Roll 231 4-Sided Dice n Times.
        // Each group of 231 dice is a round
        // We want to track the highest number of times a '1' is rolled in a round
        // Inspired by https://www.youtube.com/watch?v=M8C8dHQE2Ro where a program solving the same problem took 8 days to run for 1 billion rounds

        Stopwatch sw = Stopwatch.StartNew();

        byte HighestNumberOf1sRolled = 0;
        ToBeLocked toBeLocked = new(); // For thread safety.
        const int NUM_ROUNDS_TO_SIM = 1000000000;

        Parallel.For<RandAndByte>(0, NUM_ROUNDS_TO_SIM, () => new(), (x, loopState, threadLocal) =>
        {
            byte NumberOf1sRolled = 0;

            for (int i = 0; i < 231; i++)
            {
                if (threadLocal.r.Next(4) == 0)
                    NumberOf1sRolled++;
            }

            threadLocal.b = NumberOf1sRolled > threadLocal.b ? NumberOf1sRolled : threadLocal.b;
            return threadLocal;
        }, (x) => 
        {
            lock (toBeLocked)
            {
                if (x.b > HighestNumberOf1sRolled)
                    HighestNumberOf1sRolled = x.b;
            }
        });

        sw.Stop();

        Console.WriteLine($"Highest number of 1s rolled in {NUM_ROUNDS_TO_SIM} rounds: {HighestNumberOf1sRolled}");
        Console.WriteLine($"Ran in {sw.Elapsed}");
    }
}

class ToBeLocked { };

struct RandAndByte 
{
    public Random r;
    public byte b;
    public RandAndByte()
    {
        r = new();
        b = 0;
    }
};