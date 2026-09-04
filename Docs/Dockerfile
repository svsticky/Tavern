# -- Generation dependencies with .net, nodejs and npm and all packages for documentation generation
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS generation-deps

WORKDIR /app

RUN apt-get update && \
    apt-get install -y bash curl && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs && \
    rm -rf /var/lib/apt/lists/*

RUN dotnet tool install --global Swashbuckle.AspNetCore.Cli --version 6.6.2
RUN dotnet tool install --global NSwag.ConsoleCore
RUN dotnet tool install -g docfx

ENV PATH="/root/.dotnet/tools:${PATH}"

# -- Install node dependencies for the project
FROM node:lts-alpine AS project-deps
WORKDIR /app

COPY Docs/package.json ./ 
COPY Docs/package-lock.json ./ 
COPY Docs/source.config.ts ./

RUN npm ci --ignore-scripts

# -- Development
FROM generation-deps AS development
WORKDIR /app/Docs
ENV NODE_ENV=development
ENV NEXT_TELEMETRY_DISABLED=1
EXPOSE 3001
ENV PORT=3001
ENV DOTNET_ENVIRONMENT=docs

# Create custom entrypoint script to run npm ci as well to update everything
ENTRYPOINT npm ci --ignore-scripts && npm run dev --turbopack

# -- Docs generator for production
FROM generation-deps AS docs-generator
WORKDIR /app

# Copy the content
COPY Docs ./Docs
COPY Backend ./Backend
COPY Frontend ./Frontend

# install modules for frontend
WORKDIR /app/Frontend
RUN npm ci --ignore-scripts

# Generate documentation
WORKDIR /app/Docs
RUN npm ci --ignore-scripts
ENV DOTNET_ENVIRONMENT=docs
RUN npm run build-docs

# -- Build the app for production
FROM generation-deps AS builder
WORKDIR /app

# Copy the generated dependencies for the project
COPY --from=project-deps /app/node_modules ./node_modules

# Copy the docs next project
COPY Docs .

# Copy the generated documentation
COPY --from=docs-generator /app/Docs/content/Docs/Backend ./content/docs/Backend
COPY --from=docs-generator /app/Docs/content/Docs/Frontend ./content/docs/Frontend
COPY --from=docs-generator /app/Docs/content/Docs/API ./content/docs/API
COPY --from=docs-generator /app/Docs/swagger.json ./swagger.json

# Build the next project
ENV NEXT_TELEMETRY_DISABLED=1 
RUN npm run build

# -- Run the app as production
FROM node:lts-alpine AS production
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

# Create a non-root user
RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

# Copy only necessary files
COPY --from=builder /app/public ./public
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static
COPY --from=docs-generator /app/Docs/swagger.json ./swagger.json

USER nextjs
EXPOSE 3001
ENV PORT=3001
ENV HOSTNAME="0.0.0.0"

CMD ["node", "server.js"]
