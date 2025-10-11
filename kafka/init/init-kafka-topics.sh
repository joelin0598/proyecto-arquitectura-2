#!/bin/bash
# Espera a que Kafka esté completamente disponible
echo "Esperando a que Kafka esté disponible..."
sleep 30

# Crea los topics de Kafka
kafka-topics --create --topic events.standardized --bootstrap-server localhost:9092 --partitions 1 --replication-factor 1
kafka-topics --create --topic correlated.alerts --bootstrap-server localhost:9092 --partitions 1 --replication-factor 1

echo "Topics creados exitosamente"
