using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace PollApp.Api.Telemetry;

// Central place for all custom telemetry sources.
// ActivitySource = OpenTelemetry "Tracer" (creates spans)
// Meter = OpenTelemetry "Meter" (creates metrics like counters and histograms)
public static class DiagnosticsConfig
{
    public const string ServiceName = "PollApp";

    // ActivitySource lets us create custom spans (called "Activities" in .NET).
    public static readonly ActivitySource Source = new(ServiceName);

    // Meter lets us create custom metrics (counters, histograms).
    public static readonly Meter Meter = new(ServiceName);

    // Custom counter: how many votes have been cast total?
    public static readonly Counter<long> VoteCounter = Meter.CreateCounter<long>("pollapp.votes.count");
}
