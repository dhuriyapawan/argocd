FROM node:20-alpine

# Use a non-root user for improved security
ENV NODE_ENV=production
RUN addgroup -S appgroup && adduser -S appuser -G appgroup

# Set working directory
WORKDIR /app

# Install app dependencies
COPY package*.json ./
# install production dependencies only, disable audit to speed up build
RUN npm install --production --no-audit --no-fund

# Bundle app source and set ownership
COPY . .
RUN chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Expose server port
EXPOSE 80

# Start app
CMD ["npm", "start"]
