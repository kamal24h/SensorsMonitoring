# SensorsMonitoring

An Application for sensors data aggregation and monitoring reports

# Sensor Monitoring System

## Architecture Decisions

### Domain-Driven Design (DDD)
- **Domain Layer**: Contains business logic (deduplication, validation) independent of infrastructure
- **Application Layer**: Orchestrates use cases (file processing, aggregation)
- **Infrastructure Layer**: Handles data persistence and external dependencies
- **API Layer**: Exposes HTTP endpoints

### Technology Choices
- **.NET 8**: Latest LTS version with performance improvements
- **In-Memory Database**: Chosen for simplicity and demonstration (would use MS-SQL Server or PostgreSQL in production)
- **Entity Framework Core**: For data access abstraction

### Deduplication Strategy
- Readings are considered duplicates when `(deviceId, metric, timestamp, sequence)` match
- First occurrence is stored, subsequent duplicates are rejected and counted
- This strategy preserves data integrity while handling out-of-order arrivals

### Performance Considerations

### Extensibility Points
1. **New Aggregation Types**: Extend aggregation logic in Application layer
2. **New Data Sources**: Implement new repository interfaces
3. **New Validation Rules**: Extend domain validation logic

### Tests


### Unit Tests

1. Unit test for testing the app for duplication prevent works properly.
