# Simulador de Eventos Smart City

Este proyecto es un simulador desarrollado en Python para generar y enviar eventos aleatorios de una ciudad inteligente a un endpoint HTTP.  
Emula diferentes tipos de sensores y reportes ciudadanos con el objetivo de facilitar las pruebas y validación de servicios de ingesta de eventos.

---

## Descripción General

El simulador produce continuamente eventos en formato JSON y los envía mediante solicitudes HTTP `POST` hacia un endpoint configurable.  
Cada evento contiene metadatos como versión, tipo, identificadores de correlación, marcas de tiempo, geolocalización, nivel de severidad y un payload específico del tipo de sensor simulado.

El simulador está pensado para utilizarse junto con otros componentes de una plataforma distribuida, como pipelines basados en Kafka o microservicios de ingesta de eventos.

---

## Características

- Bucle infinito generando eventos en intervalos configurables.
- Tipos de eventos aleatorios:
  - `panic.button`
  - `sensor.lpr`
  - `sensor.speed`
  - `sensor.acoustic`
  - `citizen.report`
- Identificadores generados automáticamente:
  - `event_id`
  - `correlation_id`
  - `trace_id`
- Valores de geolocalización dinámicos dentro de rangos definidos.
- Cargas útiles estructuradas según el tipo de evento.
- Envío mediante solicitudes HTTP `POST` hacia el endpoint configurado.

---

## Estructura del Proyecto

```
Simulador/
├── Dockerfile
├── requirements.txt
├── producer.py
└── docker-compose.yml
```

---

## Configuración

El simulador utiliza las siguientes variables de entorno:

| Variable | Descripción | Valor por defecto |
|-----------|--------------|------------------|
| `EVENT_ENDPOINT` | Endpoint HTTP de destino para las solicitudes POST | `http://host.docker.internal:5245/events` |
| `SLEEP_TIME` | Intervalo (en segundos) entre eventos generados | `0.25` |

Estos valores pueden modificarse en el `Dockerfile`, en el `docker-compose.yml` o al ejecutar el contenedor.

---

## Ejecución Local

1. Instalar dependencias:
   ```bash
   pip install -r requirements.txt
   ```

2. Ejecutar el simulador:
   ```bash
   python producer.py
   ```

3. El simulador enviará eventos de forma continua al endpoint definido en `EVENT_ENDPOINT`.

---

## Ejecución con Docker

1. Construir la imagen:
   ```bash
   docker build -t smart-simulator:1.0 .
   ```

2. Ejecutar el contenedor:
   ```bash
   docker run --rm smart-simulator:1.0
   ```

3. O usar Docker Compose:
   ```bash
   docker-compose up --build
   ```

El contenedor comenzará a enviar solicitudes HTTP POST al endpoint configurado.

---

## Ejemplo de Evento Generado

```json
{
  "event_version": "1.0",
  "event_type": "sensor.lpr",
  "event_id": "b5e3e3d4-7b12-4c34-8a1e-6f3c5a3b8d89",
  "producer": "python-simulation",
  "source": "simulated",
  "correlation_id": "c6f7435a-47a2-47b3-9e28-2128a624d9ef",
  "trace_id": "d2f98a65-7ac2-4c13-b5a2-13e1843d4b12",
  "timestamp": "2025-10-11T04:22:31Z",
  "partition_key": "sensor.lpr",
  "geo": {
    "zone": "zone_4",
    "lat": 14.629,
    "lon": -90.519
  },
  "severity": "warning",
  "payload": {
    "placa_vehicular": "XYZ123",
    "velocidad_estimada": 85,
    "modelo_vehiculo": "sedan",
    "color_vehiculo": "rojo",
    "ubicacion_sensor": "blvd_las_americas_01"
  }
}
```

---

## Notas

- El simulador se ejecuta de forma indefinida hasta que se detenga manualmente con `Ctrl + C` o `docker-compose down`.
- Cada evento es independiente y posee identificadores únicos de correlación y trazabilidad.
- En sistemas Linux puede ser necesario agregar la siguiente línea en el `docker-compose.yml` para permitir el acceso al host:
  ```yaml
  extra_hosts:
    - "host.docker.internal:host-gateway"
  ```

---

## Licencia

Este proyecto se distribuye bajo la licencia MIT.
