param(
  [int]$Count = 20,
  [double]$Delay = 0.5
)

Write-Host "Generating $Count events with $Delay second(s) delay..."

function New-Event([string]$type) {
  $id = [guid]::NewGuid().ToString()
  $cid = [guid]::NewGuid().ToString()
  $tid = [guid]::NewGuid().ToString()
  $ts = (Get-Date).ToString('o')
  $geo = @{ zone = 'Zona 10'; lat = 14.6091; lon = -90.5252 }

  switch ($type) {
    'panic.button' {
      $tipos = @('panico','emergencia','incendio')
      $contexts = @('movil','quiosco','web')
      $payload = @{ 
        tipo_de_alerta = $tipos[(Get-Random -Maximum $tipos.Length)];
        identificador_dispositivo = "BTN-Z10-{0}" -f (Get-Random -Maximum 999);
        user_context = $contexts[(Get-Random -Maximum $contexts.Length)]
      }
    }
    'sensor.lpr' {
      $models = @('Toyota Corolla','Ford Mustang','Honda Civic','Nissan Altima')
      $colors = @('rojo','blanco','negro','azul')
      $payload = @{ 
        placa_vehicular = "O-{0:D3}{1}" -f (Get-Random -Maximum 999),(Get-Random -Minimum 1 -Maximum 9);
        velocidad_estimada = (Get-Random -Minimum 20 -Maximum 140);
        modelo_vehiculo = $models[(Get-Random -Maximum $models.Length)];
        color_vehiculo = $colors[(Get-Random -Maximum $colors.Length)];
        ubicacion_sensor = "Diagonal {0}" -f (Get-Random -Minimum 1 -Maximum 20)
      }
    }
    'sensor.speed' {
      $dirs = @('Norte','Sur','Este','Oeste','Noreste','Noroeste')
      $payload = @{ 
        velocidad_detectada = (Get-Random -Minimum 20 -Maximum 140);
        sensor_id = "SPEED-Z10-{0:D3}" -f (Get-Random -Maximum 999);
        direccion = $dirs[(Get-Random -Maximum $dirs.Length)]
      }
    }
    'sensor.acoustic' {
      $types = @('disparo','explosion','vidrio_roto')
      $payload = @{ 
        tipo_sonido_detectado = $types[(Get-Random -Maximum $types.Length)];
        nivel_decibeles = (Get-Random -Minimum 60 -Maximum 170);
        probabilidad_evento_critico = [math]::Round((Get-Random),2)
      }
    }
    'citizen.report' {
      $tipos = @('accidente','incendio','altercado')
      $origenes = @('usuario','app','punto_fisico')
      $msgs = @('Accidente con heridos','Incendio en vehículo','Pelea en la calle','Persona desmayada')
      $payload = @{ 
        tipo_evento = $tipos[(Get-Random -Maximum $tipos.Length)];
        mensaje_descriptivo = $msgs[(Get-Random -Maximum $msgs.Length)];
        ubicacion_aproximada = "Calle {0} y Avenida {1}" -f (Get-Random -Minimum 1 -Maximum 50),(Get-Random -Minimum 1 -Maximum 20);
        origen = $origenes[(Get-Random -Maximum $origenes.Length)]
      }
    }
    default {
      $payload = @{ raw = 'unknown' }
    }
  }

  $event = @{ 
    event_version = '1.0';
    event_type = $type;
    event_id = $id;
    producer = 'js-sim';
    source = 'simulated';
    correlation_id = $cid;
    trace_id = $tid;
    timestamp = $ts;
    partition_key = 'zona-10';
    geo = $geo;
    severity = 'warning';
    payload = $payload
  }

  return $event
}

$types = @('panic.button','sensor.lpr','sensor.speed','sensor.acoustic','citizen.report')

for ($i=0; $i -lt $Count; $i++) {
  $t = $types[(Get-Random -Maximum $types.Length)]
  $ev = New-Event $t
  $json = $ev | ConvertTo-Json -Depth 10
  try {
    $resp = Invoke-RestMethod -Method Post -Uri 'http://localhost:5000/events' -ContentType 'application/json' -Body $json -ErrorAction Stop
    Write-Host "[$i] Sent: $($ev.event_type) $($ev.event_id) -> Response: $resp" -ForegroundColor Green
  } catch {
    Write-Host "[$i] Failed to send event $($ev.event_id): $_" -ForegroundColor Red
  }
  Start-Sleep -Seconds $Delay
}

Write-Host "Done. Generated $Count events."
