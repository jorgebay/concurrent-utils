//
//   Copyright (C) 2016 Jorge Bay Gondra
//
//   This software may be modified and distributed under the terms
//   of the MIT license.  See the LICENSE.txt file for details.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;

namespace ConcurrentUtils
{
    /// <summary>
    /// Provides a set of methods useful for concurrent programming.
    /// </summary>
    public static class ConcurrentUtils
    {
        /// <summary>
        /// Executes an asynchronous method n number of times, limiting the amount of operations in parallel without
        /// blocking.
        /// </summary>
        /// <param name="times">The amount of times to execute the async operation.</param>
        /// <param name="limit">The maximum amount of executions in parallel</param>
        /// <param name="method">The method to execute to obtain the asynchronous operation.</param>
        /// <returns>
        /// A Task that is completed when all Tasks are completed or is faulted when any of the Tasks
        /// transition to Faulted state.
        /// </returns>
        public static Task Times(long times, int limit, Func<long, Task> method)
        {
            var counter = new SendReceiveCounter();
            var tcs = new TaskCompletionSource<bool>();
            if (limit > times)
            {
                limit = (int) times;
            }
            for (var i = 0; i < limit; i++)
            {
                ExecuteOnceAndContinue(times, method, tcs, counter);
            }
            return tcs.Task;
        }

        /// <summary>
        /// Asynchronously projects each element of a sequence into a new form, limiting the amount of operations
        /// in parallel without blocking.
        /// </summary>
        /// <param name="source">An immutable sequence of values to invoke a async transform function on.</param>
        /// <param name="limit">The maximum amount of async transformations in parallel.</param>
        /// <param name="method">A transform function to apply to each element.</param>
        /// <returns>The transformed elements</returns>
        public static Task<TResult[]> Map<TSource, TResult>(
            IList<TSource> source, int limit, Func<TSource, Task<TResult>> method)
        {
            var counter = new SendReceiveCounter();
            var resultArray = new TResult[source.Count];
            var tcs = new TaskCompletionSource<TResult[]>();
            if (limit > source.Count)
            {
                limit = source.Count;
            }
            for (var i = 0; i < limit; i++)
            {
                MapOneByOne(source, resultArray, method, tcs, counter);
            }
            return tcs.Task;
        }

        /// <summary>
        /// Creates collection of objects to which apply the asynchronous method in a first-in first-out manner.
        /// <para>Items added to the queue are processed in parallel according to the given limit.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="limit">The maximum amount of async operations in parallel.</param>
        /// <param name="method">The method to process the queue item.</param>
        /// <returns>A <see cref="IJobQueue{T}"/> instance.</returns>
        public static IJobQueue<T> CreateQueue<T>(int limit, Func<T, Task> method)
        {
            return new JobQueue<T>(limit, method);
        }

        private static void MapOneByOne<TSource, TResult>(
            IList<TSource> source, 
            TResult[] resultArray, 
            Func<TSource, Task<TResult>> method, 
            TaskCompletionSource<TResult[]> tcs, 
            SendReceiveCounter counter)
        {
            var index = counter.IncrementSent() - 1L;
            if (index >= source.Count)
            {
                return;
            }
            var t1 = method(source[(int)index]);
            t1.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    tcs.TrySetException(t.Exception.InnerException);
                    return;
                }
                var received = counter.IncrementReceived();
                resultArray[index] = t.Result;
                if (received == source.Count)
                {
                    tcs.TrySetResult(resultArray);
                    return;
                }
                MapOneByOne(source, resultArray, method, tcs, counter);
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        private static void ExecuteOnceAndContinue(long times, Func<long, Task> method, TaskCompletionSource<bool> tcs, SendReceiveCounter counter)
        {
            var index = counter.IncrementSent() - 1L;
            if (index >= times)
            {
                return;
            }
            var t1 = method(index);
            t1.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    tcs.TrySetException(t.Exception.InnerException);
                    return;
                }
                var received = counter.IncrementReceived();
                if (received == times)
                {
                    tcs.TrySetResult(true);
                    return;
                }
                ExecuteOnceAndContinue(times, method, tcs, counter);
            }, TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}
