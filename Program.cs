/* Attempt net10.0 + memory improvements
Highest number of 1s rolled in 1000000000 rounds: 101
Ran in 00:00:01.5319304

real 0m1.553s
user 0m18.267s
sys  0m0.012s
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

        Parallel.For<RandAndByte>(0, NUM_ROUNDS_TO_SIM, () => new(), (x, loopState, threadLocal) =>
        {
            byte NumberOf1sRolled;

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
            Span<byte> randBytes = threadLocal.arr; // Cast array to span. Faster than stackalloc-ing every time. Casting to a span also seems to be faster than just using the array directly? (Also, keeping a Memory<byte> in the thread local was slower too.)
            threadLocal.r.NextBytes(randBytes); // Fill a slice excluding 3 bytes? we're going to discard 25 bits anyway, so we could just not generate 24 of them.

            // Create a Vector256 out of the first 32 bytes
            Vector256<byte> a = Vector256.Create(randBytes);

            // Create a Vector256 out of the second 32 bytes
            Vector256<byte> b = Vector256.Create(randBytes[32..64]);

            // AND them together and reinterpret as int64s for less PopCount calls.
            Vector256<ulong> c = Vector256.BitwiseAnd(a, b).AsUInt64();

            // Get PopCount. (No avx512_vpopcntdq in dotnet yet, see https://github.com/dotnet/runtime/issues/96162)
            NumberOf1sRolled = unchecked((byte)BitOperations.PopCount(c[0]));
            NumberOf1sRolled += unchecked((byte)BitOperations.PopCount(c[1]));
            NumberOf1sRolled += unchecked((byte)BitOperations.PopCount(c[2]));
            NumberOf1sRolled += unchecked((byte)BitOperations.PopCount(c[3] >>> 25)); // Discard 25 bits.

            // Parallel reduction stuff. If this go round was our best, save it.
            if (NumberOf1sRolled > threadLocal.b)
                threadLocal.b = NumberOf1sRolled;
                
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

struct RandAndByte
{
    public Random r;
    public byte b;
    public byte[] arr; // Can't keep span in a thread local (ref struct limitations), but this at least helps prevent allocations.
    public RandAndByte()
    {
        r = new();
        b = 0;
        arr = new byte[64];
    }
};