
# set up build environment with cached dependencies
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app/src
ADD ./Schafkopf.AI.sln ./Schafkopf.AI.sln
ADD ./Schafkopf.Lib/Schafkopf.Lib.csproj ./Schafkopf.Lib/Schafkopf.Lib.csproj
ADD ./Schafkopf.Lib.Tests/Schafkopf.Lib.Tests.csproj ./Schafkopf.Lib.Tests/Schafkopf.Lib.Tests.csproj
ADD ./Schafkopf.Lib.Benchmarks/Schafkopf.Lib.Benchmarks.csproj ./Schafkopf.Lib.Benchmarks/Schafkopf.Lib.Benchmarks.csproj
ADD ./Schafkopf.Training/Schafkopf.Training.csproj ./Schafkopf.Training/Schafkopf.Training.csproj
ADD ./Schafkopf.Training.Tests/Schafkopf.Training.Tests.csproj ./Schafkopf.Training.Tests/Schafkopf.Training.Tests.csproj
RUN dotnet restore --runtime linux-x64

# build code and run automated tests
ADD . .
RUN dotnet test --runtime linux-x64 --configuration Release --no-restore

# release pre-built binaries
RUN dotnet publish --runtime linux-x64 --configuration Release \
                   --output /app/bin/ --no-restore

# set up minimalistic runtime
FROM mcr.microsoft.com/dotnet/runtime:7.0 AS runtime
WORKDIR /app/bin
COPY --from=build-env /app/bin /app/bin

# launch training
ENTRYPOINT ["dotnet", "Schafkopf.Training.dll"]
