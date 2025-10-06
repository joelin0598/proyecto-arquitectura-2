FROM python:3.11-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY producer.py .

# Variables de entorno por defecto
ENV EVENT_ENDPOINT=http://host.docker.internal:5245/events
ENV SLEEP_TIME=0.25

CMD ["python", "producer.py"]
