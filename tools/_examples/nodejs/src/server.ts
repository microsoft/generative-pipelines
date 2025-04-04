// Copyright (c) Microsoft. All rights reserved.

import app from "./app";

const PORT = process.env.PORT || 4001;

const server = app.listen(PORT, () => {
  console.log(`Server is running on http://localhost:${PORT}`);
});

// Graceful shutdown
const shutdown = () => {
  console.log("Shutting down gracefully...");
  server.close(() => {
    console.log("Server closed.");
    process.exit(0);
  });
};

process.on("SIGINT", shutdown);  // Ctrl+C
process.on("SIGTERM", shutdown); // Docker stop
