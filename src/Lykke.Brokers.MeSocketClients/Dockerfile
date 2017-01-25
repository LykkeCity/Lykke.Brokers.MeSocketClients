FROM microsoft/dotnet:runtime
ENTRYPOINT ["dotnet", "Lykke.Brokers.MeSocketClients.dll"]
ARG source=.
WORKDIR /app
COPY $source .