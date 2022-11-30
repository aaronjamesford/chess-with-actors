using System.Diagnostics;
using ChessWithActors.Backend;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Proto.OpenTelemetry;
using Serilog;
using Serilog.Core;
using Serilog.Events;

const string ServiceName = "chess-backend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddChessBackendProtoActor();

builder.Services.AddOpenTelemetryTracing(tpb =>
{
    tpb.SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService(ServiceName))
        .AddProtoActorInstrumentation()
        .AddOtlpExporter(opt =>
        {
            opt.Protocol = builder.Configuration.GetValue<string>("Telemetry:OtlpExporter:Endpoint").Equals("grpc", StringComparison.InvariantCultureIgnoreCase)
                ? OtlpExportProtocol.Grpc : OtlpExportProtocol.HttpProtobuf;
            opt.Endpoint = new Uri(builder.Configuration.GetValue<string>("Telemetry:OtlpExporter:Endpoint"));
        });
});

builder.Host.UseSerilog((context, cfg) =>
{
    cfg
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.WithProperty("service", ServiceName)
        .Enrich.WithProperty("env", builder.Environment.EnvironmentName)
        .Enrich.With<TraceIdEnricher>();
});

builder.Services.AddOpenTelemetryMetrics(mpb =>
{
    mpb.SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService(ServiceName))
        .AddProtoActorInstrumentation()
        .AddPrometheusExporter();
});

var app = builder.Build();

app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.Run();

public partial class Program { } // For testing

public class TraceIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (Activity.Current != null)
        {
            // facilitate Grafana logs to traces correlation
            logEvent.AddOrUpdateProperty(
                propertyFactory.CreateProperty("traceID", Activity.Current.TraceId.ToHexString()));
        }
    }
}