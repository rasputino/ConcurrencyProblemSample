# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

# Copy everything else and build
COPY ConcurrencyProblem.sln ./ConcurrencyProblem.sln
COPY ConcurrencyProblem/ConcurrencyProblem.csproj ./ConcurrencyProblem/ConcurrencyProblem.csproj
COPY ConcurrencyProblem.Tests/ConcurrencyProblem.Tests.csproj ./ConcurrencyProblem.Tests/ConcurrencyProblem.Tests.csproj
RUN dotnet restore
# RUN dotnet test ConcurrencyProblem.Tests -c Release
RUN dotnet build

FROM build-env AS testrunner
WORKDIR /app/ConcurrencyProblem.Tests

CMD ["dotnet", "--version"]

CMD ["dotnet", "test", "--no-restore"]
