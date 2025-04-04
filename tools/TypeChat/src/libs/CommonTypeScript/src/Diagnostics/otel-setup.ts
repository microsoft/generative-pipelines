// Copyright (c) Microsoft. All rights reserved.

// import {diag, DiagConsoleLogger, DiagLogLevel} from "@opentelemetry/api";
// import {NodeSDK} from "@opentelemetry/sdk-node";
// import {Resource} from "@opentelemetry/resources";
// import {SemanticResourceAttributes} from "@opentelemetry/semantic-conventions";
// import {OTLPTraceExporter} from "@opentelemetry/exporter-trace-otlp-http";
// import {LoggerProvider, ConsoleLogRecordExporter, SimpleLogRecordProcessor} from "@opentelemetry/sdk-logs";
// import {OTLPLogExporter} from "@opentelemetry/exporter-logs-otlp-http";
// import {getNodeAutoInstrumentations} from "@opentelemetry/auto-instrumentations-node";
// import {logs as apiLogs} from "@opentelemetry/api-logs";

// // Enable OpenTelemetry debugging logs (optional)
// diag.setLogger(new DiagConsoleLogger(), DiagLogLevel.INFO);

// // Set up Trace Exporter (sends traces to OpenTelemetry)
// const traceExporter = new OTLPTraceExporter({
//     url: "http://localhost:4318/v1/traces", // Update if needed
//     concurrencyLimit: 10,
// });

// // Define OpenTelemetry SDK for tracing
// const serviceName = process.env.OTEL_SERVICE_NAME || process.env.TOOL_NAME || process.title || "unknown-nodejs-service";
// const sdk = new NodeSDK({
//     resource: new Resource({
//         [SemanticResourceAttributes.SERVICE_NAME]: serviceName,
//     }),
//     traceExporter,
//     instrumentations: [getNodeAutoInstrumentations()],
// });

// // Start OpenTelemetry SDK
// sdk.start();
// console.log("✅ OpenTelemetry tracing initialized");

// // Graceful shutdown on SIGTERM
// process.on("SIGTERM", async () => {
//     await sdk.shutdown();
//     process.exit(0);
// });

// // Set up Logging (Logs to OpenTelemetry)
// const otlpEndpoint = process.env.OTEL_EXPORTER_OTLP_ENDPOINT || "http://localhost:4318/v1/logs";
// const logExporter = new OTLPLogExporter({url: otlpEndpoint});
// const logProvider = new LoggerProvider();
// logProvider.addLogRecordProcessor(new SimpleLogRecordProcessor(new ConsoleLogRecordExporter())); // Also logs to console
// logProvider.addLogRecordProcessor(new SimpleLogRecordProcessor(logExporter));

// // ✅ Correct way to register the log provider globally
// apiLogs.setGlobalLoggerProvider(logProvider);

// console.log("✅ OpenTelemetry logging initialized");

// export default sdk;
