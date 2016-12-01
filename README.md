# C# Concurrent Utilities

Provides classes and methods useful in concurrent programming.

## Installation

[Get it on Nuget][nuget]

```
PM> Install-Package ConcurrentUtils
```

[![Build Status](https://travis-ci.org/jorgebay/concurrent-utils.svg?branch=master)](https://travis-ci.org/jorgebay/concurrent-utils)

## Functionalities

### `Times(long times, int limit, Func<long, Task> method)`

Executes an asynchronous method n number of times, limiting the amount of operations in parallel without blocking.

Returns a `Task` that is completed when all Tasks are completed or is faulted when any of the Tasks transition to
faulted state.

Suitable for benchmarking asynchronous methods with different maximum amount of parallel operations.

**Example**

```csharp
// Execute MyMethodAsync 1,000,000 times limiting the maximum amount of parallel async operations to 512
await ConcurrentUtils.Times(1000000, 512, (index) => MyMethodAsync());
```

### `Map(IList<TSource> source, int limit, Func<TSource, Task<TResult>> method)`

Asynchronously projects each element of a sequence into a new form, limiting the amount of operations in parallel
without blocking.

Returns a `Task` that gets completed with the transformed elements or faulted when any of the transformation
operations transition to faulted state.

**Example** 

```csharp
var urls = new []
{
    "https://www.google.com/",
    "https://www.microsoft.com/net/core",
    "https://www.nuget.org/",
    "https://dotnet.github.io/"
};
// Asynchronously get the http response of each url limiting
// the maximum amount of parallel http requests to 2
string[] responses = await ConcurrentUtils.Map(urls, 2, url => client.GetStringAsync(url));
```

### `CreateQueue<T>(int limit, Func<T, Task> method)`

Creates collection of objects to which apply the asynchronous method in a first-in first-out manner. Items added to
the queue are processed in parallel according to the given limit.

Returns a `IJobQueue<T>` instance that can be used to enqueue items.

**Example** 

```csharp
// Create the queue providing the method that is going to be used to asynchronously process each item
// and the max amount of parallel operations (in this case 2)
IJobQueue<string> jobQueue = ConcurrentUtils.CreateQueue(2, url => client.GetStringAsync(url));
// Add items to the queue that are going to be processed
Task t1 = jobQueue.Enqueue("https://www.google.com/");
Task t2 = jobQueue.Enqueue("https://www.microsoft.com/net/core");
Task t3 = jobQueue.Enqueue("https://www.nuget.org/");
Task t4 = jobQueue.Enqueue("https://dotnet.github.io/");
// Items are processed as FIFO a queue, without exceeding the max amount of parallel operations limit
await Task.WhenAll(t1, t2, t3, t4);
```

## License

Copyright (C) 2016 Jorge Bay Gondra

This software may be modified and distributed under the terms
of the MIT license.  See the LICENSE file for details.

[nuget]: https://nuget.org/packages/ConcurrentUtils
