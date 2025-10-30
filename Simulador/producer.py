import requests
import uuid
import datetime
import time
import random
import os

URL = os.getenv("EVENT_ENDPOINT", "http://localhost:5245/events")
SLEEP_TIME = float(os.getenv("SLEEP_TIME", "0.066"))

# Rango general (referencia) de la ciudad simulada
LAT_RANGE = (14.620, 14.635)
LON_RANGE = (-90.525, -90.515)

EVENT_TYPES = [
    "panic.button",
    "sensor.lpr",
    "sensor.speed",
    "sensor.acoustic",
    "citizen.report"
]

SEVERITIES = ["info", "warning", "critical"]

# Zonas candidatas (perimetro de 5km)
ZONES = ["zone_1", "zone_3", "zone_4", "zone_5", "zone_9", "zone_11", "zone_12", "zone_13"]

ZONE_BOUNDS = {
    "zone_1": ((14.620, 14.623), (-90.525, -90.523)),
    "zone_3": ((14.623, 14.626), (-90.523, -90.521)),
    "zone_4": ((14.626, 14.629), (-90.521, -90.519)),
    "zone_5": ((14.629, 14.632), (-90.519, -90.517)),
    "zone_9": ((14.632, 14.635), (-90.517, -90.515)),
    "zone_11": ((14.620, 14.622), (-90.517, -90.515)),
    "zone_12": ((14.624, 14.626), (-90.525, -90.523)),
    "zone_13": ((14.632, 14.634), (-90.521, -90.519)),
}

def random_payload(event_type):
    if event_type == "panic.button":
        return {
            "tipo_de_alerta": random.choice(["panico", "emergencia", "incendio"]),
            "identificador_dispositivo": random.choice(["BTN-001", "BTN-002", "BTN-003"]),
            "user_context": random.choice(["movil", "quiosco", "web"])
        }
    elif event_type == "sensor.lpr":
        return {
            "placa_vehicular": random.choice(["P123FRT", "XYZ123", "MNO456", "ABC789"]),
            "velocidad_estimada": random.randint(20, 120),
            "modelo_vehiculo": random.choice(["sedan", "pickup", "motocicleta", "suv"]),
            "color_vehiculo": random.choice(["rojo", "azul", "negro", "blanco"]),
            "ubicacion_sensor": random.choice(["blvd_las_americas_01", "calzada_roosevelt_05", "zona10_av_reforma"])
        }
    elif event_type == "sensor.speed":
        return {
            "velocidad_detectada": random.randint(10, 150),
            "sensor_id": random.choice(["SPD-017", "SPD-021", "SPD-033"]),
            "direccion": random.choice(["NORTE", "SUR", "ESTE", "OESTE"])
        }
    elif event_type == "sensor.acoustic":
        return {
            "tipo_sonido_detectado": random.choice(["disparo", "explosion", "vidrio_roto"]),
            "nivel_decibeles": random.randint(60, 140),
            "probabilidad_evento_critico": round(random.uniform(0.1, 0.95), 2)
        }
    elif event_type == "citizen.report":
        return {
            "tipo_evento": random.choice(["accidente", "incendio", "altercado"]),
            "mensaje_descriptivo": random.choice([
                "vehiculo volcado",
                "incendio en edificio",
                "pelea en la calle",
                "accidente de moto"
            ]),
            "ubicacion_aproximada": random.choice(["zona_1", "zona_10", "zona_18", "mixco"]),
            "origen": random.choice(["usuario", "app", "punto_fisico"])
        }

def pick_lat_lon_for_zone(zone: str):
    lat_range, lon_range = ZONE_BOUNDS.get(zone, (LAT_RANGE, LON_RANGE))
    lat = round(random.uniform(*lat_range), 6)
    lon = round(random.uniform(*lon_range), 6)
    return lat, lon

def generate_event():
    event_type = random.choice(EVENT_TYPES)
    zone = random.choice(ZONES)
    lat, lon = pick_lat_lon_for_zone(zone)

    event = {
        "event_version": "1.0",
        "event_type": event_type,
        "event_id": str(uuid.uuid4()),
        "producer": "python-simulation",
        "source": "simulated",
        "correlation_id": str(uuid.uuid4()),
        "trace_id": str(uuid.uuid4()),
        "timestamp": datetime.datetime.utcnow().isoformat() + "Z",
        "partition_key": event_type,
        "geo": {
            "zone": zone,
            "lat": lat,
            "lon": lon
        },
        "severity": random.choice(SEVERITIES),
        "payload": random_payload(event_type)
    }
    return event

def main():
    counter = 0
    while True:
        event = generate_event()
        try:
            response = requests.post(URL, json=event, timeout=5)
            counter += 1
            print(f"[{counter}] Sent event_id={event['event_id']} type={event['event_type']} zone={event['geo']['zone']} -> {response.status_code}")
        except Exception as e:
            print(f"[ERROR] Could not send event: {e}")
        time.sleep(SLEEP_TIME)

if __name__ == "__main__":
    main()
