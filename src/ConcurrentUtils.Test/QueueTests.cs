using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
// ReSharper disable AccessToModifiedClosure

namespace ConcurrentUtils.Test
{
    [TestFixture]
    public class QueueTests : BaseUnitTest
    {
        [Test, TestCase(9), TestCase(8)]
        public async Task Enqueue_Should_Complete_Task_When_Job_Is_Completed_According_To_Limit(int limit)
        {
            var tcsArray = GetTaskCompletionSources<bool>(50);
            var started = 0;
            Func<long, Task> method = index =>
            {
                Interlocked.Increment(ref started);
                return tcsArray[index % tcsArray.Length].Task;
            };
            var jobQueue = ConcurrentUtils.CreateQueue(limit, method);
            var tasks = tcsArray.Select((tcs, i) => jobQueue.Enqueue(i)).ToArray();
            Assert.AreEqual(limit, Volatile.Read(ref started));
            Assert.AreEqual(jobQueue.Count, tcsArray.Length);
            for (var i = 0; i < tcsArray.Length; i++)
            {
                tcsArray[i].SetResult(true);
                await Task.Delay(10);
                Assert.AreEqual(TaskStatus.RanToCompletion, tasks[i].Status);
                var expectedStarted = limit + 1 + i;
                if (expectedStarted > tcsArray.Length)
                {
                    expectedStarted = tcsArray.Length;
                }
                Assert.AreEqual(expectedStarted, Volatile.Read(ref started));
                Assert.AreEqual(tcsArray.Length - i - 1, jobQueue.Count);
            }
        }

        [Test]
        public async Task Enqueue_Should_Fault_Task_When_Job_Failed()
        {
            var tcsArray = GetTaskCompletionSources<bool>(10);
            Func<long, Task> method = index => tcsArray[index % tcsArray.Length].Task;
            var jobQueue = ConcurrentUtils.CreateQueue(4, method);
            var tasks = tcsArray.Select((tcs, i) => jobQueue.Enqueue(i)).ToArray();
            Assert.AreEqual(jobQueue.Count, tcsArray.Length);
            for (var i = 0; i < tcsArray.Length; i++)
            {
                if (i != tcsArray.Length/2)
                {
                    tcsArray[i].SetResult(true);
                }
                else
                {
                    tcsArray[i].SetException(new Exception("Test exception"));
                }
            }
            await Task.Delay(10);
            Assert.AreEqual(tcsArray.Length - 1, tasks.Count(t => t.Status == TaskStatus.RanToCompletion));
            Assert.AreEqual(1, tasks.Count(t => t.Status == TaskStatus.Faulted));
        }

        [Test]
        public async Task Enqueue_Should_Fault_Task_When_Func_Throws()
        {
            var tcsArray = GetTaskCompletionSources<bool>(10);
            Console.WriteLine("Started");

            Func<int, Task> method = index =>
            {
                if (index == tcsArray.Length/2)
                {
                    throw new Exception("Test exception");
                }
                return tcsArray[index%tcsArray.Length].Task;
            };
            var jobQueue = ConcurrentUtils.CreateQueue(4, method);
            var tasks = tcsArray.Select((tcs, i) => jobQueue.Enqueue(i)).ToArray();
            Assert.AreEqual(tcsArray.Length, jobQueue.Count);
            for (var i = 0; i < tcsArray.Length; i++)
            {
                if (i != tcsArray.Length / 2)
                {
                    tcsArray[i].SetResult(true);
                }
            }
            await Task.Delay(100);
            Assert.AreEqual(tcsArray.Length - 1, tasks.Count(t => t.Status == TaskStatus.RanToCompletion));
            Assert.AreEqual(1, tasks.Count(t => t.Status == TaskStatus.Faulted));
        }
    }
}
