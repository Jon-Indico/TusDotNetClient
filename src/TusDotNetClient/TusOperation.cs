using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TusDotNetClient
{
    public delegate void ProgressDelegate(long bytesTransferred, long bytesTotal);

    public class TusOperation<T>
    {
        private readonly OperationDelegate _operation;
        private Task<T> _operationTask;

        public delegate Task<T> OperationDelegate(ProgressDelegate reportProgress);

        public event ProgressDelegate Progressed;

        public Task<T> Operation =>
            _operationTask ??
            (_operationTask = _operation((transferred, total) =>
                Progressed?.Invoke(transferred, total)));

        internal TusOperation(OperationDelegate operation)
        {
            _operation = operation;
        }

        public TaskAwaiter<T> GetAwaiter() => Operation.GetAwaiter();
    }
}