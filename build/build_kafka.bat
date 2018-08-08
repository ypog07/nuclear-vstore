@echo off

docker build -t kafka_temp -f .\Dockerfile.librdkafka-alpine .

docker run --rm --entrypoint cat kafka_temp /librdkafka/src/librdkafka.so.1 > .\librdkafka.so

docker rmi kafka_temp
