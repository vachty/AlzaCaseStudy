# AlzaCaseStudy

## Prerequisites

To run the solution locally, make sure you have the following installed:

- [.NET SDK 10.0](https://dotnet.microsoft.com/)
- [Docker](https://www.docker.com/)
- Docker Compose support (`docker compose`)
- Git

It is recommended to run the project from the repository root.

## How to run

1. Clone the repository
2. In solution root folder, where docker compose file is located:
	- docker compose up --build 

This command starts:

- aggregationservice.api
- productsimulation.api
- pricingsimulation.api
- stocksimulation.api
- rabbitmq

3. Access the services
After startup, the following endpoints are available:

- Aggregation API: `http://localhost:5001`
- Product Simulation API: `http://localhost:5003`
- Pricing Simulation API: `http://localhost:5005`
- Stock Simulation API: `http://localhost:5007`
- RabbitMQ Management UI: `http://localhost:15672`
- RabbitMQ credentials:

  - username: `guest`
  - password: `guest`


## Overview

This repository contains a small cloud-oriented product aggregation solution built in .NET.

The main API receives a product identifier, calls multiple downstream services, and returns a single aggregated response to the caller. The solution demonstrates a pragmatic microservice-style approach focused on:

- request-time aggregation
- asynchronous downstream calls
- resilience and graceful degradation
- in-memory caching
- observability
- containerized local execution
- integration event publishing through RabbitMQ
- integration testing with Testcontainers and `WebApplicationFactory`

The project is intentionally kept compact and focused on the case study scope.

---

## Architecture

### Main components

- **AggregationService.API**
  - the main entry point
  - exposes the HTTP API for aggregated product reads
  - wires up controllers, OpenAPI, telemetry, caching, clients, and services

- **AggregationService.Application**
  - application contracts and orchestration logic
  - contains service abstractions and DTO contracts

- **AggregationService.Infrastructure**
  - technical integrations
  - HTTP clients for downstream services
  - RabbitMQ publisher implementation

- **ProductSimulation.API**
  - simulated product information source

- **PricingSimulation.API**
  - simulated pricing source

- **StockSimulation.API**
  - simulated stock availability source

- **RabbitMQ**
  - integration broker used for publishing product aggregation events

---

## Request flow

The aggregation flow is synchronous from the caller perspective:

1. client calls the aggregation endpoint
2. the API fans out to:
   - product service
   - pricing service
   - stock service
3. the responses are combined into one aggregated DTO
4. the final response is returned to the client
5. after a successful aggregation, an integration event is published to RabbitMQ

This keeps the external API simple while still allowing future asynchronous consumers.

---

## Resilience and degradation

The solution is designed so that a partial response can still be returned if some downstream dependencies fail.

### Current approach

- downstream calls are executed asynchronously
- pricing client has resilience policies configured
- the aggregation flow tolerates selected dependency failures
- degraded dependencies are tracked in the response
- in-memory caching reduces repeated downstream calls

This means the API can still serve useful data even if some non-critical downstream information is temporarily unavailable.

---

## Event publishing

After a successful aggregation, the service publishes an integration event to RabbitMQ.

### Why RabbitMQ is included

RabbitMQ is used as an asynchronous extension point for:

- audit-like downstream processing
- analytics
- future consumers
- future read-model or reporting pipelines

The current API remains request/response oriented. RabbitMQ does not replace the HTTP response path; it extends the architecture with a decoupled integration mechanism.

### Current messaging configuration

The Docker Compose setup provides RabbitMQ with:

- AMQP port: `5672`
- management UI: `15672`

The aggregation service is configured through `MessagingOptions` environment variables in `docker-compose.yml`.

---

## Local development architecture

When started locally, the following services are available:

- Aggregation API: `http://localhost:5001`
- Product simulation API: `http://localhost:5003`
- Pricing simulation API: `http://localhost:5005`
- Stock simulation API: `http://localhost:5007`
- RabbitMQ AMQP: `localhost:5672`
- RabbitMQ management UI: `http://localhost:15672`

Default RabbitMQ credentials in local development:

- username: `guest`
- password: `guest`

---

## How to run

### Prerequisites

You need:

- .NET SDK
- Docker
- Docker Compose / Docker Desktop

### Run the whole solution with Docker Compose

From the repository root:

```bash
docker compose up --build
```

This starts:

- `aggregationservice.api`
- `productsimulation.api`
- `pricingsimulation.api`
- `stocksimulation.api`
- `rabbitmq`

### Open the API

After startup, open:

```text
http://localhost:5001
```

If running in development mode, OpenAPI/Scalar endpoints are also exposed by the API. The application configures OpenAPI and Scalar in development through the ASP.NET Core startup pipeline.
```text
http://localhost:5001/scalar/
```


### RabbitMQ management UI

Open:

```text
http://localhost:15672
```

Credentials:

```text
guest / guest
```

---

## Configuration

### Downstream services

The aggregation API reads downstream service URLs from `ServicesOptions`:

- `ServicesOptions__ProductSimulationUrl`
- `ServicesOptions__PricingSimulationUrl`
- `ServicesOptions__StockSimulationUrl`

### Messaging

RabbitMQ configuration is supplied through `MessagingOptions`:

- `MessagingOptions__HostName`
- `MessagingOptions__Port`
- `MessagingOptions__UserName`
- `MessagingOptions__Password`
- `MessagingOptions__ExchangeName`
- `MessagingOptions__RoutingKey`

These values are provided in the local Docker Compose setup.

---

## Testing

The solution contains both unit and integration tests.

### Unit tests

Unit tests focus on application/service behavior and business flow validation in isolation.

Run:

```bash
dotnet test
```

### Integration tests

The integration tests use:

- `WebApplicationFactory`
- Testcontainers
- real containerized dependencies
- dynamic configuration overrides for service URLs and RabbitMQ

This allows the tests to boot the real API startup path and validate behavior against realistic infrastructure instead of mocks only.

### Suggested integration coverage

The current integration testing direction is suited for scenarios such as:

- the endpoint returns a successful aggregated response
- RabbitMQ integration event is published after a successful aggregation
- degraded responses still work when a downstream dependency fails

---

## Observability

The API includes:

- Serilog logging
- OpenTelemetry tracing
- OpenTelemetry metrics

This supports local diagnostics and makes the application easier to evolve toward a more production-like cloud deployment model. Startup registration for logging and telemetry is done in the API composition root.

---

## Security note

Authentication and authorization are intentionally treated as design-only in this case study.

See:

- `Auth.md`

That document describes the intended production direction, including:

- API gateway placement
- OAuth2 / OpenID Connect
- JWT bearer validation
- service-to-service security
- secrets management
- TLS everywhere
- authorization model

---

## Architectural rationale and trade-offs

### Why I chose this orchestration approach

I chose synchronous request-time aggregation for the external API because it keeps the contract simple for the caller: one request in, one aggregated response out.

Inside the request, the downstream calls are executed asynchronously so the API can fan out to multiple services in parallel instead of calling them sequentially. This gives a good balance between simplicity and performance for the size of this case study.

I added RabbitMQ as an asynchronous integration hook after successful aggregation. This keeps the client-facing flow straightforward while still demonstrating how the solution can evolve toward a more event-driven architecture.

This approach was chosen because it is:
- easy to explain
- small enough to implement cleanly
- realistic for a read-oriented aggregation use case
- extensible without changing the external API contract

### Trade-offs of the current solution

The current design intentionally favors clarity and incremental evolution over full production complexity.

Main trade-offs:

- **Synchronous API response path**
  - simple for clients
  - but request latency still depends on downstream services

- **Request-time aggregation instead of persisted read model**
  - easier to implement
  - but repeated reads can still trigger repeated aggregation work without a stronger caching strategy

- **In-memory cache**
  - simple and fast
  - but local to a single instance and not shared across replicas

- **RabbitMQ publisher without a full consumer pipeline**
  - demonstrates asynchronous extensibility
  - but does not yet provide a full event-driven read-model architecture

- **Resilience focused mainly on selected dependencies**
  - good enough for the case study
  - but a production system would require broader resilience policies and more operational tuning

### What would change under 10x load

Under significantly higher load, I would evolve the solution in several areas.

#### 1. Move from local in-memory cache to distributed caching
The current in-memory cache is instance-local. Under 10x load and multiple replicas, I would replace or complement it with a distributed cache such as Redis.

#### 2. Introduce a persisted read model
If the read traffic became much higher, request-time aggregation would become less efficient. I would introduce a materialized read model updated asynchronously from integration events, so the API could serve reads faster and with less fan-out pressure on downstream services.

#### 3. Scale horizontally
The aggregation API and downstream services would run with multiple replicas behind a load balancer or Kubernetes service.

#### 4. Strengthen RabbitMQ usage
At higher scale I would treat messaging more explicitly as infrastructure:
- durable topology management
- clearer event versioning
- dedicated consumers
- retry/dead-letter strategy
- potentially an outbox pattern for stronger delivery guarantees

#### 5. Improve observability and operational controls
Under higher traffic I would invest more in:
- dashboards
- alerts
- better health checks
- latency/error budgets
- tracing across all service boundaries

#### 6. Revisit connection and resource management
At higher scale I would optimize HTTP and messaging client lifecycle, tune concurrency, and validate backpressure behavior carefully.

### What I intentionally simplified

This solution intentionally does not implement every production concern in full depth.

I simplified the following areas on purpose:

- **No persisted read model yet**
  - aggregation is done at request time

- **RabbitMQ publisher only**
  - no downstream consumer service is implemented yet

- **Security is design-only**
  - authentication and authorization are described in `Auth.md`, not fully implemented in code

- **Local orchestration uses Docker Compose**
  - Kubernetes was considered a natural next step, but not implemented to keep the scope reasonable

- **Cache is in-memory only**
  - suitable for local/demo scope, not the final scaling model

- **Operational hardening is limited**
  - no full dead-letter handling, no outbox, no production-grade secret store integration, and no full CI/CD deployment setup

These simplifications were intentional so the solution could stay focused on the main case-study goals:
aggregation, resilience, messaging extension, observability, and testability.
---

## Future evolution

If this solution were extended further, the next natural steps would be:

- Kubernetes manifests or Helm charts
- persisted read model
- dedicated RabbitMQ consumers
- analytics or audit pipeline
- stronger health checks
- more end-to-end integration scenarios
- CI pipeline execution of integration tests
- MCP/server-style AI-facing interface over aggregated product data

---

## Repository structure

```text
AggregationService.API/
AggregationService.Application/
AggregationService.Domain/
AggregationService.Infrastructure/
AggregationService.Tests/
AggregationService.IntegrationTests/
ProductSimulation.API/
PricingSimulation.API/
StockSimulation.API/
docker-compose.yml
README.md
Auth.md
```

---

## Summary

This project demonstrates a practical .NET aggregation service with:

- asynchronous downstream orchestration
- resilience and partial response handling
- caching
- observability
- RabbitMQ integration event publishing
- containerized local runtime
- integration testing with realistic infrastructure

The solution is intentionally scoped to stay implementable while still showing clear paths toward a more complete cloud-native architecture.