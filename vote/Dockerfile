# Using a lightweight official Python image
FROM python:3.11-slim

# Set working directory
WORKDIR /app

# Install dependencies first (to leverage docker cache)
COPY requirements.txt /app/
RUN pip install --no-cache-dir -r requirements.txt

# Copy the application code
COPY . /app/

# Expose port 80
EXPOSE 80

# Run using gunicorn for production grade WSGI server
CMD ["gunicorn", "-b", "0.0.0.0:80", "--workers", "4", "--keep-alive", "0", "app:app"]
