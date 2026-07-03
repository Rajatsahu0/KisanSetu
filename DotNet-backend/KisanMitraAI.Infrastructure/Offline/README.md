# Offline Capability and Resilience

This module implements offline capability and resilience features for the Kisan Mitra AI platform, allowing farmers to continue using the platform even with intermittent network connectivity.

## Components

### 1. OfflineCacheService

Caches data for offline access using DynamoDB.

**Features:**
- Caches last 7 days of Mandi prices
- Enforces 50 MB cache size limit per farmer
- Automatic cache expiration and invalidation
- Filtered retrieval by commodity and location

**Usage:**
```csharp
var cacheService = serviceProvider.GetRequiredService<IOfflineCacheService>();

// Cache prices for offline access
await cacheService.CachePricesAsync(farmerId, prices);

// Retrieve cached prices when offline
var cachedPrices = await cacheService.GetCachedPricesAsync(
    farmerId, 
    commodity: "Wheat", 
    location: "Delhi");

// Check cache size
var cacheSize = await cacheService.GetCacheSizeAsync(farmerId);

// Invalidate cache
await cacheService.InvalidateCacheAsync(farmerId);
```

### 2. OfflineQueueService

Queues operations when offline for processing when connectivity returns.

**Features:**
- Queues voice queries, image uploads, and document uploads
- Stores operations in DynamoDB with retry metadata
- Automatic processing when connectivity returns
- Enforces 50 MB queue size limit per farmer
- Maximum 3 retry attempts per operation

**Usage:**
```csharp
var queueService = serviceProvider.GetRequiredService<IOfflineQueueService>();

// Enqueue operation when offline
var operation = new OfflineOperation(
    OperationId: Guid.NewGuid().ToString(),
    FarmerId: farmerId,
    OperationType: OfflineOperationType.VoiceQuery,
    Payload: audioData,
    QueuedAt: DateTimeOffset.UtcNow);

await queueService.EnqueueAsync(farmerId, operation);

// Process queue when connectivity returns
await queueService.ProcessQueueAsync(farmerId);

// Check queue status
var queueDepth = await queueService.GetQueueDepthAsync(farmerId);
var queueSize = await queueService.GetQueueSizeAsync(farmerId);
```

### 3. ConnectivityMonitor

Monitors network connectivity and notifies observers of status changes.

**Features:**
- Periodic connectivity checks (every 10 seconds)
- Observable pattern for status change notifications
- Identifies operations requiring connectivity
- Ping-based connectivity verification

**Usage:**
```csharp
var monitor = serviceProvider.GetRequiredService<IConnectivityMonitor>();

// Check current connectivity status
var isOnline = await monitor.IsOnlineAsync();
var status = await monitor.GetStatusAsync();

// Subscribe to connectivity changes
var subscription = monitor.ObserveConnectivityChanges()
    .Subscribe(status =>
    {
        if (status.IsOnline)
        {
            // Process queued operations
            await queueService.ProcessQueueAsync(farmerId);
        }
        else
        {
            // Show offline indicator
            Console.WriteLine("Offline mode");
        }
    });

// Check if operation requires connectivity
var requiresNetwork = monitor.RequiresConnectivity("VoiceQuery");
```

## Configuration

Add offline services to your dependency injection container:

```csharp
services.AddOfflineServices();
```

## DynamoDB Tables

### OfflineCache Table
- **Partition Key:** FarmerId (String)
- **Sort Key:** CacheKey (String)
- **Attributes:**
  - Data (String) - JSON serialized cached data
  - CachedAt (String) - ISO 8601 timestamp
  - ExpiresAt (Number) - Unix timestamp
  - SizeBytes (Number) - Size in bytes

### OfflineQueue Table
- **Partition Key:** FarmerId (String)
- **Sort Key:** OperationId (String)
- **Attributes:**
  - OperationType (String) - VoiceQuery, ImageUpload, DocumentUpload
  - OperationData (String) - JSON serialized operation payload
  - QueuedAt (String) - ISO 8601 timestamp
  - RetryCount (Number) - Number of retry attempts

## Correctness Properties

The offline capability implementation validates the following properties:

1. **Property 51:** Offline mode caches recent prices (last 7 days accessible)
2. **Property 52:** Offline operations are queued for later processing
3. **Property 53:** Sync occurs after reconnection (all queued items processed)
4. **Property 54:** Offline status is indicated to users
5. **Property 55:** Connectivity requirements are communicated
6. **Property 56:** Offline storage is bounded (50 MB limit enforced)

## Testing

Property-based tests are located in `tests/KisanMitraAI.Tests/Offline/OfflineCapabilityPropertyTests.cs`.

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~Offline"
```

## Error Handling

- **Cache size exceeded:** Automatically invalidates old cache to make room
- **Queue size exceeded:** Throws `InvalidOperationException`
- **Max retries exceeded:** Removes operation from queue after 3 failed attempts
- **Connectivity check failures:** Assumes offline and logs warning

## Performance Considerations

- Cache and queue operations use DynamoDB for persistence
- Connectivity checks run every 10 seconds (configurable)
- Ping timeout is 3 seconds
- Queue processing is sequential to maintain order
- Cache retrieval supports filtering to reduce data transfer

## Security

- All cached and queued data is encrypted at rest in DynamoDB
- Farmer data isolation is enforced (farmers can only access their own cache/queue)
- No sensitive data is logged
- Queue operations are authenticated before processing
