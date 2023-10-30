using Schafkopf.Lib.Benchmarks;

BenchmarkRunner.Run<DeckShuffleBenchmark>();
BenchmarkRunner.Run<DeckAttributesBenchmark>();
BenchmarkRunner.Run<HandAttributesBenchmark_FarbeCount>();
BenchmarkRunner.Run<HandAttributesBenchmark_FirstFourCards>();
BenchmarkRunner.Run<HandAttributesBenchmark_HasFarbe>();
BenchmarkRunner.Run<HandAttributesBenchmark_HasTrumpf>();
BenchmarkRunner.Run<TurnAttributesBenchmark_WinnerId>();
BenchmarkRunner.Run<TurnAttributesBenchmark_Augen>();
BenchmarkRunner.Run<CardsComparerBenchmark>();
BenchmarkRunner.Run<GameSessionBenchmark>();
BenchmarkRunner.Run<GameCallBenchmark>();
