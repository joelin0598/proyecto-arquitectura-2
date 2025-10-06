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

# Levantar Contenedor individual de Server-Kafka

- Ingresar al directorio raíz del contenedor Server-Kafka dentro de la terminal
- Ejecutar los siguientes comandos: docker-compose up -d
- Ingresar a la terminal dentro de docker para el conetenedor de Server-Kafka
- Entrar al modo comandos de Linux, comando wsl + Enter
- Copiar y Ejecutar comandos para creación de topics de eventos y alertas, dentro de archivo topics.md

## Levantar Contenedor individual de API-Event-Ingestor

- Ingresar al directorio raíz del contenedor API-Event-Ingestor dentro de la terminal
- Ejecutar los siguientes comandos: docker-compose up --build

## Endpoint `http://localhost:5245/swagger/index.html`

| Método | Endpoint         | Descripción                                                | Ejemplo curl                                      |
| ------ | ---------------- | ---------------------------------------------------------- | ------------------------------------------------- |
| POST   | `/events`        | Publica un evento en kafka                                 | `curl http://localhost:5245/events`               |
| ------ | ---------------  | ---------------------------------------------------------- | ------------------------------------------------- |
| GET    | `/events/health` | Verifica la salud del endpoint                             | `curl http://localhost:5245/events/health`        |

# Levantar Contenedor individual de Simulador

- Ingresar al directorio raíz del contenedor Simulador dentro de la terminal
- Ejecutar los siguientes comandos: docker-compose up --build

# Levantar Contenedor individual de Correlator

- Ingresar al directorio raíz del contenedor Correlator dentro de la terminal
- Ejecutar los siguientes comandos:
  docker-compose build
  docker-compose up -d

# Detener los contenedores

- docker-compose down
