/* Attempt 5.5
Ran in 00:01:22.7753993

real    1m24.636s
user    10m50.635s
sys     0m0.625s
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
            int l = 0;

            for (int i = 0; i < 231 / 15; i++)
            {
                l = threadLocal.r.Next(); // 31 random bits. each dice roll is 2 bits. 15 dice rolls per long 
                if ((l & 0b11) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 2)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 4)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 6)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 8)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 10)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 12)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 14)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 16)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 18)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 20)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 22)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 24)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 26)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 28)) == 0)
                    NumberOf1sRolled++;
            }
            for (int i = 0; i < 231 % 15; i++)
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