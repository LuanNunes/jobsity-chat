using Serilog;
using Serilog.Formatting.Compact;

namespace com.jobsite.chat.Shared.Logging;

// Shared console-sink selection reused by the Api and Bot hosts: readable output in Development,
// compact JSON everywhere else. Kept out of config so the environment-driven format choice lives in
// one place (the "no WriteTo in config" decision).
public static class SerilogConsoleSink
{
    public static LoggerConfiguration ConfigureConsoleSink(this LoggerConfiguration loggerConfiguration, bool isDevelopment)
    {
        return isDevelopment
            ? loggerConfiguration.WriteTo.Console()
            : loggerConfiguration.WriteTo.Console(new CompactJsonFormatter());
    }
}
