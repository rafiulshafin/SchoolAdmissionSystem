
# STAGE 1: Build React application
FROM node:24-alpine AS build

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy frontend source code
COPY . .

# Build production files
RUN npm run build


# STAGE 2: Serve using Nginx
FROM nginx:alpine AS final

# Copy React production files
COPY --from=build /app/dist /usr/share/nginx/html

# Copy Nginx configuration template
COPY nginx.conf.template /etc/nginx/templates/default.conf.template

# Substitute only BACKEND_URL, preserving Nginx variables
ENV NGINX_ENVSUBST_FILTER='^BACKEND_URL$'

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
