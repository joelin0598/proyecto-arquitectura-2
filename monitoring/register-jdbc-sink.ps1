param(
  [string]$ConnectUrl = 'http://localhost:8083',
  [string]$Topic = 'events.standardized'
)

# Read Postgres connection details from environment variables
$pgHost = $env:PGHOST
$pgPort = $env:PGPORT
$pgDatabase = $env:PGDATABASE
$pgUser = $env:PGUSER
$pgPassword = $env:PGPASSWORD

if (-not $pgHost) { Write-Error "PGHOST not set"; exit 1 }

$connectionUrl = "jdbc:postgresql://$pgHost`:$pgPort/$pgDatabase?ssl=true&sslmode=require"

$connector = @{
  name = "events-jdbc-sink"
  config = @{
    'connector.class' = 'io.confluent.connect.jdbc.JdbcSinkConnector'
    'tasks.max' = '1'
    'connection.url' = $connectionUrl
    'connection.user' = $pgUser
    'connection.password' = $pgPassword
    'topics' = $Topic
    'auto.create' = 'true'
    'pk.mode' = 'record_value'
    'pk.fields' = 'event_id'
    'insert.mode' = 'upsert'
    'table.name.format' = 'events'
    'key.converter' = 'org.apache.kafka.connect.json.JsonConverter'
    'value.converter' = 'org.apache.kafka.connect.json.JsonConverter'
    'value.converter.schemas.enable' = 'false'
    'transforms' = 'ExtractPayload'
    'transforms.ExtractPayload.type' = 'org.apache.kafka.connect.transforms.ExtractField$Value'
    'transforms.ExtractPayload.field' = 'payload'
  }
}

$json = $connector | ConvertTo-Json -Depth 10

Write-Host "Registering connector to $ConnectUrl/connectors ..."

try {
  $resp = Invoke-RestMethod -Method Post -Uri "$ConnectUrl/connectors" -ContentType 'application/json' -Body $json -ErrorAction Stop
  Write-Host "Connector registered:`n$($resp | ConvertTo-Json)"
} catch {
  Write-Error "Failed to register connector: $_"
  exit 1
}
