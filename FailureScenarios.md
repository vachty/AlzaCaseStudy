## Failure scenarios

### 1. A downstream service is slow or unavailable

#### Example
The pricing or stock service does not respond in time, returns errors, or is temporarily unavailable.

#### Impact
Without protection, the aggregation endpoint could become slow or fail completely even when partial data would still be useful.

#### Current handling
The solution executes downstream calls asynchronously and applies resilience to selected dependencies. If a non-critical dependency fails, the API can still return a partial aggregated response and mark the response as degraded.

This keeps the API useful even during partial dependency outages.

#### Production evolution
For a production-grade system, I would extend this with:
- broader timeout and retry coverage across all relevant downstream clients
- clearer fallback policies per dependency
- circuit breaker tuning based on real traffic
- health-aware routing and monitoring

---

### 2. RabbitMQ is temporarily unavailable

#### Example
The aggregation request succeeds, but the broker is restarting, unreachable, or not ready to accept connections.

#### Impact
The client-facing API may still be able to build the aggregated response, but event publication could fail.

#### Current handling
The messaging layer includes retry behavior to handle transient broker startup and connectivity problems, which is especially useful in local containerized environments and integration tests.

#### Production evolution
For a production system, I would make this more robust with:
- stronger retry and backoff policies
- dead-letter handling where appropriate
- durable topology management
- publisher confirms if stronger guarantees are required
- potentially an outbox pattern to avoid losing events when the HTTP request succeeds but event publication fails

---

### 3. Load increases and many requests miss the cache at once

#### Example
A traffic spike hits the same or many products at the same time, while the cache is cold or expired.

#### Impact
The aggregation service may produce a burst of fan-out traffic to downstream services, increasing latency and pressure on dependencies.

#### Current handling
The current solution uses in-memory caching to reduce repeated downstream calls and improve repeated-read performance.

#### Production evolution
Under higher load, I would improve this with:
- distributed caching
- better cache invalidation strategy
- replica scaling
- precomputed read models for hot paths
- request collapsing or stronger cache coordination if necessary