# TechnicalAssignment.BlockchainCollector

## Build

Run the following command to build the solution.
```bash
dotnet build TechnicalAssignment.BlockchainCollector.sln
```

## Run

To run the web application:

Run the Docker Compose command
```bash
docker compose up
```

Navigate to the gateway, through which you can reach the Blockchain service:
```bash
https://localhost:5008/swagger
```

You can directly access the Blockchain service API documentation and test the endpoints via Swagger UI:
```bash
https://localhost:5000/swagger
```

If you want to block direct access to the Blockchain service, comment out the port mapping as shown in the example below.

```yaml
services:
  technicalassignment.blockchaincollector.api:
    image: ${DOCKER_REGISTRY-}blockchain-collector-api
    build:
      context: .
      dockerfile: src/TechnicalAssignment.BlockchainCollector.API/Dockerfile
    ports:
    # - 5000:8080
    networks:
      - technicalassignment
```

## Test

The solution contains unit, integration, and functional tests.

To run the tests:
```bash
dotnet test TechnicalAssignment.BlockchainCollector.sln
```
