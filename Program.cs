/* Attempt 5
Ran in 00:01:16.4777112

real    1m20.391s
user    9m57.077s
sys     0m0.476s
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
            long l = 0;

            for (int i = 0; i < 231 / 31; i++)
            {
                l = threadLocal.r.NextInt64(); // 63 random bits. each dice roll is 2 bits. 31 dice rolls per long 
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
                if ((l & (0b11 << 30)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 32)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 34)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 36)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 38)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 40)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 42)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 44)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 46)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 48)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 50)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 52)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 54)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 56)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 58)) == 0)
                    NumberOf1sRolled++;
                if ((l & (0b11 << 60)) == 0)
                    NumberOf1sRolled++;
            }
            for (int i = 0; i < 231 % 31; i++)
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