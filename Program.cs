/* Attempt 1
real    12m21.808s -- Actual execution time.
user    59m18.304s -- "CPU Seconds" in user mode   - Since this is multithreaded, it is more than the actual time.
sys     17m48.641s -- "CPU Seconds" in kernel mode - Since this is multithreaded, it is more than the actual time.
*/

namespace Graveler;

using System;
using System.Threading.Tasks;

class Program
{
    public static void Main(string[] args)
    {
        // Roll 231 4-Sided Dice n Times.
        // Each group of 231 dice is a round
        // We want to track the highest number of times a '1' is rolled in a round
        // Inspired by https://www.youtube.com/watch?v=M8C8dHQE2Ro where a program solving the same problem took 8 days to run for 1 billion rounds

        byte highest_amount_of_1s_rolled = 0;
        To_be_locked to_be_locked = new(); // For thread safety.
        const int Num_Rounds_To_Sim = 1000000000;

        Parallel.For(0, Num_Rounds_To_Sim, (x) => 
        {
            Round round = new();
            round.Run();

            lock (to_be_locked) // Might be very slow. EVERY SINGLE ROUND IS WAITING ON THIS LOCK.
            {
                if (round.times_that_a_1_is_rolled > highest_amount_of_1s_rolled)
                    highest_amount_of_1s_rolled = round.times_that_a_1_is_rolled;
            }
        });

        Console.WriteLine($"Highest number of 1s rolled in {Num_Rounds_To_Sim} rounds: {highest_amount_of_1s_rolled}");
    }   
}

class Round
{
    Random r;
    public byte times_that_a_1_is_rolled;

    public Round()
    {
        r = new();
    }

    public void Run()
    {
        for(int i = 0; i < 231; i++)
        {
            if (r.Next(4) == 0)
                times_that_a_1_is_rolled++;
        }
    }
}

class To_be_locked{};