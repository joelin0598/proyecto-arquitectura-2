docker exec -it kafka bash

kafka-topics --create \
 --topic events.standardized \
 --bootstrap-server localhost:9092 \
 --partitions 1 \
 --replication-factor 1

kafka-topics --create \
 --topic correlated.alerts \
 --bootstrap-server localhost:9092 \
 --partitions 1 \
 --replication-factor 1
