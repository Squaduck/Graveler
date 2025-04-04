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
using System.Runtime.Intrinsics;

class Program
{
    public static void Main(string[] args)
    {
        // Roll 231 4-Sided Dice n Times.
        // Each group of 231 dice is a round
        // We want to track the highest number of times a '1' is rolled in a round
        // Inspired by https://www.youtube.com/watch?v=M8C8dHQE2Ro where a program solving the same problem took 8 days to run for 1 billion rounds

        //Console.WriteLine($"Vector256<byte>.IsSupported: {Vector256<byte>.IsSupported}\nVector256.IsHardwareAccelerated: {Vector256.IsHardwareAccelerated}");

        Stopwatch sw = Stopwatch.StartNew();

        byte HighestNumberOf1sRolled = 0;
        object toBeLocked = new(); // For thread safety.
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
            */

            Span<byte> randBytes = stackalloc byte[58];
            threadLocal.r.NextBytes(randBytes);

            Vector256<byte> a = Vector256.Create(randBytes[0], randBytes[1], randBytes[2], randBytes[3], randBytes[4], randBytes[5], randBytes[6], randBytes[7],
                                                 randBytes[8], randBytes[9], randBytes[10], randBytes[11], randBytes[12], randBytes[13], randBytes[14], randBytes[15],
                                                 randBytes[16], randBytes[17], randBytes[18], randBytes[19], randBytes[20], randBytes[21], randBytes[22], randBytes[23],
                                                 randBytes[24], randBytes[25], randBytes[26], randBytes[27], (byte)(randBytes[28] & 0b01111111), 0, 0, 0);

            Vector256<byte> b = Vector256.Create(randBytes[29], randBytes[30], randBytes[31], randBytes[32], randBytes[33], randBytes[34], randBytes[35], randBytes[36],
                                                 randBytes[37], randBytes[38], randBytes[39], randBytes[40], randBytes[41], randBytes[42], randBytes[43], randBytes[44],
                                                 randBytes[45], randBytes[46], randBytes[47], randBytes[48], randBytes[49], randBytes[50], randBytes[51], randBytes[52],
                                                 randBytes[53], randBytes[54], randBytes[55], randBytes[56], (byte)(randBytes[57] & 0b01111111), 0, 0, 0);

            Vector256<long> c = Vector256.BitwiseAnd(a, b).AsInt64();

            NumberOf1sRolled = (byte)BitOperations.PopCount((ulong)c[0]);
            NumberOf1sRolled += (byte)BitOperations.PopCount((ulong)c[1]);
            NumberOf1sRolled += (byte)BitOperations.PopCount((ulong)c[2]);
            NumberOf1sRolled += (byte)BitOperations.PopCount((ulong)c[3]);

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