/* Attempt 3
Ran in 00:09:25.4170262 -- Output from program itself

real    9m29.226s -- Includes compilation
user    37m9.127s
sys     31m7.335s
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

        Parallel.For(0, NUM_ROUNDS_TO_SIM, (x) =>
        {
            Random r = new();
            byte NumberOf1sRolled = 0;

            for (int i = 0; i < 231; i++)
            {
                if (r.Next(4) == 0)
                    NumberOf1sRolled++;
            }

            if (NumberOf1sRolled > HighestNumberOf1sRolled) // Quick and dirty solution.
            {
                lock (toBeLocked)
                {
                    if (NumberOf1sRolled > HighestNumberOf1sRolled)
                        HighestNumberOf1sRolled = NumberOf1sRolled;
                }
            }
        });

        sw.Stop();

        Console.WriteLine($"Highest number of 1s rolled in {NUM_ROUNDS_TO_SIM} rounds: {HighestNumberOf1sRolled}");
        Console.WriteLine($"Ran in {sw.Elapsed}");
    }
}

class ToBeLocked { };