/* Attempt 7
Ran in 00:00:08.6363016

real    0m10.370s
user    1m7.437s
sys     0m0.319s
*/

namespace Graveler;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Numerics;

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

            for (int i = 0; i < 231 / 63; i++)
            {
                // Generate 2 random numbers each with 31 random bits. (Sign bit is never set.)
                // AND them together, resulting in a number where each bit is set only if both of the corresponding bits were set in the original numbers.
                // The odds of each bit being set in each original number is 50/50, so its 1/4 odds that the same bit in each of the numbers was set. 
                // Just count the number of bits set in the result, and it's equivalent to the number of 1's rolled in 31 d4 rolls.
                // This is a very bad explanation, but I think my math checks out.
                NumberOf1sRolled += (byte)BitOperations.PopCount((ulong)(threadLocal.r.NextInt64() & threadLocal.r.NextInt64()));
            }
            // 231 % 63 = 42
            // The loop above misses 42 dice rolls.
            // This does a similar thing to the loop above, but then only takes the last 42 bits of it.
            // That 
            NumberOf1sRolled += (byte)BitOperations.PopCount((ulong)(threadLocal.r.NextInt64() & threadLocal.r.NextInt64()) & 0b111111111111111111111111111111111111111111);
            //for (int i = 0; i < 231 % 63; i++)
            //{
            //    if (threadLocal.r.Next(4) == 0)
            //        NumberOf1sRolled++;
            //}

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