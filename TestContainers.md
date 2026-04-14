# Test Containers implementation

The implementation of tests containers in integration tests project aims to be a simple and reusable way to create and manage test containers for different services, 
such as databases, message brokers, or any other dependencies required for integration testing.

It uses simple container configuration and one RabbitMq builder. 
However in real scenario I have used more complex configuration which consists of:

- Pulling images of required services from AWS ECR via AWS ECR SDK. 
Because of VPN and whitelisting on AWS ECR, the solution also uses VPN kit to enable pulling via VPN as the VPN is not available for the WSL.
To note the aim was to provide solution suitable for both Podman machine and Docker.

- Orchestration
- Overriding configuration of the services in the containers using environment variables to ensure all services can communicate with each other (if it is their demand).
- A developer who implements it in his own tests can easily choose which services to "testcontainerize" and simply configure it via fluent api.
- That solution also provides support for Mongo container and Postgres container
- It also provides support for running tests in pipelines so the results of tests are avaiable when doing Pull request.
- Reads configuration from secrets to avoid hardcoding credentials in test app settings

However this case study aims to be simple aggregation project I have decided to provide only simple implementation of test containers for RabbitMQ as an example.