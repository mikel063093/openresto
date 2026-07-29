# Base durable de n8n para WhatsApp en prueba

## Alcance de esta fase
- Esta carpeta deja lista la base durable de `n8n-test` para la Fase 3.
- No agrega workflows funcionales de Meta, LLM, confirmaciones ni handoff.
- No crea secretos reales, no hace despliegues y no modifica topología de producción.

## Decisión de persistencia
- Backend durable principal de `n8n`: `Postgres` en el servicio `n8n-test-postgres`.
- Estado local durable adicional de la orquestación: volúmenes separados montados en `n8n-test`.
- Datos de OpenResto siguen siendo autoridad exclusiva de reserva en SQLite del backend.

## Servicios definidos en `docker-compose.test-rest.yml`
- `reservation-bot-test`
  - Solo red interna `test-rest-internal`.
  - Recibe solicitudes cerradas desde `n8n-test`.
  - No recibe secretos de Meta ni de OpenAI.
- `n8n-test-postgres`
  - Persistencia durable de la base de datos de `n8n`.
- `n8n-test-volume-init`
  - Servicio efímero de preparación de permisos para los volúmenes de `n8n`.
  - No expone red ni recibe secretos.
- `n8n-test`
  - Usa `Postgres` para el estado interno de `n8n`.
  - Publica la entrada HTTPS solo por `n8n-test.joypaw.tech` vía Traefik.
  - Conserva artefactos de sesión/dedupe/orden/replay/outbound en volúmenes dedicados.

## Frontera de red
- Flujo permitido en esta fase:
  - público -> `n8n-test`
  - `n8n-test` -> `reservation-bot-test`
  - `reservation-bot-test` -> `test-rest-backend`
- Flujo negado o no expuesto:
  - público -> `reservation-bot-test`
  - público -> `/api/private/channels/whatsapp/*`
  - `n8n-test` -> rutas privadas arbitrarias de OpenResto fuera del contrato del bot

## Volúmenes durables
- `n8n_test_postgres_data`: base de datos de `n8n`.
- `n8n_test_data`: configuración general y credenciales cifradas de `n8n`.
- `n8n_test_binary_data`: binarios de ejecuciones si una fase posterior los necesita.
- `n8n_test_sessions_data`: sesiones conversacionales confirmables.
- `n8n_test_dedupe_data`: ledger de deduplicación de eventos Meta.
- `n8n_test_ordering_data`: marcas de agua para orden de eventos.
- `n8n_test_replay_data`: artefactos redaccionados para replay y depuración.
- `n8n_test_outbound_data`: correlación idempotente de mensajes salientes.

## Variables y secretos
- Los nombres permitidos están en [.env.example](/tmp/openresto-whatsapp-delivery/.env.example).
- Valores reales:
  - `USER/AWAITING`
- Propiedad obligatoria:
  - Meta, OpenAI y claves de assertion de WhatsApp: solo `n8n`.
  - Credencial interna del bot: compartida entre `n8n` y `reservation-bot-test`, nunca en prompts ni logs públicos.
  - Credencial interna del canal privado de OpenResto: usada por `reservation-bot-test`, no por modelos.

## Comandos de arranque local de prueba
```bash
docker compose -f docker-compose.test-rest.yml config
docker compose -f docker-compose.test-rest.yml up -d --build backend reservation-bot-test n8n-test-postgres n8n-test
docker compose -f docker-compose.test-rest.yml ps
docker compose -f docker-compose.test-rest.yml down
```

## Siguiente fase
- La Fase 4 debe importar/exportar workflows reales dentro de `n8n/test/workflows/` y consumir únicamente estos puntos de persistencia ya definidos.
