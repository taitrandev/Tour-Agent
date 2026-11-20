# ==============================================================================
# Stage 1: Build React SPA with Vite
# ==============================================================================
FROM node:20-alpine AS builder

WORKDIR /app

# Build arguments with production Azure API default
ARG VITE_API_BASE_URL=https://touragent-api.gentlecliff-410d3b6b.southeastasia.azurecontainerapps.io/api/v1
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

# Copy package dependency manifests first for Docker layer caching
COPY package*.json ./
RUN npm ci

# Copy full frontend source code
COPY . .

# Build production assets (emits to /app/dist)
RUN npm run build

# ==============================================================================
# Stage 2: Production Nginx Runtime
# ==============================================================================
FROM nginx:alpine

# Remove default nginx static assets
RUN rm -rf /usr/share/nginx/html/*

# Copy compiled static assets from builder stage
COPY --from=builder /app/dist /usr/share/nginx/html

# Copy custom Nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose HTTP port
EXPOSE 80

# Start Nginx in foreground
CMD ["nginx", "-g", "daemon off;"]

