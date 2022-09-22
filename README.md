
# Schafkopf AI

## About
This project is about teaching an AI to play Schafkopf.

It covers topics such as:
- low-level programming in C#
  - usage of memory-efficient structs
  - parallel / branchless processing with SIMD
  - usage of unsafe pointer arithmetics
- DDD / TDD techniques
  - concepts are properly modeled for low coupling
  - tests allow to gradually enhance the game logic
    with better performing structures / algorithms
- ML.NET AI training
  - still under construction, wip

## Quickstart

### Install .NET Tools

```sh
sudo apt-get update && sudo apt-get install -y dotnet6
```

### Download the Source Code

```sh
git clone https://github.com/Bonifatius94/Schafkopf.AI
cd Schafkopf.AI
```

### Build + Run Tests

Just run the tests

```sh
dotnet test
```

Test with code coverage

```sh
dotnet test /p:CollectCoverage=true \
    /p:CoverletOutputFormat=lcov \
    /p:CoverletOutput=./lcov.info
```

---

***Note:** This workflow requires 'Coverlet' and 'Coverage Gutters'
VSCode extensions to be installed. Moreover, the 'coverlet.msbuild'
NuGet package needs to be installed to the unit test projects
to collect coverage logs from.*

### Run Benchmarks

```sh
dotnet run --project Schafkopf.Lib.Benchmarks/Schafkopf.Lib.Benchmarks.csproj --configuration Release
```

### Run AI Training (Docker)

```sh
docker build . -f TrainEnv.Dockerfile -t schafkopf-trainenv
docker run schafkopf-trainenv
```

This is still wip, game logic needs to be finished first
