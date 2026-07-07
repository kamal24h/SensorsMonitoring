# SensorsMonitoring

A .NET 8 solution for importing, validating, storing and aggregating sensor readings.

This project was developed as a solution for a backend technical assessment. The implementation focuses on clean architecture, domain-driven design principles, performance, maintainability and extensibility.

---

## Features

* Import sensor readings from a JSON Lines (`.jsonl`) file
* Stream file processing with low memory usage
* Domain-level validation
* Duplicate detection using composite identity
* Persistent storage using Entity Framework Core
* Aggregated statistics over configurable time buckets
* Processing statistics
* Dependency Injection
* Structured logging
* Unit-test friendly architecture

---

## Architecture

The solution follows a layered architecture inspired by Clean Architecture and Domain-Driven Design.

### Domain-Driven Design (DDD)
- **Domain Layer**: Contains business logic (deduplication, validation) independent of infrastructure
- **Application Layer**: Orchestrates use cases (file processing, aggregation)
- **Infrastructure Layer**: Handles data persistence and external dependencies
- **WebApi Layer**: Exposes HTTP endpoints

```
UnitTest
        │
WebApi
        │
Application
        │
Domain
        │
Infrastructure
```

### Domain

Contains the business model and business rules.

* Reading
* ReadingIdentity
* ReadingImportSession
* ProcessedStats
* Domain Exceptions

The domain layer has no dependency on infrastructure.

---

### Application

Contains the application use cases.

* ReadSensorDataService
* Aggregation Strategy
* DTOs
* Interfaces

Responsibilities include:

* Reading input files
* Creating domain entities
* Coordinating repositories
* Returning DTOs

---

### Infrastructure

Contains persistence and framework-specific implementations.

* Entity Framework Core
* Repository implementation
* DbContext
* Logging configuration

---

## Duplicate Detection

Each reading is uniquely identified by:

```
(DeviceId, Metric, Timestamp, Sequence)
```

Duplicate detection is performed during the import process using a `HashSet<ReadingIdentity>`.

Only unique readings are persisted.

---

## Validation

Validation is implemented inside the domain entity.

Rules include:

* DeviceId is required
* Metric is required
* Sequence must be non-negative
* Sensor value must be within the accepted range
* Invalid JSON records are ignored

Invalid records are counted but do not stop the import process.

---

### Tests

### Unit Tests

1. Unit test for testing the app to check if duplication prevention works properly.
2. Unit test for testing the app to check if aggregation works properly.
3. Unit test for testing the app to check if aggregation strategy works properly.
4. Unit test for testing the app to check if reading validations works properly.
5. test is developed using Moq third-party library.

---


## File Processing

The import process uses

```csharp
File.ReadLines(...)
```

which reads the file instead of loading it entirely into memory.

This allows processing very large files with limited memory usage.

---

## Aggregation

Sensor readings can be aggregated using configurable bucket sizes.

Supported metrics:

* Count
* Average
* Minimum
* Maximum

Buckets are generated according to the interval:

```
[from, to)
```

which means:

* from is inclusive
* to is exclusive

---

## Extensibility

### Extensibility Points
1. **Aggregation Logic**: is separated behind the `IAggregationStrategy` interface.
2. **New Aggregation Algorithms**: (for example Median, Standard Deviation or Percentile)
      only requires implementing a new strategy without modifying the application service.
3. **New Data Sources**: Implement new repository interfaces
4. **New Validation Rules**: Extend domain validation logic

---

## Technologies

* .NET 8
* ASP.NET Core
* Entity Framework Core
* InMemory
* Microsoft Dependency Injection
* Microsoft.Extensions.Logging
* xUnit
* FluentAssertions

---

## Running the project

Restore packages

```
dotnet restore
```

Run the application

```
dotnet run
```

Run tests

```
dotnet test
```

---

## Project Structure

```
SensorMonitoring
 ├── Application
 ├── Domain
 ├── Infrastructure
 ├── WebApi
 └── UnitTests.Tests
```
---

## Design Decisions

### Why use File.ReadLines?

Streaming keeps memory usage nearly constant regardless of file size.

---

### Why validate inside the domain?

Business rules belong to the domain model.

This prevents invalid entities from ever being created.

---

### Why use a composite identity?

The task defines uniqueness using:

* DeviceId
* Metric
* Timestamp
* Sequence

Representing this identity as a dedicated value object makes duplicate detection explicit and reliable.

---

### Why Strategy Pattern?

Aggregation algorithms are expected to evolve.

Separating aggregation behind an interface keeps the service closed for modification but open for extension.

---

## Logging

Structured logging is implemented using `ILogger<T>`.

Major events include:

* Import started
* Import completed
* Invalid records
* Aggregation requests
* Errors

---

## Future Improvements

* Parallel file processing
* Additional Services (such as Anomaly detection among sensor data)
* Batch database inserts
* Additional aggregation algorithms
* Pagination for large query results
* Metrics endpoint
* Docker support
* BenchmarkDotNet performance benchmarks

---

## AI Usage Statement

Generative AI tools were used during the development of this project as a technical review and documentation assistant.

The assistance included:

* Reviewing code quality and adherence to SOLID and DDD principles
* Suggesting refactoring opportunities
* Improving documentation and project presentation

AI was **not** used as an autonomous code generator. Every suggested change was manually reviewed, implemented, tested and, where appropriate, modified to fit the project's architecture and the technical requirements.

I take full responsibility for the final design, implementation and correctness of the solution.


## Author

Kamal Bastani

Senior Software Engineer / Full Stack .NET Developer
