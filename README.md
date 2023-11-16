
# Schafkopf AI

## About
This project is about teaching an AI to play Schafkopf.

It covers topics such as:
- low-level programming in C#
  - usage of memory-efficient structs
  - parallel / branchless techniques and SIMD
  - usage of unsafe pointer arithmetics
- DDD / TDD techniques
  - concepts are properly modeled for low coupling
  - tests allow to gradually enhance the game logic
    with better performing structures / algorithms
- AI training
  - custom neural network framework
  - several approaches to train agents (wip)

## Quickstart

### Install .NET Tools

```sh
sudo apt-get update && sudo apt-get install -y dotnet-sdk-7.0
```

### Download the Source Code

```sh
git clone https://github.com/Bonifatius94/Schafkopf.AI
cd Schafkopf.AI
```

### Build + Run Tests

```sh
dotnet test --configuration Release
```

### Run Benchmarks

```sh
dotnet run  --configuration Release \
    --project Schafkopf.Lib.Benchmarks/Schafkopf.Lib.Benchmarks.csproj
```

### Run AI Training

```sh
dotnet run  --configuration Release \
    --project Schafkopf.Training/Schafkopf.Training.csproj
```

DISCLAIMER: The training is currently under construction
and won't deliver any results yet.

### Install Docker (Optional)

```sh
sudo apt-get update && sudo apt-get install -y docker.io docker-compose
sudo usermod -aG docker $USER && reboot
```

### Run AI Training (Docker)

```sh
docker-compose -f train-compose.yml build && \
    docker-compose -f train-compose.yml up
```

DISCLAIMER: The training is currently under construction
and won't deliver any results yet.
