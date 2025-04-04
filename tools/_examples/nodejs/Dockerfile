# ========================================================
FROM node:20-alpine AS builder

WORKDIR /app

# Install dependencies required for pnpm
RUN apk add --no-cache curl

# Download the standalone pnpm binary directly (no shell detection issues and no signatures issues)
RUN curl -fsSL https://github.com/pnpm/pnpm/releases/latest/download/pnpm-linuxstatic-x64 \
    -o /usr/local/bin/pnpm \
    && chmod +x /usr/local/bin/pnpm

# Confirm pnpm is working
RUN pnpm --version

# Copy package.json and pnpm-lock.yaml before running install
COPY package.json pnpm-lock.yaml ./

# Install dependencies using pnpm
RUN pnpm install --frozen-lockfile

# Copy the rest of the application files
COPY . .

# Transpile TypeScript files
RUN pnpm exec tsc

# ========================================================
# Production image (lighter, without pnpm)
FROM node:20-alpine

WORKDIR /app

# Copy only necessary files from the builder stage
COPY --from=builder /app/package.json ./
COPY --from=builder /app/dist ./dist
COPY --from=builder /app/node_modules ./node_modules

EXPOSE 3000

# Start the compiled JavaScript server
CMD ["node", "dist/server.js"]
