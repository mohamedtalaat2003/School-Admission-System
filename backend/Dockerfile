FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Restore dependencies
COPY src/SchoolAdmissionSystem.Api/SchoolAdmissionSystem.Api.csproj src/SchoolAdmissionSystem.Api/
RUN dotnet restore src/SchoolAdmissionSystem.Api/SchoolAdmissionSystem.Api.csproj

# Copy code and publish release build
COPY src/SchoolAdmissionSystem.Api/ src/SchoolAdmissionSystem.Api/
WORKDIR /app/src/SchoolAdmissionSystem.Api
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SchoolAdmissionSystem.Api.dll"]
