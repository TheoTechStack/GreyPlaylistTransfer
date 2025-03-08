# Use the official ASP.NET Core runtime as a base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the SDK image to build the project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# UPDATED: Copy the API and Shared project files
COPY ["PlaylistTransfer.API/PlaylistTransfer.API.csproj", "PlaylistTransfer.API/"]
COPY ["PlaylistTransfer.Shared/PlaylistTransfer.Shared.csproj", "PlaylistTransfer.Shared/"]
RUN dotnet restore "PlaylistTransfer.API/PlaylistTransfer.API.csproj"

# UPDATED: Copy the entire solution into the container
COPY . .

# UPDATED: Set the working directory to the API project
WORKDIR "/src/PlaylistTransfer.API"
RUN dotnet build "PlaylistTransfer.API.csproj" -c Release -o /app/build

# Publish the project
FROM build AS publish
RUN dotnet publish "PlaylistTransfer.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage: copy the build output to the runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PlaylistTransfer.API.dll"]