# Sparc

![Sparc logo](./sparc.png)

Transport-agnostic .NET RPC for real-time applications.

## Quick start

```bash
dotnet add package Sparc
```

Define a client contract to call operations on the client(s):

```csharp
using Sparc;

public interface IChatClient
{
  [Operation(1)]
  ValueTask MessageReceivedAsync(
    IClientConnection connection,
    string message,
    CancellationToken cancellationToken);

  // Use IEnumerable<IClientConnection> to efficiently broadcast a message by
  // only building the payload once and sending it to all connections
  [Operation(2)]
  ValueTask BroadcastMessageReceivedAsync(
    IEnumerable<IClientConnection> connection,
    int roomId,
    string message,
    CancellationToken cancellationToken);
}
```

Define a service and its operations:

```csharp
using Sparc;

public sealed class ChatService
{
  private readonly IChatClient _client;
  private readonly ChatConnection _someOtherConnection;

  public ChatService(IClientProxyFactory<IChatClient> clientFactory)
  {
    _client = _clientFactory.Create();
  }

  [Operation(1)]
  public ValueTask SendPrivateMessageAsync(
    ChatConnection connection,
    string message,
    CancellationToken cancellationToken)
  {
    return _client.MessageReceivedAsync(_someOtherConnection, message, cancellationToken);
  }

  [Operation(2)]
  public ValueTask SendPublicMessageAsync(
    ChatConnection connection,
    int roomId,
    string message,
    CancellationToken cancellationToken)
  {
    return _client.BroadcastMessageReceivedAsync(connection, roomId, message, cancellationToken);
  }
}
```

Register Sparc in DI container:

```csharp
using Sparc;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSparcService<ChatService, ChatConnection>();
services.AddSparcClient<IChatClient>();
```

Use the dispatcher in your transport layer:

```csharp
using Sparc;
using System.Net.WebSockets;
using System.Buffers.Binary;

public sealed class ChatConnection(WebSocket webSocket) : IClientConnection
{
  private readonly WebSocket _webSocket = webSocket;
  // Store other state like connection ID, name, ...

  public ValueTask SendAsync(ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
  {
    return _webSocket.SendAsync(
      payload,
      WebSocketMessageType.Binary,
      endOfMessage: true,
      cancellationToken);
  }
}

public sealed class ChatTransport(IServiceDispatcher<ChatService, ChatConnection> dispatcher)
{
  private readonly IServiceDispatcher<ChatService, ChatConnection> _dispatcher;

  public async Task HandleAsync(WebSocket webSocket, CancellationToken cancellationToken)
  {
    var connection = new ChatConnection(webSocket);
    while (!cancellationToken.IsCancellationRequested)
    {
      byte[] messageFrame = await ReceiveMessageAsync(webSocket);

      // NOTE: In a real transport layer, you'd want to read the operation ID first,
      // get a maximum message size (for security) and the initial buffer size (for
      // performance) for the rest of the payload, and then continue reading the rest
      var operationId = BinaryPrimitives.ReadInt32LittleEndian(messageFrame.AsSpan(0, sizeof(int)));
      var payload = messageFrame.AsMemory(sizeof(int));
      await dispatcher.DispatchAsync(operationId, payload, connection, cancellationToken);
    }
  }
}
```

## Real-time application focus

Sparc is built for predictable request latency and low GC pressure, all while
being as flexible and unopinionated as possible.

- Hot paths are tuned for zero allocations
- Client proxy and service dispatcher code is generated at runtime
- Broadcast serializes once and reuses payload bytes for multiple clients
- Transport type is your choice (WebSockets, TCP, ...)

Note: Certain parameter types (e.g. strings, arrays, dictionaries, ...) are still allocated by nature of the type/format.

## Metrics

Sparc records metrics for inbound and outbound payload sizes for each contract and operation.
These allow you to tune buffer sizes for optimal allocation/pooling efficiency and also monitor
use (or misuse) of your application.

These metrics are enabled by default because .NET enables all metrics by default. They do incur
overhead of about 100ns on my machine; so if this unacceptable for you, you can disable them as follows:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;

