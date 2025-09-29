# Proyecto Arquitectura Distribuida

Este repositorio contiene varios contenedores que representan servicios independientes:

- **API-Event-Ingestor** → Servicio .NET para ingestar eventos.
- **Database** → Contenedor de base de datos.
- **Event-Correlator** → Servicio para correlacionar eventos.
- **Server-Kafka** → Servidor de mensajería.
- **Simulator-Events** → Simulador de eventos de prueba.

## Requisitos

- Docker
- Docker Compose

## Ejecución

1. Clonar este repositorio:
   ```bash
   git clone https://github.com/joelin0598/proyecto-arquitectura-2.git
   cd proyecto-arquitectura-2
   ```

## Levantar Contenedor individual de API-Event-Ingestor

- Ingresar al contenedor que se desea utilizar
- docker-compose up --build

## Endpoint `http://localhost:5245/swagger/index.html`

| Método | Endpoint         | Descripción                                                | Ejemplo curl                                      |
| ------ | ---------------- | ---------------------------------------------------------- | ------------------------------------------------- |
| POST   | `/events`        | Publica un evento en kafka                                 | `curl http://localhost:5245/events`               |
| ------ | ---------------  | ---------------------------------------------------------- | ------------------------------------------------- |
| GET    | `/events/health` | Verifica la salud del endpoint                             | `curl http://localhost:5245/events/health`        |

# Detener los contenedores

- docker-compose down
