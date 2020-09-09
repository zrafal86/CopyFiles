namespace BlankCoreAppCopyTask.Services
{
    public sealed class Result<T>
    {
        public T Value { get; }
        public Error? Error { get; }

        public Result(T value, Error? error)
        {
            Value = value;
            Error = error;
        }
    }
}