
# Benchmark

## Deck Benchmarks

|                Method |      Mean |     Error |    StdDev | Ratio | Allocated |
|---------------------- |----------:|----------:|----------:|------:|----------:|
| Baseline_InitialHands |  3.133 μs | 0.0171 μs | 0.0160 μs |  1.00 |      56 B |
|   Simple_InitialHands | 75.471 μs | 0.2326 μs | 0.2062 μs | 24.09 |  212992 B |

|        Method |     Mean |   Error |  StdDev | Ratio | Allocated | Alloc Ratio |
|-------------- |---------:|--------:|--------:|------:|----------:|------------:|
|   SimdShuffle | 356.0 ns | 1.64 ns | 1.54 ns |  1.00 |         - |          NA |
| SimpleShuffle | 437.1 ns | 0.99 ns | 0.88 ns |  1.23 |     144 B |          NA |

## Hand Benchmarks

|            Method |       Mean |     Error |    StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------ |-----------:|----------:|----------:|------:|--------:|----------:|------------:|
|   Simd_FarbeCount |   8.746 μs | 0.0283 μs | 0.0251 μs |  1.00 |    0.00 |         - |          NA |
| Simple_FarbeCount | 156.926 μs | 0.7095 μs | 0.6636 μs | 17.94 |    0.09 |         - |          NA |

|                Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
|   Simd_FirstFourCards |  1.501 μs | 0.0031 μs | 0.0026 μs |  1.00 |    0.00 |      32 B |        1.00 |
| Simple_FirstFourCards | 89.382 μs | 0.2851 μs | 0.2528 μs | 59.53 |    0.21 |  155648 B |    4,864.00 |

|          Method |      Mean |     Error |    StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------- |----------:|----------:|----------:|------:|--------:|----------:|------------:|
|   Simd_HasFarbe |  9.396 μs | 0.0831 μs | 0.0737 μs |  1.00 |    0.00 |         - |          NA |
| Simple_HasFarbe | 86.326 μs | 0.4238 μs | 0.3964 μs |  9.19 |    0.07 |         - |          NA |

|           Method |     Mean |     Error |    StdDev | Ratio | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|------:|----------:|------------:|
|   Simd_HasTrumpf | 3.251 μs | 0.0079 μs | 0.0066 μs |  1.00 |         - |          NA |
| Simple_HasTrumpf | 6.947 μs | 0.0349 μs | 0.0326 μs |  2.14 |         - |          NA |

## Turn Benchmarks

|          Method |       Mean |    Error |  StdDev | Ratio | RatioSD | Allocated |  Alloc Ratio |
|---------------- |-----------:|---------:|--------:|------:|--------:|----------:|-------------:|
|   Simd_WinnerId |   532.3 μs |  1.95 μs | 1.82 μs |  1.00 |    0.00 |       1 B |         1.00 |
| Simple_WinnerId | 3,230.3 μs | 10.48 μs | 9.29 μs |  6.07 |    0.03 | 5666091 B | 5,666,091.00 |

|       Method |       Mean |   Error |  StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------- |-----------:|--------:|--------:|------:|--------:|----------:|------------:|
|   Simd_Augen |   110.8 μs | 1.82 μs | 1.61 μs |  1.00 |    0.00 |         - |          NA |
| Simple_Augen | 1,630.9 μs | 4.05 μs | 3.79 μs | 14.72 |    0.22 | 2621442 B |          NA |

## Card Comparer Benchmarks

|         Method |     Mean |     Error |    StdDev | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------- |---------:|----------:|----------:|------:|--------:|----------:|------------:|
|   Simd_Compare | 4.869 us | 0.0700 us | 0.0621 us |  1.00 |    0.00 |         - |          NA |
| Simple_Compare | 4.130 us | 0.0741 us | 0.0854 us |  0.85 |    0.02 |         - |          NA |

## Possible Game Calls Benchmark

|                  Method |       Mean |     Error |    StdDev |     Median | Ratio | RatioSD |  Allocated | Alloc Ratio |
|------------------------ |-----------:|----------:|----------:|-----------:|------:|--------:|-----------:|------------:|
|   Simd_AllPossibleCalls |   855.7 μs |  12.09 μs |  11.31 μs |   856.5 μs |  1.00 |    0.00 |  468.59 KB |        1.00 |
| Simple_AllPossibleCalls | 6,487.1 μs | 128.48 μs | 302.83 μs | 6,386.1 μs |  7.75 |    0.35 | 9129.29 KB |       19.48 |

## Random Play Benchmark

|        Method |     Mean |    Error |   StdDev | Allocated |
|-------------- |---------:|---------:|---------:|----------:|
| PlayGames_10k | 74.62 ms | 0.414 ms | 0.346 ms |   25.1 MB |
