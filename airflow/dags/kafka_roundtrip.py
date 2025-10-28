from __future__ import annotations
from datetime import datetime
import time
import uuid

from airflow import DAG
from airflow.decorators import task

BOOTSTRAP_SERVERS = "kafka:9092"
TOPIC = "kafka_roundtrip_demo"  # usa el MISMO en produce y consume

with DAG(
    dag_id="kafka_roundtrip",
    start_date=datetime(2024, 1, 1),
    schedule=None,
    catchup=False,
    tags=["demo", "kafka"],
) as dag:

    @task()
    def produce():
        from confluent_kafka import Producer
        p = Producer({
            "bootstrap.servers": BOOTSTRAP_SERVERS,
            "acks": "all",
        })
        payload = f"hola-desde-airflow-{uuid.uuid4()}"
        p.produce(TOPIC, value=payload.encode("utf-8"))
        p.flush(10.0)
        print(f"Enviado: {payload}")

    @task()
    def consume():
        from confluent_kafka import Consumer, KafkaException
        conf = {
            "bootstrap.servers": BOOTSTRAP_SERVERS,
            "group.id": f"roundtrip-{int(time.time())}",  # grupo nuevo por run
            "auto.offset.reset": "earliest",               # lee desde el inicio
            "enable.auto.commit": False,
            # logs útiles de librdkafka si quieres más detalle:
            # "debug": "cgrp,topic,fetch",
        }
        c = Consumer(conf)
        c.subscribe([TOPIC])

        deadline = time.time() + 60  # dale tiempo suficiente
        msg = None
        try:
            while time.time() < deadline:
                m = c.poll(1.0)
                if m is None:
                    continue
                if m.error():
                    raise KafkaException(m.error())
                msg = m
                break
        finally:
            c.close()

        if not msg:
            raise ValueError("No se recibió el mensaje esperado en 60s.")

        print(f"Recibido: {msg.value().decode('utf-8')}")

    produce() >> consume()