services.AddSparc();
services.Configure<MetricsOptions>(options => options.DisableMetrics("Sparc"));
```

This alone would not fix it, so Sparc reads the metrics rules at startup and does
not even create the instrument if it is disabled. It then also avoids construction
of a `TagList` and some logic inside the instrument's code.

## Performance tuning

### Initial buffer size for outbound RPC

Sparc pools memory to avoid allocations when constructing payloads sent to clients.
It automatically grows the buffer as needed, but this is inefficient if it happens
too often.

You are thus encouraged to tune the initial buffer size in a way it only rents memory
once for most sent payloads of any given operation.

You can do it by observing the exported `rpc.operation.payload.size{kind="outbound"}`
histogram, choosing e.g. p95 payload size and using that for the `InitialBufferSize`
property on the `[Operation]` attribute as follows:

```csharp
public interface IChatClient
{
  [Operation(1, InitialBufferSize = 512)]
  ValueTask MessageReceivedAsync(
    IClientConnection connection,
    string message,
    CancellationToken cancellationToken);
}
```

It is also recommended to use as little pool buckets as possible, so instead of exactly
matching the value you get for e.g. p95, try to find a similar value your application
already uses (Sparc for example uses 256 by default for all operations) and just use that.

## Wire format

Default wire format is binary, little-endian.

- `int`, `long`, `float`, `double`, etc: fixed-size little-endian
- `bool`: 1 byte
- `string`: length prefix (`int32`, little-endian) + UTF-8 bytes
- `char`: UTF-8 encoded scalar
- `DateTime`, `DateTimeOffset`, `Guid`: UTF-8 text format
- `TimeSpan`: 64-bit integer milliseconds
- `T[]`, `List<T>`, `Dictionary<TKey, TValue>`, `Nullable<T>`: length/presence-prefixed container format

Supported default parameter types include:

- `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `Int128`, `UInt128`, `float`, `double`, `decimal`
- `bool`, `char`, `string`
- `Guid`, `DateTime`, `DateTimeOffset`, `TimeSpan`
- `T[]`, `List<T>`, `Dictionary<TKey,TValue>`, `Nullable<T>`

You are still required to do high-level validation of the parameters, like range checks.

## Benchmarks

Benchmarks were created with the excellent BenchmarkDotNet library.

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8037/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 3950X 3.49GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.101
  [Host]     : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.1 (10.0.1, 10.0.125.57005), X64 RyuJIT x86-64-v3
