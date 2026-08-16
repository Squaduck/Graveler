/* Attempt Parallel.Invoke + refactor
Highest number of 1s rolled in 1000000000 rounds: 100
Ran in 00:00:01.1708345

real 0m1.188s
user 0m13.914s
sys  0m0.011s
*/

namespace Graveler;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using System.Runtime.Intrinsics;
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

        // Warn if SIMD acceleration isn't available.
        if (!(Vector256<byte>.IsSupported && Vector256.IsHardwareAccelerated))
            Console.WriteLine($"WARNING:\nVector256<byte>.IsSupported: {Vector256<byte>.IsSupported}\nVector256.IsHardwareAccelerated: {Vector256.IsHardwareAccelerated}\nThe previous implementation may be faster on this CPU.");

        // Start a stopwatch.
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Determine how much work to give to each thread.
        int threads = Environment.ProcessorCount; // This ignores big-little designs, or any other kind of non-uniform CPU topology
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
        Span<byte> randBytes = stackalloc byte[64];
        Random random = new();
        byte localHighestNumberOf1sRolled = 0;

        for (int i = 0; i < numRoundsToDo; i++)
        {
            /*
                Rethinking it yet again: 
                We want to simulate 231 1-in-4 probabilities. 
                Each 1-in-4 probability is equivalent to 2 1-in-2 probabilities ANDed together.
                If we can get 462 1-in-2 probabilities, and AND pairs together, we'll have 231 1-in-4 probabilities
                Each bit in a random number is a 1-in-2 probability
                So we need 462 bits in two 231-bit vectors to AND together.
                Sounds like AVX512 could work!
                Or avx256 would suffice, since my 7600x doesn't have full width avx-512. 
                It only has a half-width avx512 pipeline, and does two* operations to complete avx512 instructions.
                    *Two times what it would have to do for a 256-wide pipeline. It's probably more than one "step."

                231 bits / 8 bits per byte = 28.875 bytes per vector. 
                58 bytes total
                256-231=25
            */

            // Get random bytes
            random.NextBytes(randBytes);

            // Create a Vector256 out of the first 32 bytes
            Vector256<byte> a = Vector256.Create(randBytes);

            // Create a Vector256 out of the second 32 bytes
            Vector256<byte> b = Vector256.Create(randBytes[32..64]);

            // AND them together and reinterpret as int64s for less PopCount calls.
            Vector256<ulong> c = Vector256.BitwiseAnd(a, b).AsUInt64();

            // Get PopCount. (No avx512_vpopcntdq in dotnet yet, see https://github.com/dotnet/runtime/issues/96162)
            byte numberOf1sRolled;
            numberOf1sRolled = unchecked((byte)BitOperations.PopCount(c[0]));
            numberOf1sRolled += unchecked((byte)BitOperations.PopCount(c[1]));
            numberOf1sRolled += unchecked((byte)BitOperations.PopCount(c[2]));
            numberOf1sRolled += unchecked((byte)BitOperations.PopCount(c[3] >>> 25)); // Discard 25 bits.

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
