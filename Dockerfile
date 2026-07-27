# ============================================================
# NomadCN - Dockerfile (多阶段构建)
# 支持在独立仓库中构建：自动从 GitHub 拉取前端代码
# 默认使用 SQLite（零配置），也支持 PostgreSQL（设置 DATABASE_URL）
#
# 构建命令（在 nomad-cn-api 目录执行）：
#   docker build -t nomadcn .
#
# 运行命令（SQLite 零配置）：
#   docker run -p 8080:8080 nomadcn
#
# 运行命令（PostgreSQL）：
#   docker run -p 8080:8080 -e DATABASE_URL="postgres://..." nomadcn
# ============================================================

# ===== Build stage =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 安装 git 并克隆前端仓库
RUN apt-get update && apt-get install -y git && rm -rf /var/lib/apt/lists/*
RUN git clone --depth 1 https://github.com/Echoban/nomad-cn.git /tmp/frontend

# 先复制项目文件，利用 Docker 缓存加速依赖还原
COPY NomadCN.Api.csproj ./
RUN dotnet restore

# 复制 API 源代码
COPY . ./

# 复制前端文件到 wwwroot 目录
RUN mkdir -p wwwroot && cp -r /tmp/frontend/* wwwroot/ && rm -rf /tmp/frontend

# 发布 Release 版本
RUN dotnet publish -c Release -o /app/publish --no-restore

# ===== Runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# 配置容器环境
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080

# 从 build 阶段复制发布输出（包含 wwwroot 前端文件）
COPY --from=build /app/publish ./

# 创建数据目录（SQLite 数据文件存放位置）
RUN mkdir -p /data

# 暴露端口
EXPOSE 8080

ENTRYPOINT ["dotnet", "NomadCN.Api.dll"]
