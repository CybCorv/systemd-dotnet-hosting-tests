# Multi-stage build approach:
# This Dockerfile assumes the application has been published locally as self-contained.
# Build locally first with: dotnet publish kestrelTests.csproj -c Release -r linux-x64 --self-contained -o ./publish
# Then build the image with: docker build -t test-kestrel .

FROM debian:trixie-slim
WORKDIR /app

# Copy the published application.
# The publish must include --self-contained so the .NET runtime is bundled.
COPY ./publish/ .
RUN chmod +x ./kestrelTests

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENTRYPOINT ["./kestrelTests"]
