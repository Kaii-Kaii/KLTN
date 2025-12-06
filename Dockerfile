# Dùng image SDK để build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy file csproj và restore các thư viện
COPY *.csproj ./
RUN dotnet restore

# Copy toàn bộ code còn lại và build
COPY . ./
RUN dotnet publish -c Release -o out

# Dùng image Runtime nhẹ để chạy
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Mở port (Render thường dùng port 80 hoặc biến môi trường PORT)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BE_QLTiemThuoc.dll"]