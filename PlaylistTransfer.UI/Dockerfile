# Use the official ASP.NET Core runtime as a base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the SDK image to build the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# UPDATED: Use paths relative to the root directory
COPY ["PlaylistTransfer.UI/PlaylistTransfer.UI.csproj", "PlaylistTransfer.UI/"]
COPY ["PlaylistTransfer.Shared/PlaylistTransfer.Shared.csproj", "PlaylistTransfer.Shared/"]
RUN dotnet restore "PlaylistTransfer.UI/PlaylistTransfer.UI.csproj"

# UPDATED: Copy the entire solution (root directory) into the container
COPY . .

# UPDATED: Set the working directory to the UI project
WORKDIR "/src/PlaylistTransfer.UI"
RUN dotnet build "PlaylistTransfer.UI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the project
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "PlaylistTransfer.UI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage: copy the build output to the runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PlaylistTransfer.UI.dll"]