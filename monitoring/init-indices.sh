#!/bin/bash
echo "⏳ Esperando que Elasticsearch esté disponible..."
curl -X PUT http://localhost:9200/alerts -H "Content-Type: application/json" -d '
{
  "mappings": {
    "properties": {
      "timestamp": { "type": "date" },
      "alert_id": { "type": "keyword" },
      "type": { "type": "keyword" },
      "score": { "type": "float" },
      "zone": { "type": "keyword" },
      "window_start": { "type": "date" },
      "window_end": { "type": "date" },
      "evidence": { "type": "text" }
    }
  }
}'

curl -X PUT http://localhost:9200/events -H "Content-Type: application/json" -d '
{
  "mappings": {
    "properties": {
      "event_id": { "type": "keyword" },
      "event_type": { "type": "keyword" },
      "timestamp": { "type": "date" },
      "geo": {
        "properties": {
          "zone": { "type": "keyword" },
          "lat": { "type": "float" },
          "lon": { "type": "float" }
        }
      },
      "severity": { "type": "keyword" },
      "payload": { "type": "text" }
    }
  }
}'

