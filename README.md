# 📦 Proyecto de Arquitectura Distribuida: Monitoreo Urbano en Tiempo Real

Este repositorio implementa un sistema distribuido de monitoreo urbano basado en Apache Kafka, Docker y microservicios. El objetivo es simular eventos urbanos en tiempo real, ingestar datos, correlacionarlos y generar alertas, aplicando principios de arquitectura distribuida.

## 🧩 Componentes del sistema

Cada módulo corre como un contenedor independiente:

| Servicio           | Descripción                                                                 |
|--------------------|------------------------------------------------------------------------------|
| **EventIngestor**  | Microservicio .NET que recibe eventos HTTP y los publica en Kafka.          |
| **Correlator**     | Microservicio .NET que consume eventos desde Kafka y genera alertas.        |
| **Simulator**      | Script Python que simula eventos urbanos y los envía al EventIngestor.      |
| **Kafka**          | Broker de mensajería con KRaft, sin Zookeeper.                              |
| **Kafka UI**       | Interfaz web para visualizar topics y mensajes.                             |
| **PostgreSQL**     | Base de datos para persistencia de alertas.                                 |
| **PgAdmin**        | Interfaz web para administrar PostgreSQL.                                   |
| **Redis**          | Cache para correlación temporal de eventos.                                 |
| **RedisInsight**   | Interfaz web para visualizar claves y datos en Redis.                       |
| **Airflow**        | Orquestador para flujos de datos y tareas programadas.                      |

## 🛠️ Requisitos

- Docker
- Docker Compose

## 🚀 Ejecución del sistema

1. Clonar el repositorio:

```bash
git clone https://github.com/joelin0598/proyecto-arquitectura-2.git
cd proyecto-arquitectura-2
```
2. Levantar todos los servicios (excepto el simulador):
```bash
docker-compose up -d
```
4. Crear los topics necesarios en Kafka:
```bash
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
```
🧪 Simulación de eventos
El simulador se ejecuta por separado para enviar eventos al EventIngestor:
```bash
cd Simulador
docker-compose up --build
```
Esto generará eventos como robos, incendios o accidentes, que serán publicados en Kafka y procesados por el Correlator.

🌐 Endpoints del EventIngestor
Acceder a la documentación Swagger en:
```bash
http://localhost:5245/swagger/index.html
```
## Endpoint `http://localhost:5245/swagger/index.html`

| Método | Endpoint         | Descripción                                                | Ejemplo curl                                      |
| ------ | ---------------- | ---------------------------------------------------------- | ------------------------------------------------- |
| POST   | `/events`        | Publica un evento en kafka                                 | `curl http://localhost:5245/events`               |
| ------ | ---------------  | ---------------------------------------------------------- | ------------------------------------------------- |
| GET    | `/events/health` | Verifica la salud del endpoint                             | `curl http://localhost:5245/events/health`        |

📊 Interfaces disponibles


Servicio	URL de acceso
Kafka UI	----------------------------- http://localhost:8085
PgAdmin	  ----------------------------- http://localhost:5050
RedisInsight	------------------------- http://localhost:5540
Airflow Flower	----------------------- http://localhost:5555

Detener el sistema
Para detener todos los contenedores:
```bash
docker-compose down
```
