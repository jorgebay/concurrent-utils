using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConcurrentUtils.Test
{
    public abstract class BaseUnitTest
    {
        protected TaskCompletionSource<T>[] GetTaskCompletionSources<T>(int amount)
        {
            var tcsArray = new TaskCompletionSource<T>[amount];
            for (var i = 0; i < tcsArray.Length; i++)
            {
                tcsArray[i] = new TaskCompletionSource<T>();
            }
            return tcsArray;
        }
    }
}
