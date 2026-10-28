# ---------- 构建阶段：SDK 编译发布 ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 先还原依赖，充分利用 Docker 层缓存（源码改动不触发重新还原）
COPY CrcWeb.csproj .
RUN dotnet restore

# 再拷贝全部源码并发布
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ---------- 运行阶段：精简运行时镜像（官方多架构，含 linux/arm64，适配树莓派） ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# 容器内监听 8080（非特权端口）；宿主端口由 -p / compose 映射决定
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "CrcWeb.dll"]
