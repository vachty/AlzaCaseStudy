# AUTH (DESIGN ONLY)

This solution does not implement full authentication/authorization because the assignment specifies security as design only. 
The current code focuses on aggregation, resilience, observability, and performance. 
In a production deployment, I would secure the system as follows:

## API behind API Gateway
The aggregation service would not be exposed directly to the public internet. 
Instead, it would sit behind an API gateway or ingress / nginx layer.

Responsibilities of the gateway:

- TLS termination
- request routing
- basic request filtering
- rate limiting
- forwarding trusted headers

## Authentication via OAuth2 / OpenID Connect
For production, I would use OAuth2 / OIDC with JWT bearer tokens.

- user-facing clients: Authorization Code Flow with PKCE
- service-to-service communication: Client Credentials Flow

For this project, the most realistic scenario is machine-to-machine access, 
where a trusted caller invokes aggregation service using an access token issued by an identity provider.

**Examples of suitable providers:**
- Microsoft Entra ID
- Keycloak

The API would validate bearer tokens and require a valid audience/scope 
before allowing access to `/api/v1/products/{productId}`.

**JWT bearer validation in the API**

The current API already has the standard ASP.NET Core middleware pipeline, so JWT validation could be added cleanly through:

authentication middleware
authorization middleware
[Authorize] on controllers/endpoints
That means the current codebase is structurally ready for:

AddAuthentication().AddJwtBearer(...)
AddAuthorization()
endpoint protection at controller level

In case I can provide code regarding JWT bearers in httponly cookies from my bachelor work,
which was also a paid project for the university.

## Service-to-service security
Currently, downstream simulator services are called over internal HTTP for simplicity. 
In production, I would not leave them open like that.

I would secure downstream calls using one of these approaches:

- internal network isolation + gateway-controlled access
- service-to-service access tokens

For this case study, the most realistic production evolution would be:

1) aggregation service receives a validated bearer token
2) API calls internal downstream services over a private network
3) optionally propagates service identity to downstream systems if needed

## WAF protection
would be placed in front of the public entry point.

Its role would be to mitigate:

- obvious malicious traffic
- common web attack patterns
- request anomalies
- volumetric abuse before traffic reaches the API
- Even though this API is relatively small and does not process browser forms or HTML, WAF still makes sense as part of a defense-in-depth approach.

## Secrets management
The current local setup uses configuration suitable for local development and Docker Compose. 
In production, I would not store sensitive configuration directly in files or images.

Secrets should be stored in a managed secret store such as:

- Azure Key Vault
- AWS Secrets Manager

This would apply to:

- downstream service credentials
- OAuth client secrets
- signing/audience configuration
- any connection secrets introduced later


The application should receive these through environment-specific secure configuration providers.

## TLS everywhere
For local development, plain internal HTTP may be acceptable. In production, I would require:

HTTPS at the public edge
encrypted traffic between gateway and API
encrypted traffic between services where required by environment/security posture
So the production target would be effectively TLS everywhere, with any HTTP-only setup limited to local development/test environments.

## Authorization model
If the API were exposed to multiple consumers, I would authorize access using:
- scopes
- roles
- audience restrictions