```

### Dispatch (metrics disabled for fair comparison)

| Method                                 | Mean      | Ratio | Allocated |
|--------------------------------------- |----------:|------:|----------:|
| BaselineDecodeInvokeZeroParameters     |  1.450 ns |  1.00 |         - |
| DispatchZeroParameters                 |  8.733 ns |  6.03 |         - |
| BaselineDecodeInvokeOneParameter       |  3.068 ns |  2.12 |         - |
| DispatchOneParameter                   | 12.856 ns |  8.87 |         - |
| BaselineDecodeInvokeMultipleParameters |  7.154 ns |  4.94 |         - |
| DispatchMultipleParameters             | 15.816 ns | 10.91 |         - |

The baseline methods decode the parameters and dispatch directly.

### Proxy (metrics disabled for fair comparison)

| Method                          | Mean     | Ratio | Allocated |
|-------------------------------- |---------:|------:|----------:|
| ProxyBaselineZeroParameters     | 18.37 ns |  1.00 |         - |
| ProxyZeroParameters             | 18.72 ns |  1.02 |         - |
| ProxyBaselineOneParameter       | 24.07 ns |  1.31 |         - |
| ProxyOneParameter               | 21.48 ns |  1.17 |         - |
| ProxyBaselineMultipleParameters | 27.92 ns |  1.52 |         - |
| ProxyMultipleParameters         | 29.39 ns |  1.60 |         - |

The baseline methods encode the parameters and send the payload directly.

### Broadcast (metrics enabled)

| Method                     | ClientCount | SyncCompletionPercent | Mean          | Allocated |
|--------------------------- |------------ |---------------------- |--------------:|----------:|
| BroadcastToManyConnections | 1000        | 0                     |    174.766 μs |         - |
| BroadcastToManyConnections | 1000        | 25                    |    134.539 μs |         - |
| BroadcastToManyConnections | 1000        | 50                    |     97.560 μs |         - |
| BroadcastToManyConnections | 1000        | 100                   |      4.415 μs |         - |
| BroadcastToManyConnections | 10000       | 0                     |  1,621.930 μs |         - |
| BroadcastToManyConnections | 10000       | 25                    |  1,210.011 μs |         - |
| BroadcastToManyConnections | 10000       | 50                    |    842.753 μs |         - |
| BroadcastToManyConnections | 10000       | 100                   |     40.261 μs |         - |
| BroadcastToManyConnections | 100000      | 0                     | 15,893.082 μs |         - |
| BroadcastToManyConnections | 100000      | 25                    | 11,482.864 μs |         - |
| BroadcastToManyConnections | 100000      | 50                    |  8,316.387 μs |         - |
| BroadcastToManyConnections | 100000      | 100                   |    400.986 μs |         - |

`SyncCompletionPercent` controls how many send calls to the transport layer
complete synchronously. It is impossible to predict how exactly each individual
application will behave in this regard, so we just cover the extreme cases and
a couple cases in between.

The run times here are meaningless. They're just for ensuring overhead is minimal and finding regressions.
You should load test your actual transport layer to find out how well broadcasting scales for you.

## Plug custom readers/writers

You can add parameter readers/writers for custom parameter types:

```csharp
using Sparc.IO;
using System.Buffers.Binary;
using Microsoft.Extensions.DependencyInjection;

public sealed class JsonParameterReader<T> : IParameterReader<T>
{
  public T Read(ref PayloadReader reader)
  {
    // Probably also possible to read without length prefix by wrapping
    // reader.AvailableSpan in a ReadOnlySequence<byte> and using Utf8JsonReader.
    var lengthPrefix = BinaryPrimitives.ReadInt32LittleEndian(reader.Read(sizeof(int)));
    return JsonSerializer.Deserialize<T>(reader.Read(lengthPrefix));
  }
}

services.AddSingleton<IParameterReader<MyObject>, JsonParameterReader<MyObject>>();
```

## Override default readers/writers

You can also override Sparc's default parameter readers/writers by registering the
services prior to calling the DI extension methods from Sparc:

```csharp
using Sparc.IO;
using Microsoft.Extensions.DependencyInjection;

services.AddSingleton<IParameterReader<int>, CustomInt32ParameterReader>();
services.AddSingleton<IParameterWriter<int>, CustomInt32ParameterWriter>();
```

## Overriding readers/writers for specific container types

Sparc supports generic containers like arrays and dictionaries out of the box. However,
in select cases Sparc's one-size-fits-all solution is not the most efficient, as Sparc injects the
parameter readers/writers for the contained type(s) and calls their routines to grant
flexibility and composability.

One such case is byte arrays, as it will call a virtual method for every single byte.
I did not test it, but even if the JIT manages to devirtualize and inline the call,
you're still reading each byte one by one.

This is how you can roll your own implementation:

```csharp
using Sparc.IO;
using System.Buffers.Binary;
using Microsoft.Extensions.DependencyInjection;

// You can also deserialize into a view-like type if you can guarantee that the
// bytes are not used outside the target operation (like stored in a service field).
// Then you could avoid allocating the array here.
public sealed class ByteArrayReader : IParameterReader<byte[]>
{
  public byte[] Read(ref PayloadReader reader)
  {
    var arrayLength = BinaryPrimitives.ReadInt32LittleEndian(reader.Read(sizeof(int)));
    var array = new byte[arrayLength];
    reader.Read(arrayLength).CopyTo(array);
    return array;
  }
}

services.AddSingleton<IParameterReader<byte[]>, ByteArrayReader>();
```
This works because Sparc prioritizes closed generic implementations and only if
there is none will it fall back to open generic implementations.