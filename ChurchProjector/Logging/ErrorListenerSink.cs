using Serilog.Core;
using Serilog.Events;
using ChurchProjector.Classes;

namespace ChurchProjector.Logging;
public class ErrorListenerSink : ILogEventSink
{
    /// <summary>
    /// Emit the provided log event to the sink.
    /// </summary>
    /// <param name="logEvent">The log event to write</param>
    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level >= LogEventLevel.Error)
        {
            GlobalConfig.HasError.Value = true;
        }
    }
}