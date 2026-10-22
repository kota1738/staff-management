FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/StaffManagement.Api/StaffManagement.Api.csproj src/StaffManagement.Api/
RUN dotnet restore src/StaffManagement.Api/StaffManagement.Api.csproj

COPY src/StaffManagement.Api/ src/StaffManagement.Api/
RUN dotnet publish src/StaffManagement.Api/StaffManagement.Api.csproj \
        -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "StaffManagement.Api.dll"]
