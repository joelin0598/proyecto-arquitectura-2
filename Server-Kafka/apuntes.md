//Crear topics pra comunicación

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

//Simulación con Event Ingestor
{
"event_version": "1.0",
"event_type": "panic.button",
"event_id": "",
"producer": "simulator",
"source": "simulated",
"correlation_id": "123e4567-e89b-12d3-a456-426614174001",
"trace_id": "123e4567-e89b-12d3-a456-426614174002",
"timestamp": "",
"partition_key": "",
"geo": {
"zone": "zona_4",
"lat": 14.628,
"lon": -90.522
},
"severity": "critical",
"payload": {
"tipo_de_alerta": "panico",
"identificador_dispositivo": "BTN-001",
"user_context": "movil"
}
}

//Tecnologías a utlizar en Correlator y Grafana

Apache AirFlow-> Redis -> logstach -> Elastic -> export
Postgres -> exporter -> Prometeous -> Grafana
