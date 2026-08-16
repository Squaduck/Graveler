## How to run
### Install .NET 10
Follow the instructions at https://dotnet.microsoft.com/en-us/download
### Clone this repo
Either run `git clone https://github.com/Squaduck/Graveler.git` or click the big green "<> Code" button above and click "Download ZIP"
### Run the program
Navigate into the cloned directory with a command line and run `dotnet run -c Release` to run it with optimizations

## Performance notes
The `main` branch of this project relies on sufficient 256-bit vector SIMD acceleration being available on *every* CPU core, and may perform poorly on CPUs that don't support the required features.

Theoretically, this project currently benefits from the `AVX2` and `POPCNT`/`BMI1` x86 extensions, but should support the ARM equivalents as well.
In the future I want to use `avx512_vpopcntdq` to accelerate this even further, but [dotnet currently doesn't support it](https://github.com/dotnet/runtime/issues/96162).

Additionally, to minimize overhead, the `main` branch of this project creates a task with an equal amount of work for every thread.
If there are threads that perform worse, then it will likely bottleneck the process.
On 12th gen and later Intel CPUs, the efficiency cores *do support `AVX2` and `POPCNT`*, but I haven't benchmarked them yet.

To try and get around these potential issues, this project has different branches.
- [Main branch](https://github.com/Squaduck/Graveler/tree/main) - **You are here.** All performance notes above apply.
- [Parallel.For branch](https://github.com/Squaduck/Graveler/tree/ParallelFor) - Should support non-equal CPU cores better, at the cost of having extra overhead to manage the thread pool.
- [No Vectors branch](https://github.com/Squaduck/Graveler/tree/no_vectors) - Only uses 64-bit `POPCNT`. I don't know if this will perform better anywhere because 256-bit vectors can be emulated in software.