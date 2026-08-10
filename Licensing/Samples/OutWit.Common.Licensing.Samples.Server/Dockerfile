# The mock runs as a non-root user on purpose. "Does the store land somewhere
# writable in a container running as non-root?" is one of the questions this
# exists to answer, and it cannot be answered by an image that runs as root.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Build.targets Directory.Packages.props global.json nuget.config* ./
COPY Common/OutWit.Common/ Common/OutWit.Common/
COPY Platform/OutWit.Common.Platform/ Platform/OutWit.Common.Platform/
COPY Licensing/OutWit.Common.Licensing/ Licensing/OutWit.Common.Licensing/
COPY Licensing/Samples/OutWit.Common.Licensing.Samples.Server/ Licensing/Samples/OutWit.Common.Licensing.Samples.Server/

RUN dotnet publish Licensing/Samples/OutWit.Common.Licensing.Samples.Server/OutWit.Common.Licensing.Samples.Server.csproj \
    -c Release -o /app --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /app .

# The licence directory is a volume mount point, and it must be writable by the
# user the process runs as — otherwise the installation id cannot be persisted
# and every restart is a different deployment.
RUN mkdir -p /app/license && chown -R app:app /app

USER app

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV Licensing__Directory=/app/license

EXPOSE 8080

ENTRYPOINT ["dotnet", "OutWit.Common.Licensing.Samples.Server.dll"]
