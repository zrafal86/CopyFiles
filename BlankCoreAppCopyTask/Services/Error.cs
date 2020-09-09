namespace BlankCoreAppCopyTask.Services
{
    public sealed class Error
    {
        public string Msg { get; }

        public Error(string msg)
        {
            Msg = msg;
        }
    }
}