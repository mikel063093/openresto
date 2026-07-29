# Topología pública Fase 6 para WhatsApp de prueba

## Alcance
- Esta fase habilita solo el edge público mínimo para los webhooks de Meta en el entorno `test`.
- No hace despliegue real, no crea secretos reales, no toca DNS fuera de placeholders del repo y no publica el bot ni la API privada de OpenResto.

## Rutas públicas permitidas
- `https://test-rest.joypaw.tech`
  - Sigue sirviendo la app y la API pública existente de OpenResto.
- `https://n8n-test.joypaw.tech/webhook/*`
  - Entrada pública para webhooks productivos/versionados de `n8n-test`.
- `https://n8n-test.joypaw.tech/webhook-test/*`
  - Entrada pública para pruebas manuales de webhook en `n8n-test`.

## Rutas públicas prohibidas
- `https://n8n-test.joypaw.tech/`
- `https://n8n-test.joypaw.tech/rest/*`
- `https://n8n-test.joypaw.tech/metrics`
- cualquier host público para `reservation-bot-test`
- `/api/private/channels/whatsapp/*`

## Fronteras privadas obligatorias
- `n8n-test` puede hablar con `reservation-bot-test`.
- `reservation-bot-test` puede hablar con `test-rest-backend`.
- `n8n-test` no puede hablar con `test-rest-backend` por red interna compartida.
- `reservation-bot-test` no se publica en Traefik ni se adjunta al edge público.
- El editor/UI/API de `n8n` queda privado; el host público solo enruta `/webhook*`.

## Inyección de configuración
- Placeholder público:
  - `N8N_TEST_WEBHOOK_BASE_URL=https://n8n-test.joypaw.tech`
- Secretos que siguen siendo solo de `n8n`:
  - `N8N_TEST_META_VERIFY_TOKEN`
  - `N8N_TEST_META_APP_SECRET`
  - `N8N_TEST_META_ACCESS_TOKEN`
  - `N8N_TEST_OPENAI_API_KEY`
  - `N8N_TEST_WHATSAPP_ASSERTION_ACTIVE_KEY`
  - `N8N_TEST_WHATSAPP_ASSERTION_PREVIOUS_KEY`
- Secretos compartidos solo entre capas internas:
  - `N8N_TEST_BOT_INTERNAL_CREDENTIAL`
  - `ReservationBot__InternalCredential`
  - `WhatsAppChannel__InternalCallerCredential`

## Pruebas estáticas que deben seguir pasando
- El compose debe mostrar `WEBHOOK_URL` solo en `n8n-test`.
- Traefik debe exponer `n8n-test.joypaw.tech` solo con reglas `/webhook` y `/webhook-test`.
- No debe existir host público `reservation-bot-test.joypaw.tech`.
- Los templates públicos de Nginx deben seguir devolviendo `404` para `/api/private/channels/whatsapp/*`.
