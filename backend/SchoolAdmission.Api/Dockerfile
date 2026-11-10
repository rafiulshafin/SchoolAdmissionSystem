# STAGE 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy the project file and restore dependencies
COPY ["SchoolAdmission.Api.csproj", "./"]

RUN dotnet restore "SchoolAdmission.Api.csproj"

# Copy the remaining source code
COPY . .

# Compile and publish the application
RUN dotnet publish "SchoolAdmission.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false


# STAGE 2: Run the application
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "SchoolAdmission.Api.dll"]