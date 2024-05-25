using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmpyrionLogger
{
    internal class AsyncManualResetEvent
    {
        private readonly object _lock = new object();
        private TaskCompletionSource<object?> _tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool IsSet
        {
            get { lock (_lock) return _tcs.Task.IsCompleted; }
        }

        public Task WaitAsync(int timeout, CancellationToken cancellationToken = default)
        {
            lock(_lock)
            {
                return Task.WhenAny(_tcs.Task, Task.Delay(timeout, cancellationToken));
            }
        }

        public void Set()
        {
            lock(_lock)
            {
                _tcs.SetResult(null);
            }
        }

        public void Reset()
        {
            lock(_lock)
            {
                if (_tcs.Task.IsCompleted)
                    _tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }
}
