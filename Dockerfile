# Acesse https://aka.ms/customizecontainer para saber como personalizar seu contêiner de depuração e como o Visual Studio usa este Dockerfile para criar suas imagens para uma depuração mais rápida.

# Esta fase é usada durante a execução no VS no modo rápido (Padrão para a configuração de Depuração)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Esta fase é usada para compilar o projeto de serviço
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["auth-api-login.Api/auth-api-login.Api.csproj", "auth-api-login.Api/"]
COPY ["auth-api-login.Application/auth-api-login.Application.csproj", "auth-api-login.Application/"]
COPY ["auth-api-login.Infrastructure/auth-api-login.Infrastructure.csproj", "auth-api-login.Infrastructure/"]
COPY ["auth-api-login.Domain/auth-api-login.Domain.csproj", "auth-api-login.Domain/"]
RUN dotnet restore "auth-api-login.Api/auth-api-login.Api.csproj"
COPY . .
WORKDIR "/src/auth-api-login.Api"
RUN dotnet build "auth-api-login.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase é usada para publicar o projeto de serviço a ser copiado para a fase final
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "auth-api-login.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase é usada na produção ou quando executada no VS no modo normal (padrão quando não está usando a configuração de Depuração)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "auth-api-login.Api.dll"]