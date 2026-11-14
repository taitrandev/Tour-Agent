# -------------------------------------------------------------
# Stage 1: Build Stage (Dùng SDK .NET 10 đầy đủ công cụ build)
# -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /app

# Tối ưu Docker Layer Cache: Copy Directory.Build.props và các file .csproj trước
COPY ["Directory.Build.props", "/app/"]
COPY ["src/TourAgent.Domain/TourAgent.Domain.csproj", "/app/src/TourAgent.Domain/"]
COPY ["src/TourAgent.Application/TourAgent.Application.csproj", "/app/src/TourAgent.Application/"]
COPY ["src/TourAgent.Infrastructure/TourAgent.Infrastructure.csproj", "/app/src/TourAgent.Infrastructure/"]
COPY ["src/TourAgent.Api/TourAgent.Api.csproj", "/app/src/TourAgent.Api/"]

# Restore dependencies (Layer này được Docker cache lại, không phải tải lại mỗi khi sửa code .cs)
RUN dotnet restore "/app/src/TourAgent.Api/TourAgent.Api.csproj"

# Copy toàn bộ mã nguồn src/
COPY src/ /app/src/

# Chuyển vào thư mục project Api
WORKDIR "/app/src/TourAgent.Api"

# -------------------------------------------------------------
# Stage 2: Publish Stage (Biên dịch tối ưu hóa Release)
# -------------------------------------------------------------
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "/app/src/TourAgent.Api/TourAgent.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

# -------------------------------------------------------------
# Stage 3: Final Image (Chỉ chứa binary đã publish và runtime tối giản)
# -------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
USER $APP_UID
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production
COPY --from=publish /app/publish /app/
ENTRYPOINT ["dotnet", "TourAgent.Api.dll"]