FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

RUN dotnet workload install wasm-tools wasm-tools-net7

RUN apt-get update && apt-get install -y git && \
    rm -rf /var/lib/apt/lists/*

COPY SnakeWebGL/SnakeWebGL.csproj SnakeWebGL/
COPY SnakeCore/SnakeCore.csproj SnakeCore/
RUN dotnet restore SnakeWebGL/SnakeWebGL.csproj

COPY SnakeWebGL/ SnakeWebGL/
COPY SnakeCore/ SnakeCore/
COPY submodules/ submodules/
COPY .git/ .git/

RUN git submodule update --init --recursive

RUN apt-get update && apt-get install -y curl && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app/SnakeWebGL
RUN npm install

RUN dotnet publish -c Release -o /app/publish

FROM nginx:alpine

COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]