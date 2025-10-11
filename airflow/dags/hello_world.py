from datetime import datetime
from airflow import DAG
from airflow.operators.python import PythonOperator

def hi():
    print("Hola Airflow!")

with DAG(
    dag_id="hello_world",
    start_date=datetime(2024, 1, 1),
    schedule_interval=None,
    catchup=False,
    tags=["test"],
):
    PythonOperator(task_id="say_hi", python_callable=hi)
