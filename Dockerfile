# ============================================================
# NomadCN API - Dockerfile (多阶段构建)
#
# 构建命令（在 /workspace 目录执行，构建上下文为 /workspace）：
#   docker build -f nomad-cn-api/Dockerfile -t nomadcn-api .
#
# 运行命令：
#   docker run -p 8080:8080 \
#     -e DATABASE_URL="Host=xxx;Database=xxx;Username=xxx;Password=xxx;Port=5432" \
#     -e JWT_KEY="your-secret-key" \
#     nomadcn-api
# ============================================================

# ===== Build stage =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 先复制项目文件，利用 Docker 缓存加速依赖还原
COPY nomad-cn-api/NomadCN.Api.csproj ./
RUN dotnet restore

# 复制 API 源代码
COPY nomad-cn-api/ ./

# 复制前端文件到 wwwroot 目录（dotnet publish 会自动包含 wwwroot）
COPY nomad-cn/ ./wwwroot/

# 发布 Release 版本
RUN dotnet publish -c Release -o /app/publish --no-restore

# ===== Runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# 配置容器环境
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# 从 build 阶段复制发布输出（包含 wwwroot 前端文件）
COPY --from=build /app/publish ./

# 暴露端口
EXPOSE 8080

ENTRYPOINT ["dotnet", "NomadCN.Api.dll"]
