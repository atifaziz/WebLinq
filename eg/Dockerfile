FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /app

COPY *.sln .
COPY eg/*.csproj ./eg/
COPY src/Core/*.csproj ./src/Core/
COPY tests/*.csproj ./src/tests/
RUN dotnet sln remove tests/WebLinq.Tests.csproj
RUN dotnet restore

COPY . .
WORKDIR /app/eg
RUN dotnet publish -c Release -o dist

FROM microsoft/dotnet:2.1-runtime AS runtime
WORKDIR /app
COPY --from=build /app/eg/dist .
ENTRYPOINT ["dotnet", "WebLinq.Samples.dll"]
