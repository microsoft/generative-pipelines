Vibe:
```
Create a new nodejs project from scratch, follow these requirements:
- Show the commands to run in bash terminal, so they can be saved in a setup.txt file for later.
- Use "pnpm". pnpm doesn't support -y or --yes.
- Use typecript and remember to add the types for development. TypeScript requires explicit initialization. Allow decorators and importing JSON files.
- Use tsoa to autogenerate and server swagger.
- Keep all files under ./src, without nested folders. There will be very few endpoint so no need for the extra complexity caused by directories.
- Add one sample /users endpoint with well defined classes/interfaces for input and output. The endpoint should be a "POST /" accepting in input a user object and returning an account object.
- The port to listen to will be passed using a PORT env var.
- Add dockerfile. The code should be transpiled to JS for faster execution. Reduce the docker image size using multistage. The docker image should stop on CTRL+C.
- Show how to run the docker image in interactive mode and delete when stopped.
```

# 1️⃣ Initialize a new Node.js project
pnpm init

# 2️⃣ Install TypeScript & TSOA
pnpm add -D typescript ts-node @types/node

pnpm add express tsoa class-transformer class-validator
pnpm add -D @types/express @types/swagger-ui-express @types/joi @tsoa/runtime

pnpm add ioredis multer
pnpm add -D @types/multer


# 3️⃣ Initialize TypeScript
npx tsc --init

# 4️⃣ Initialize TSOA
npx tsoa init

# 5️⃣ Install Swagger UI Express
pnpm add swagger-ui-express

# File upload

Multer is a middleware for handling multipart/form-data, which is used for file uploads.

```shell
pnpm add multer
pnpm add -D @types/multer
```

# TypeChat

```shell
pnpm add typechat
```

# Barrelsby

pnpm add -D barrelsby

Barrelsby is a tool for automatically generating TypeScript barrel files (index.ts)
that export modules from a directory. This makes imports cleaner and easier to manage.
Run this to generate/update the `src/libs/CommonTypeScript/src/index.ts` file:

```shell
npx barrelsby --config barrelsby.json
```

# OpenTelemetry

pnpm add @opentelemetry/sdk-logs

pnpm add \
    @opentelemetry/api \
    @opentelemetry/sdk-node \
    @opentelemetry/resources \
    @opentelemetry/semantic-conventions \
    @opentelemetry/exporter-trace-otlp-http \
    @opentelemetry/exporter-logs-otlp-http \
    @opentelemetry/instrumentation-http \
    @opentelemetry/instrumentation-express \
    @opentelemetry/auto-instrumentations-node \
    pino

# For swagger

pnpm add -D cpy-cli

```json
{
  "scripts": {
    "build": "tsc && tsoa routes && tsoa spec && cpy src/swagger.json dist/"
  }
}
```