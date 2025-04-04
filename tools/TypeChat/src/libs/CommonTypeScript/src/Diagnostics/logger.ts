// Copyright (c) Microsoft. All rights reserved.

import pino from "pino";

// import {logs as apiLogs, SeverityNumber} from "@opentelemetry/api-logs";

// // Get the OpenTelemetry logger
// const otelLogger = apiLogs.getLogger("pino-otel");

// // Map Pino log levels to OpenTelemetry severity levels
// const severityMap: Record<string, SeverityNumber> = {
//     trace: SeverityNumber.TRACE,
//     debug: SeverityNumber.DEBUG,
//     info: SeverityNumber.INFO,
//     warn: SeverityNumber.WARN,
//     error: SeverityNumber.ERROR,
//     fatal: SeverityNumber.FATAL,
// };

// // Create Pino logger
// const serviceName = process.env.OTEL_SERVICE_NAME || process.env.TOOL_NAME || process.title || "unknown-nodejs-service";
// const log = pino({
//     formatters: {
//         level(label) {
//             return {severity: label.toUpperCase()}; // For stdout logs
//         },
//         log(obj) {
//             const levelStr = String(obj.level); // Ensure obj.level is a string
//             const severity = severityMap[levelStr] || SeverityNumber.INFO; // No more "unknown index type" error

//             // Send log data to OpenTelemetry
//             otelLogger.emit({
//                 severityNumber: severity,
//                 body: JSON.stringify(obj),
//             });

//             return obj; // Also log to stdout
//         },
//     },
//     base: {
//         service: serviceName,
//     },
// });

const log = pino();

export default log;
