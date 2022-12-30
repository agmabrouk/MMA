namespace mma.common;
public class Logger
{
    public string Error(string msg)
    {
        return $"ERROR: {msg}";
    }

    public string Info(string msg)
    {
        return $"Information: {msg}";
    }


    public string SystemMsg(string msg)
    {
        return $"System: {msg}";
    }
}

