# eShopOnContainers

eShopOnContainers is a sample application written in C# running on .NET Core using a microservice architecture, Domain Driven Design.

> .NET Core reference application, powered by Microsoft, based on a simplified microservices architecture and Docker containers.

> This reference application is cross-platform at the server and client side, thanks to .NET Core services capable of running on Linux or Windows containers depending on your Docker host, and to Xamarin for mobile apps running on Android, iOS or Windows/UWP plus any browser for the client web apps.

> The architecture proposes a microservice oriented architecture implementation with multiple autonomous microservices (each one owning its own data/db) and implementing different approaches within each microservice (simple CRUD vs. DDD/CQRS patterns) using Http as the communication protocol between the client apps and the microservices and supports asynchronous communication for data updates propagation across multiple services based on Integration Events and an Event Bus (a light message broker, to choose between RabbitMQ or Azure Service Bus, underneath) plus other features defined at the roadmap.

## eShopOnContainers with CAP

You can see how to use caps in eShopOnContainers at the Github repository.

https://github.com/yang-xiaodong/eShopOnContainers