namespace mma.grpc.server.helper
{
    public class Logger:ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public string Error(string msg)
        {
            return $"ERROR: {msg}";
        }

        public string Info(string msg)
        {
            return $"Information: {msg}";
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            throw new NotImplementedException();
        }

        public string SystemMsg(string msg)
        {
            return $"System: {msg}";
        }
    }
}