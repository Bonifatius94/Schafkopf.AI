
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

```sh
dotnet restore
dotnet test
```

### Run AI Training
This is still wip, game logic needs to be finished first
