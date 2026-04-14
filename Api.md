# API documentation

The API exposes OpenAPI documentation in development mode.

After starting the application locally, use the API host on:

`http://localhost:5001/scalar`

OpenAPI/Scalar documentation is available from the development startup pipeline.

## Products
- exposes the main read endpoint of the aggregation service.

It provides a GET `/api/v1/products/{productId}` endpoint that accepts a product identifier, delegates the aggregation work to IProductAggregationService, and returns the aggregated product response.

Behavior
if the product is found, the endpoint returns HTTP 200 OK with the aggregated product payload
if the product does not exist, the endpoint returns HTTP 404 Not Found
the request supports CancellationToken, so downstream work can be cancelled if the client disconnects or the request is aborted
Responsibility
The controller is intentionally thin:

it does not contain business logic
it only handles HTTP concerns
all aggregation logic is delegated to the application service
This keeps the API layer simple and makes the core logic easier to test independently.

### GET /api/v1/products/{productId}

Returns an aggregated product view for the specified product ID.

#### Request
- method: `GET`
- path parameter: `productId`
- request body: none
- optional header: `X-Correlation-Id`

If the client provides `X-Correlation-Id`, the API uses it for request tracing and structured logging.  
If the header is not provided, the service generates a correlation identifier automatically.

Example:

```http
GET /api/v1/products/1
X-Correlation-Id: 7d1d6d65-6b15-4f22-8f77-6c1b71d7d101
```

#### Successful response

- status: 200 OK
- body: 

```JSON
{
  "productId": "1",
  "name": "Sample product",
  "price": {
    "amount": 199.99,
    "currency": "CZK"
  },
  "stock": {
    "available": true,
    "quantity": 12
  },
  "isDegraded": false,
  "degradedServices": []
}
```

##### Response headers
- X-Correlation-Id: correlation identifier associated with the request

#### Not found response
- status: 404 Not Found
- status: 500 Internal server error
