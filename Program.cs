/* Attempt Parallel.Invoke + old int64 implementation
Highest number of 1s rolled in 1000000000 rounds: 100
Ran in 00:00:02.1567085

real 0m2.174s
user 0m25.733s
sys  0m0.007s
*/

namespace Graveler;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using System.Runtime.CompilerServices;

class Program
{
    const int NUM_ROUNDS_TO_SIM = 1_000_000_000;

    static byte HighestNumberOf1sRolled = 0;
    static readonly Lock HighestNumberOf1sRolled_lock = new(); // For thread safety.

    public static void Main(string[] args)
    {
        // Roll 231 4-Sided Dice n Times.
        // Each group of 231 dice is a round
        // We want to track the highest number of times a '1' is rolled in a round
        // Inspired by https://www.youtube.com/watch?v=M8C8dHQE2Ro where a program solving the same problem took 8 days to run for 1 billion rounds

        // Start a stopwatch.
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Determine how much work to give to each thread.
        // This ignores big-little designs, or any other kind of non-uniform CPU topology.
        // For recent Intel systems, using Parallel.For may be faster. (Or manually setting threads to the number of threads that support AVX512.)
        int threads = Environment.ProcessorCount;
        int baseCount = NUM_ROUNDS_TO_SIM / threads;
        int leftoverCount = NUM_ROUNDS_TO_SIM % threads;

        // Prepare array of jobs to run.
        Action[] actions = new Action[threads];
        for (int i = 0; i < threads; i++)
        {
            int numRoundsToDo = baseCount;

            if (i < leftoverCount)
                numRoundsToDo++;

            actions[i] = () => RollRounds(numRoundsToDo);
        }

        // Run jobs in parallel.
        Parallel.Invoke(actions);

        // Work is done, not counting printing in our own time.
        stopwatch.Stop();

        // Print results.
        Console.WriteLine($"Highest number of 1s rolled in {NUM_ROUNDS_TO_SIM} rounds: {HighestNumberOf1sRolled}");
        Console.WriteLine($"Ran in {stopwatch.Elapsed}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)] // I'm not sure if this is actually doing anything.
    public static void RollRounds(int numRoundsToDo)
    {
        Random random = new();
        byte localHighestNumberOf1sRolled = 0;

        for (int i = 0; i < numRoundsToDo; i++)
        {
            byte numberOf1sRolled = 0;

            for (int j = 0; j < 231 / 63; j++) // loops 3 times for 189 bits.
            {
                // Generate 2 random numbers each with 31 random bits. (Sign bit is never set.)
                // AND them together, resulting in a number where each bit is set only if both of the corresponding bits were set in the original numbers.
                // The odds of each bit being set in each original number is 50/50, so its 1/4 odds that the same bit in each of the numbers was set. 
                // Just count the number of bits set in the result, and it's equivalent to the number of 1's rolled in 31 d4 rolls.
                // This is a very bad explanation, but I think my math checks out.
                numberOf1sRolled += (byte)BitOperations.PopCount((ulong)(random.NextInt64() & random.NextInt64()));
            }

            // 231 % 63 = 42
            // The loop above misses 42 dice rolls.
            // 63-42 = 21 bits that need to be discarded.
            numberOf1sRolled += (byte)BitOperations.PopCount((ulong)(random.NextInt64() & random.NextInt64()) >>> 21);

            // If this go round was our best, save it.
            if (numberOf1sRolled > localHighestNumberOf1sRolled)
                localHighestNumberOf1sRolled = numberOf1sRolled;
        }

        // Check if our thread's highest number of 1s rolled is the overall highest.
        lock (HighestNumberOf1sRolled_lock)
        {
            if (localHighestNumberOf1sRolled > HighestNumberOf1sRolled)
                HighestNumberOf1sRolled = localHighestNumberOf1sRolled;
        }
    }
}
