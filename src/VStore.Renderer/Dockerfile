FROM docker-hub.2gis.ru/microsoft/dotnet:2.0.7-runtime-deps
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS http://*:5000

ENTRYPOINT ["./VStore.Renderer"]

COPY . .