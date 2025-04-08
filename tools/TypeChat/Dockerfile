# Build Stage (Compiles TypeScript)
FROM node:20-slim AS builder

# Set working directory
WORKDIR /app

# Build-time argument
# Note: Webpack and Babel optimizers strip out process.env.NODE_ENV !== "production" blocks
ARG NODE_ENV=production
ENV NODE_ENV=$NODE_ENV

# Copy package files and install dependencies (only prod deps to save space)
COPY package.json pnpm-lock.yaml ./
RUN npm install -g pnpm && pnpm install --frozen-lockfile

# Copy source code and build TypeScript
COPY . .
RUN pnpm run build

# Runtime Stage (Final lightweight image)
FROM node:20-slim

# Set working directory
WORKDIR /app

# Persist NODE_ENV inside the running container
# If NODE_ENV=production,:
# - Express automatically enables optimized view rendering.
# - Express automatically enables caching for performance.
# - Express automatically disables detailed error messages.
# - dotenv typically ignores .env files, unless explicitly loaded.
ARG NODE_ENV=production
ENV NODE_ENV=$NODE_ENV

# Copy only built JS files and production dependencies
COPY --from=builder /app/dist /app/dist
COPY --from=builder /app/node_modules /app/node_modules
COPY --from=builder /app/package.json /app/package.json

# Start the server
# Note: you might want to remove source maps in prod
#CMD ["node", "--enable-source-maps", "dist/app.js"]
CMD ["sh", "-c", "exec node --enable-source-maps dist/app.js"]
