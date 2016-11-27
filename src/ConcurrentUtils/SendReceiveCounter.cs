//
//   Copyright (C) 2016 Jorge Bay Gondra
//
//   This software may be modified and distributed under the terms
//   of the MIT license.  See the LICENSE.txt file for details.
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrentUtils
{
    /// <summary>
    /// Represents a dual counter
    /// </summary>
    internal class SendReceiveCounter
    {
        private long _receiveCounter;
        private long _sendCounter;

        public long IncrementSent()
        {
            return Interlocked.Increment(ref _sendCounter);
        }

        public long IncrementReceived()
        {
            return Interlocked.Increment(ref _receiveCounter);
        }
    }
}
