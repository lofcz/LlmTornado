# LlmTornado Agents API Usage Examples

This document provides examples of how to use the LlmTornado Agents API to communicate with ChatRuntime instances and handle streaming events.

## API Endpoints

### Base URL
- Development: `http://localhost:5036`

### Swagger Documentation
- Swagger UI: `http://localhost:5036/` (redirects to Swagger UI)
- Swagger JSON: `http://localhost:5036/swagger/v1/swagger.json`

## Core API Operations

### 1. Create a ChatRuntime Instance

```bash
curl -X POST http://localhost:5036/api/chatruntime/create \
  -H "Content-Type: application/json" \
  -d '{
    "agentName": "MyAssistant",
    "instructions": "You are a helpful AI assistant that provides clear and concise answers.",
    "enableStreaming": true,
    "configurationType": "simple"
  }'
```

**Response:**
```json
{
  "runtimeId": "e262802c-fef5-4364-99db-928a7dc74adb",
  "status": "created"
}
```

### 2. Send a Message to a Runtime

```bash
RUNTIME_ID="e262802c-fef5-4364-99db-928a7dc74adb"

curl -X POST "http://localhost:5036/api/chatruntime/$RUNTIME_ID/message" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Hello, how can you help me today?",
    "role": "user",
    "enableStreaming": true
  }'
```

**Response:**
```json
{
  "content": "Echo: Hello, how can you help me today?",
  "role": "Assistant",
  "requestId": "d48255f9-b280-48a4-8844-ffc97781f3d2",
  "isStreamed": true
}
```

### 3. Get Runtime Status

```bash
curl -X GET "http://localhost:5036/api/chatruntime/$RUNTIME_ID/status"
```

**Response:**
```json
{
  "runtimeId": "e262802c-fef5-4364-99db-928a7dc74adb",
  "status": "active",
  "streamingEnabled": true,
  "messageCount": 2
}
```

### 4. List All Active Runtimes

```bash
curl -X GET "http://localhost:5036/api/chatruntime/list"
```

**Response:**
```json
[
  "e262802c-fef5-4364-99db-928a7dc74adb",
  "another-runtime-id-here"
]
```

### 5. Cancel Runtime Execution

```bash
curl -X POST "http://localhost:5036/api/chatruntime/$RUNTIME_ID/cancel"
```

**Response:**
```json
{
  "message": "Runtime execution cancelled"
}
```

### 6. Remove a Runtime

```bash
curl -X DELETE "http://localhost:5036/api/chatruntime/$RUNTIME_ID"
```

**Response:** `204 No Content`

## SignalR Streaming Events

The API supports real-time streaming events via SignalR. Connect to the hub at:

**Hub URL:** `http://localhost:5036/hub/chatruntime`

### JavaScript Client Example

```javascript
// Install @microsoft/signalr package first
import { HubConnectionBuilder } from '@microsoft/signalr';

const connection = new HubConnectionBuilder()
    .withUrl("http://localhost:5036/hub/chatruntime")
    .build();

// Subscribe to a specific runtime
const runtimeId = "your-runtime-id-here";
await connection.start();
await connection.invoke("SubscribeToRuntime", runtimeId);

// Listen for streaming events
connection.on("ReceiveStreamingEvent", (event) => {
    console.log("Received event:", event);
    // Event structure:
    // {
    //   eventType: "MessageReceived" | "MessageResponse" | "RuntimeCancelled",
    //   sequenceNumber: 1,
    //   data: { ... },
    //   timestamp: "2024-01-01T12:00:00.000Z",
    //   runtimeId: "runtime-id"
    // }
});

// Unsubscribe when done
await connection.invoke("UnsubscribeFromRuntime", runtimeId);
```

### C# Client Example

```csharp
using Microsoft.AspNetCore.SignalR.Client;

var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5036/hub/chatruntime")
    .Build();

await connection.StartAsync();

// Subscribe to runtime events
await connection.InvokeAsync("SubscribeToRuntime", runtimeId);

// Handle streaming events
connection.On<StreamingEventResponse>("ReceiveStreamingEvent", (eventResponse) =>
{
    Console.WriteLine($"Event: {eventResponse.EventType}");
    Console.WriteLine($"Data: {eventResponse.Data}");
});

// Unsubscribe when done
await connection.InvokeAsync("UnsubscribeFromRuntime", runtimeId);
await connection.DisposeAsync();
```

## Event Types

The following streaming events are supported:

- **MessageReceived**: Fired when a user message is received
- **MessageResponse**: Fired when the agent responds
- **RuntimeCancelled**: Fired when runtime execution is cancelled
- **RuntimeStarted**: Fired when runtime processing begins
- **RuntimeCompleted**: Fired when runtime processing completes

## Error Handling

All endpoints return appropriate HTTP status codes:

- `200 OK`: Successful operation
- `201 Created`: Resource created successfully  
- `204 No Content`: Successful deletion
- `400 Bad Request`: Invalid request data
- `404 Not Found`: Runtime not found
- `500 Internal Server Error`: Server error

Error responses include details:

```json
{
  "error": "Runtime not found",
  "details": "Runtime with ID xyz not found"
}
```

## CORS Support

The API includes CORS support for web applications. In production, configure CORS policies appropriately for your domain.

## Authentication

Currently, the API does not require authentication. In production environments, implement appropriate authentication and authorization mechanisms.