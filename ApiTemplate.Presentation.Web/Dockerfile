#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ApiTemplate.Presentation.Web/ApiTemplate.Presentation.Web.csproj", "ApiTemplate.Presentation.Web/"]
COPY ["ApiTemplate.Application/ApiTemplate.Application.csproj", "ApiTemplate.Application/"]
COPY ["ApiTemplate.SharedKernel/ApiTemplate.SharedKernel.csproj", "ApiTemplate.SharedKernel/"]
COPY ["ApiTemplate.Infrastructure/ApiTemplate.Infrastructure.csproj", "ApiTemplate.Infrastructure/"]
RUN dotnet restore "ApiTemplate.Presentation.Web/ApiTemplate.Presentation.Web.csproj"
COPY . .
WORKDIR "/src/ApiTemplate.Presentation.Web"
RUN dotnet build "ApiTemplate.Presentation.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ApiTemplate.Presentation.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApiTemplate.Presentation.Web.dll"]