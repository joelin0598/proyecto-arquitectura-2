# Proyecto Arquitectura Distribuida

Este repositorio contiene varios contenedores que representan servicios independientes:

- **Server-Kafka** → Servidor de mensajería.
- **API-Event-Ingestor** → Servicio .NET para ingestar eventos.
- **Correlator** → Servicio para correlacionar eventos.
- **Simulator** → Simulador de eventos de prueba.
- **Database** → Contenedor de base de datos.

## Requisitos

- Docker
- Docker Compose

## Ejecución

1. Clonar este repositorio:
   ```bash
   git clone https://github.com/joelin0598/proyecto-arquitectura-2.git
   cd proyecto-arquitectura-2
   ```
2. Levantar contenedores

- docker-compose up

3. Agregar topics

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

# Levantar Contenedor individual de Simulador

- Ingresar al directorio raíz del contenedor Simulador dentro de la terminal
- Ejecutar los siguientes comandos: docker-compose up --build

# Event Ingestor

## Endpoint `http://localhost:5245/swagger/index.html`

| Método | Endpoint         | Descripción                                                | Ejemplo curl                                      |
| ------ | ---------------- | ---------------------------------------------------------- | ------------------------------------------------- |
| POST   | `/events`        | Publica un evento en kafka                                 | `curl http://localhost:5245/events`               |
| ------ | ---------------  | ---------------------------------------------------------- | ------------------------------------------------- |
| GET    | `/events/health` | Verifica la salud del endpoint                             | `curl http://localhost:5245/events/health`        |

# Detener los contenedores

- docker-compose down
