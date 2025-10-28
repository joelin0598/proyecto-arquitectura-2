# 📦 Proyecto de Arquitectura Distribuida: Monitoreo Urbano en Tiempo Real

Este repositorio implementa un sistema distribuido de monitoreo urbano basado en Apache Kafka, Docker y microservicios. El objetivo es simular eventos urbanos en tiempo real, ingestar datos, correlacionarlos y generar alertas, aplicando principios de arquitectura distribuida.

## 🧩 Componentes del sistema

Cada módulo corre como un contenedor independiente:

| Servicio          | Descripción                                                            |
| ----------------- | ---------------------------------------------------------------------- |
| **EventIngestor** | Microservicio .NET que recibe eventos HTTP y los publica en Kafka.     |
| **Correlator**    | Microservicio .NET que consume eventos desde Kafka y genera alertas.   |
| **Simulator**     | Script Python que simula eventos urbanos y los envía al EventIngestor. |
| **Kafka**         | Broker de mensajería con KRaft, sin Zookeeper.                         |
| **Kafka UI**      | Interfaz web para visualizar topics y mensajes.                        |
| **PostgreSQL**    | Base de datos para persistencia de alertas.                            |
| **PgAdmin**       | Interfaz web para administrar PostgreSQL.                              |
| **Redis**         | Cache para correlación temporal de eventos.                            |
| **RedisInsight**  | Interfaz web para visualizar claves y datos en Redis.                  |
| **Airflow**       | Orquestador para flujos de datos y tareas programadas.                 |

## 🛠️ Requisitos

- Docker
- Docker Compose
- Git

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

---

🧪 Simulación de eventos

## 🧪 Simulación de eventos urbanos

El simulador se ejecuta por separado para enviar eventos al microservicio **EventIngestor**, el cual los valida y publica en **Kafka**.  
Este componente permite emular sensores urbanos sin necesidad de hardware físico, generando eventos como robos, incendios, accidentes, disparos o reportes ciudadanos.

---

### ⚙️ Ejecución

```bash
cd Simulador
docker-compose up --build
```

Cada evento es enviado vía HTTP al endpoint /events, validado contra el esquema canónico v1.0 y publicado en el topic events.standardized.

🧠 Algoritmo de generación de eventos
El script Python genera eventos de forma programada y aleatoria, siguiendo estas reglas:

Tipos de evento simulados
panic.button: botón de pánico presionado por ciudadanos

sensor.lpr: lectura de placas vehiculares

sensor.speed: detección de velocidad anómala

sensor.acoustic: sonidos críticos (disparos, explosiones)

citizen.report: reportes ciudadanos voluntarios

Datos generados por evento
Identificadores únicos (event_id, trace_id, correlation_id)

Timestamp en formato UTC ISO‑8601

Severidad (info, warning, critical)

Coordenadas simuladas (lat, lon) dentro de un rango urbano

Payload específico según el tipo de sensor

Formato del evento
json
{
"event_type": "sensor.acoustic",
"geo": { "lat": 14.628, "lon": -90.522 },
"severity": "critical",
"payload": {
"tipo_sonido_detectado": "disparo",
"nivel_decibeles": 112,
"probabilidad_evento_critico": 0.83
}
}
🌍 Área de cobertura simulada
python
LAT_RANGE = (14.620, 14.635)
LON_RANGE = (-90.525, -90.515)
Esto representa una cuadrícula de aproximadamente 1.8 km² sobre la Ciudad de Guatemala, ideal para simular zonas como Zona 4, Zona 10 o Avenida Reforma.

🔁 Frecuencia y comportamiento
El simulador envía eventos cada 0.25 segundos (configurable vía SLEEP_TIME)

Cada evento tiene una severidad aleatoria y una ubicación simulada

El sistema puede generar ráfagas de eventos para probar correlación y tolerancia a carga

📡 Integración con el sistema
Los eventos son recibidos por EventIngestor, validados y publicados en Kafka

El Correlator consume estos eventos, los almacena temporalmente en Redis y genera alertas si se detectan patrones críticos

Las alertas se publican en correlated.alerts y se visualizan en Grafana

### Ejemplo de simulación de evento:

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

---

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

---

## 🧠 Algoritmo de Correlación de Eventos

El **Correlator** es responsable de procesar los eventos urbanos recibidos en Kafka, correlacionarlos basándose en criterios de proximidad geográfica, severidad y tipo de evento, y generar alertas si se cumplen las condiciones establecidas.

### Pasos del algoritmo de correlación:

1. **Recepción de eventos**:

   - Los eventos se consumen desde el topic `events.standardized` en Kafka y se procesan en el microservicio `Correlator`.

2. **Almacenamiento temporal en Redis**:

   - El evento actual se guarda temporalmente en Redis, donde se asocia con una clave `event_zone` (según la zona geográfica del evento).
   - Este almacenamiento en Redis permite acceder a eventos previos en la misma zona para realizar la correlación.

3. **Recuperación de eventos de la misma zona**:

   - El `Correlator` extrae los eventos almacenados en Redis de la misma zona (`zone`) y filtra aquellos cuya **fecha de ocurrencia** no sea mayor a 5 minutos con respecto al evento actual.

4. **Verificación de proximidad**:

   - Para determinar si los eventos están suficientemente cerca, se calcula la **distancia geográfica** entre el evento actual y los eventos previos utilizando la fórmula de Haversine (calculando la distancia en kilómetros). Se establece un umbral de **5 kilómetros** para considerar eventos como "cercanos".

5. **Generación de alertas**:

   - Se evalúan diversas condiciones para generar alertas:
     - **Eventos cercanos**: Si el evento actual está **dentro de los 5 km** de otros **4 eventos** y son de tipo **warning** o **critical**, se genera una alerta de **cluster de eventos múltiples**.
     - **Eventos críticos**: Si el evento actual tiene una **severidad crítica** (`severity == "critical"`), se genera una alerta de tipo **evento crítico**.
     - **Eventos graves específicos**: Si el evento actual es de tipo grave, como **incendios**, **accidentes**, **disparos** o **explosiones**, se genera una alerta específica como **incidente crítico**.

6. **Publicación de alertas**:
   - Las alertas generadas se publican en Kafka en el topic `correlated.alerts`, donde se distribuyen a otros sistemas de monitoreo.
   - Además, las alertas se persisten en la base de datos **PostgreSQL** para su posterior consulta y análisis.

---

📊 Interfaces disponibles

Servicio URL de acceso
Kafka UI ----------------------------- http://localhost:8085
PgAdmin ----------------------------- http://localhost:5050
RedisInsight ------------------------- http://localhost:5540
Airflow Flower ----------------------- http://localhost:5555

Detener el sistema
Para detener todos los contenedores:

```bash
docker-compose down
```
