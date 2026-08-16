/* Attempt Parallel.Invoke
Highest number of 1s rolled in 1000000000 rounds: 100
Ran in 00:00:01.1692795

real 0m1.186s
user 0m13.909s
sys  0m0.008s
*/

namespace Graveler;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Numerics;
using System.Runtime.Intrinsics;

class Program
{
    public static void Main(string[] args)
    {
        // Roll 231 4-Sided Dice n Times.
        // Each group of 231 dice is a round
        // We want to track the highest number of times a '1' is rolled in a round
        // Inspired by https://www.youtube.com/watch?v=M8C8dHQE2Ro where a program solving the same problem took 8 days to run for 1 billion rounds

        if(!(Vector256<byte>.IsSupported && Vector256.IsHardwareAccelerated))
            Console.WriteLine($"WARNING:\nVector256<byte>.IsSupported: {Vector256<byte>.IsSupported}\nVector256.IsHardwareAccelerated: {Vector256.IsHardwareAccelerated}\nThe previous implementation may be faster on this CPU.");

        Stopwatch sw = Stopwatch.StartNew();

        byte HighestNumberOf1sRolled = 0;
        System.Threading.Lock toBeLocked = new(); // For thread safety.
        const int NUM_ROUNDS_TO_SIM = 1000000000;

        int threads = Environment.ProcessorCount; // This ignores big-little designs, or any other kind of non-uniform CPU topology

        int leftoverCount = NUM_ROUNDS_TO_SIM % threads;
        int baseCount = NUM_ROUNDS_TO_SIM / threads;

        Action[] actions = new Action[threads];

        for (int action_idx = 0; action_idx < threads; action_idx++)
        {
            int num_todo = baseCount;

            if (action_idx < leftoverCount)
                num_todo++;

            actions[action_idx] = () =>
            {
                Span<byte> randBytes = stackalloc byte[64];
                Random random = new();
                byte localHighestNumberOf1sRolled = 0;

                for (int i = 0; i < num_todo; i++)
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

                    byte NumberOf1sRolled;

                    // Get PopCount. (No avx512_vpopcntdq in dotnet yet, see https://github.com/dotnet/runtime/issues/96162)
                    NumberOf1sRolled = unchecked((byte)BitOperations.PopCount(c[0]));
                    NumberOf1sRolled += unchecked((byte)BitOperations.PopCount(c[1]));
                    NumberOf1sRolled += unchecked((byte)BitOperations.PopCount(c[2]));
                    NumberOf1sRolled += unchecked((byte)BitOperations.PopCount(c[3] >>> 25)); // Discard 25 bits.

                    // Parallel reduction stuff. If this go round was our best, save it.
                    if (NumberOf1sRolled > localHighestNumberOf1sRolled)
                        localHighestNumberOf1sRolled = NumberOf1sRolled;
                }

                lock (toBeLocked)
                {
                    if (localHighestNumberOf1sRolled > HighestNumberOf1sRolled)
                        HighestNumberOf1sRolled = localHighestNumberOf1sRolled;
                }
            };
        }

        Parallel.Invoke(actions);

        sw.Stop();

        Console.WriteLine($"Highest number of 1s rolled in {NUM_ROUNDS_TO_SIM} rounds: {HighestNumberOf1sRolled}");
        Console.WriteLine($"Ran in {sw.Elapsed}");
    }
}
