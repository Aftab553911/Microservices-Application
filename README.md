Microservices Application
Event-Driven Food Delivery Platform (Production-Grade Architecture)
📌 Project Overview

This project is a distributed microservices-based food ordering system built using:

.NET 8

PostgreSQL

Apache Kafka

Docker & Docker Compose

JWT Authentication

YARP API Gateway

Saga Pattern (Choreography)

Retry & Dead Letter Queue (DLQ)

Idempotent Consumers

Role-Based Authorization

The system demonstrates a real-world enterprise backend architecture focusing on:

Loose coupling

Scalability

Fault tolerance

Distributed consistency

Secure API access

Event-driven communication

🏗️ Architecture Overview

The system follows Event-Driven Microservices Architecture using Kafka for inter-service communication.

🔹 Services
Service	Responsibility
API Gateway	Single entry point + JWT validation + rate limiting
Auth Service	User registration, login, access/refresh tokens
Order Service	Create orders, manage order lifecycle
Payment Service	Process payments
Notification Service	Generate user notifications
🔄 High-Level Flow
Client → API Gateway → Order Service
            ↓
       order.created (Kafka)
            ↓
       Payment Service
            ↓
   payment.completed / payment.failed
            ↓
   Order Service (compensation)
            ↓
       order.cancelled
            ↓
   Notification Service


This implements a Choreography Saga Pattern for distributed transaction management.

🔐 Authentication & Authorization
Implemented Features:

JWT Access Tokens (15 min expiry)

Refresh Tokens (30 days)

Role-Based Authorization

Gateway-level token validation

Service-level token validation

Rate limiting at API Gateway

Roles:

Admin

Customer

📦 Event-Driven Design

The system uses Kafka topics for asynchronous communication.

📢 Events & Topics
Event	Topic	Produced By	Consumed By
Order Created	order.created	Order Service	Payment Service
Payment Completed	payment.completed	Payment Service	Notification Service
Payment Failed	payment.failed	Payment Service	Order + Notification
Order Cancelled	order.cancelled	Order Service	Notification Service
Payment DLQ	payment.failed.dlq	Payment Service	(Monitoring)
Notification DLQ	notification.failed.dlq	Notification Service	(Monitoring)
🧠 Why Events Are Published After DB Save

We follow this strict pattern:

1️⃣ Save data to database (source of truth)
2️⃣ Publish event


This prevents inconsistencies in case of system crash.

This follows a simplified version of the Transactional Outbox Pattern.

🛡️ Reliability Features
✅ 1. Idempotent Consumers

Kafka guarantees at-least-once delivery, meaning duplicate events may occur.

To handle this:

ProcessedEvents table

In-memory cache

Event key tracking

This ensures safe duplicate handling.

🔁 2. Retry Mechanism

If event processing fails:

Retry up to 3 times

Retry count stored in Kafka headers

☠ 3. Dead Letter Queue (DLQ)

After retry exhaustion:

Event moved to .dlq topic

Prevents infinite retry loops

Enables operational monitoring

🔄 4. Saga Compensation

Since distributed transactions are not supported:

If payment fails:

Order → Cancelled


This ensures business consistency without distributed locks.

🗄️ Databases

Each service owns its own database.

Service	Database
Auth	auth_db
Order	order_db
Payment	payment_db
Notification	notification_db

This enforces Database per Service pattern.

🧩 Dockerized Infrastructure

The system runs fully containerized:

Kafka

Zookeeper

PostgreSQL (per service)

All microservices

API Gateway

Run everything:

docker-compose up --build

🌐 API Gateway

Built using:

YARP Reverse Proxy

JWT validation middleware

Rate limiting

It ensures:

Single entry point

Security enforcement

Request routing

Traffic control

⚙️ Key Architectural Patterns Used
Pattern	Why Used
Microservices	Independent scaling & ownership
Event-Driven Architecture	Loose coupling
Saga Pattern	Distributed consistency
Idempotency	Safe duplicate handling
Retry + DLQ	Fault tolerance
API Gateway	Centralized access control
Database per Service	Isolation
JWT Auth	Stateless authentication
📈 Scalability Design

Kafka allows:

Horizontal scaling of consumers

Partition-based load distribution

Replay capability

Backpressure handling

Services can scale independently.

🧪 Testing Strategy

To test full flow:

Register user

Login → Get access token

Call /orders via API Gateway

Observe:

Order DB entry

Payment DB entry

Notification DB entry

Force payment failure → Observe compensation

📚 What This Project Demonstrates

This project demonstrates knowledge of:

Distributed system design

Production reliability

Kafka-based messaging

Authentication architecture

Microservice communication

Fault handling

Saga-based compensation

Real-world backend scalability

🚀 Future Improvements

Transactional Outbox Pattern

Observability (OpenTelemetry + Jaeger)

Centralized logging

Kubernetes deployment

CI/CD pipeline

Kafka partition tuning

Monitoring DLQ consumer

Metrics & dashboards

🎯 Learning Outcomes

This project teaches:

When to use async messaging

How to design reliable event consumers

Why idempotency matters

How distributed transactions work

How to implement compensation logic

How to secure microservices properly

How to think like a backend architect

📌 Final Architecture Philosophy

Services should not know about each other directly.
They react to events.
Events represent facts.
State changes drive the system.
Reliability is designed — not assumed.




▶️ Running the Project
📋 Prerequisites

Make sure the following are installed:

Docker Desktop (latest)

.NET 8 SDK

EF Core CLI

dotnet tool install --global dotnet-ef

🚀 First-Time Setup (Clean Environment)

This should be used when running the project for the first time.

1️⃣ Clone Repository
git clone <repository-url>
cd Microservices-Application

2️⃣ Build and Start All Containers
docker-compose up --build


This will start:

Kafka

Zookeeper

PostgreSQL (4 instances)

API Gateway

Auth Service

Order Service

Payment Service

Notification Service

3️⃣ Run Database Migrations

Open separate terminals for each service and run:

Auth Service
cd auth-service/AuthService
dotnet ef database update

Order Service
cd order-service/OrderService
dotnet ef database update

Payment Service
cd payment-service/PaymentService
dotnet ef database update

Notification Service
cd notification-service/NotificationService
dotnet ef database update

4️⃣ Verify Kafka Topics (Optional)

Kafka topics are auto-created when first event is published.

You can verify inside Kafka container:

docker exec -it <kafka-container-name> bash

kafka-topics --bootstrap-server localhost:9092 --list

5️⃣ Test the System

Register User

Login → Get access token

Call:

POST http://localhost:5000/orders


Observe logs and DB entries.

🔁 Running Project Next Time (Normal Restart)

If everything is already set up:

docker-compose up


No need to run migrations again unless schema changed.

🔄 Full Reset (Clean Rebuild)

If you want to completely reset everything:

⚠ This will delete all database data.

docker-compose down -v
docker-compose up --build


Then re-run migrations.

🛑 Stop All Services
docker-compose down

🧹 Clean Docker System (Advanced)

If volumes or containers become inconsistent:

docker system prune -a
docker volume prune


⚠ Use carefully.