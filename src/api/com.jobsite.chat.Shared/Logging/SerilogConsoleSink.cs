using Serilog;
using Serilog.Formatting.Compact;

namespace com.jobsite.chat.Shared.Logging;

public static class SerilogConsoleSink
{
    public static LoggerConfiguration ConfigureConsoleSink(this LoggerConfiguration loggerConfiguration, bool isDevelopment)
    {
        return isDevelopment
            ? loggerConfiguration.WriteTo.Console()
            : loggerConfiguration.WriteTo.Console(new CompactJsonFormatter());
    }
}
