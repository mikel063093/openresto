# Diseño de almacenamiento durable

## Objetivo
- Garantizar que el estado mínimo de la orquestación de WhatsApp sobreviva reinicios del contenedor en el entorno de prueba.

## Backend explícito
- Base de datos de `n8n`: `Postgres` persistido en `n8n_test_postgres_data`.
- Artefactos de orquestación: volúmenes dedicados montados en `n8n-test`.
- Un init container efímero `n8n-test-volume-init` prepara permisos de escritura antes del arranque de `n8n-test`.

## Mapa de artefactos
- `/data/channel-state/sessions`
  - estado de sesión
  - referencia de confirmación pendiente
- `/data/channel-state/dedupe`
  - ids de eventos Meta procesados
  - estado de deduplicación
- `/data/channel-state/ordering`
  - watermark por conversación/remitente
  - timestamp/sequence más reciente aceptado
- `/data/channel-state/replay`
  - payloads redaccionados
  - snapshots útiles para depuración sin PII completa
- `/data/channel-state/outbound`
  - correlación de mensaje saliente
  - idempotencia de reintentos

## Reglas de contenido
- Todo payload guardado para replay debe quedar redaccionado antes de persistirse.
- No guardar secretos, tokens completos ni prompts con credenciales en estos volúmenes.
- Los artefactos deben usar identificadores correlacionables con `correlationId` y `wa_id` normalizado solo cuando la fase posterior lo requiera.

## Respaldo y limpieza
- Respaldo mínimo de prueba:
  - snapshot del volumen `n8n_test_postgres_data`
  - snapshot de `n8n_test_data`
  - export puntual de `replay` si se investiga un incidente
- Limpieza:
  - `sessions`, `dedupe`, `ordering` y `outbound` pueden podarse por TTL en Fase 4.
  - `replay` debe tener retención más corta y redacción obligatoria.

## Verificación de reinicio en esta fase
- La validación de Fase 3 comprueba que los archivos marcadores en estos montajes sobreviven a `docker compose restart n8n-test`.
