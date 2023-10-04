
# Benchmark

## Deck Benchmarks

|                Method |      Mean |     Error |    StdDev | Allocated |
|---------------------- |----------:|----------:|----------:|----------:|
| Baseline_InitialHands |  3.133 μs | 0.0171 μs | 0.0160 μs |      56 B |
|   Simple_InitialHands | 75.471 μs | 0.2326 μs | 0.2062 μs |  212992 B |

|        Method |     Mean |   Error |  StdDev | Ratio | Allocated | Alloc Ratio |
|-------------- |---------:|--------:|--------:|------:|----------:|------------:|
| SimpleShuffle | 478.5 ns | 1.94 ns | 1.62 ns |  1.25 |     144 B |          NA |
|   SimdShuffle | 381.9 ns | 1.05 ns | 0.82 ns |  1.00 |         - |          NA |

## Hand Benchmarks

|                  Method |       Mean |     Error |    StdDev | Allocated |
|------------------------ |-----------:|----------:|----------:|----------:|
|       Baseline_HasFarbe |  10.081 us | 0.0373 us | 0.0292 us |         - |
|         Simple_HasFarbe | 104.446 us | 0.3452 us | 0.3229 us |         - |
|      Baseline_HasTrumpf |   3.522 us | 0.0158 us | 0.0132 us |         - |
|        Simple_HasTrumpf |   5.795 us | 0.0758 us | 0.0672 us |         - |
|     Baseline_FarbeCount |   9.439 us | 0.0499 us | 0.0416 us |         - |
|       Simple_FarbeCount | 155.494 us | 0.3624 us | 0.3212 us |         - |
| Baseline_FirstFourCards |   1.621 us | 0.0063 us | 0.0056 us |      32 B |
|   Simple_FirstFourCards |  99.296 us | 0.7633 us | 0.6767 us |  155648 B |

## Turn Benchmarks

|          Method |       Mean |    Error |   StdDev | Allocated |
|---------------- |-----------:|---------:|---------:|----------:|
|   Simd_WinnerId |   582.9 us | 11.44 us | 10.71 us |       1 B |
| Simple_WinnerId | 3,648.8 us | 22.74 us | 17.76 us | 5698014 B |
|      Simd_Augen |   118.2 us |  1.33 us |  1.18 us |         - |
|    Simple_Augen | 1,763.7 us | 30.42 us | 25.41 us | 2621442 B |

## Card Comparer Benchmarks

|         Method |     Mean |     Error |    StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------- |---------:|----------:|----------:|------:|--------:|----------:|------------:|
|   Simd_Compare | 4.869 us | 0.0700 us | 0.0621 us |  1.00 |    0.00 |         - |          NA |
| Simple_Compare | 4.130 us | 0.0741 us | 0.0854 us |  0.85 |    0.02 |         - |          NA |

## Possible Game Calls Benchmark

|                  Method |       Mean |     Error |    StdDev | Ratio | RatioSD |  Allocated | Alloc Ratio |
|------------------------ |-----------:|----------:|----------:|------:|--------:|-----------:|------------:|
|   Simd_AllPossibleCalls |   844.1 us |  16.15 us |  17.95 us |  1.00 |    0.00 |  468.74 KB |        1.00 |
| Simple_AllPossibleCalls | 6,760.2 us | 130.47 us | 195.28 us |  8.11 |    0.37 | 9132.47 KB |       19.48 |

## Random Play Benchmark

|        Method |     Mean |   Error |  StdDev | Allocated |
|-------------- |---------:|--------:|--------:|----------:|
| PlayGames_10k | 194.7 ms | 1.61 ms | 1.34 ms | 100.19 MB |
