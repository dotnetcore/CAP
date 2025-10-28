# eShopOnContainers

eShopOnContainers is a sample application written in C# running on .NET Core that uses a microservice architecture and Domain Driven Design.

> A .NET Core reference application powered by Microsoft, based on a simplified microservices architecture with Docker containers.

> This reference application is cross-platform on both server and client sides, thanks to .NET Core services that can run on Linux or Windows containers depending on your Docker host, and Xamarin for mobile apps running on Android, iOS, or Windows/UWP, plus any browser for client web apps.

> The architecture demonstrates a microservice-oriented implementation with multiple autonomous microservices (each owning its own data/database) and implementing different approaches within each microservice (simple CRUD vs. DDD/CQRS patterns). It uses HTTP as the communication protocol between client apps and microservices, and supports asynchronous communication for data propagation across services based on Integration Events and an Event Bus (a lightweight message broker that you can choose between RabbitMQ or Azure Service Bus) plus other features in the roadmap.

## eShopOnContainers with CAP

You can see how to use CAP in eShopOnContainers in the GitHub repository:

https://github.com/yang-xiaodong/eShopOnContainers