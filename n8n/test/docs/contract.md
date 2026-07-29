# Contrato de workflows de WhatsApp de prueba

## Estado
- Fase 4 implementada en artefactos versionados `phase-4-v1`.
- Alcance estricto: solo workflows, documentación y validación estática en este worktree.
- No se agrega en esta fase enrutamiento público, DNS, despliegue, creación de secretos, ni push.

## Workflows exportados
- `n8n/test/workflows/whatsapp-meta-verification.json`
  - Verifica `GET` de Meta y responde el challenge.
- `n8n/test/workflows/whatsapp-inbound-router.json`
  - Recibe `POST`, valida HMAC antes de parsear, deduplica, controla orden, llama el LLM, exige confirmación, emite assertion y llama al bot.
- `n8n/test/workflows/whatsapp-confirmation-state-machine.json`
  - Mantiene el estado de confirmación explícita para `create`, `update`, `cancel` y `handoff`.
- `n8n/test/workflows/whatsapp-handoff.json`
  - Redacta PII, registra handoff por el bot y envía resumen saneado al destino humano.
- `n8n/test/workflows/whatsapp-observability.json`
  - Guarda replay redaccionado y correlación saliente en volúmenes durables.
- `n8n/test/workflows/whatsapp-template-messages.json`
  - Define respuestas `es-CO` y el envío saliente por Meta.

## Invariantes de seguridad
- `n8n` valida Meta y es el único emisor de assertions.
- La verificación HMAC ocurre antes de parsear el sobre Meta.
- Ningún workflow llama de forma directa a `/api/private/channels/whatsapp/*`.
- El único contrato aplicacional permitido es `POST /api/internal/reservation-bot/operations`.
- El LLM trabaja con salida JSON estricta y `tools: []`.
- Toda persistencia fuera de OpenResto pasa por redacción previa de PII.
- Los logs/replay no deben guardar correos, teléfonos completos, tokens ni secretos.
- Las mutaciones `create`, `update`, `cancel` y `handoff` requieren confirmación explícita.
- La Fase 6 define el edge público de webhook de prueba; el editor/API de `n8n`, el bot y la API privada de OpenResto permanecen privados.

## Credenciales permitidas por nombre
- `Meta Verify Token (placeholder)`
- `Meta App Secret (placeholder)`
- `Meta Access Token (placeholder)`
- `LLM Provider API Key (placeholder)`
- `Reservation Bot Internal Credential (placeholder)`
- `WhatsApp Assertion Active Key (placeholder)`
- `WhatsApp Assertion Previous Key (placeholder)`

## Variables esperadas
- `N8N_TEST_META_VERIFY_TOKEN`
- `N8N_TEST_META_APP_SECRET`
- `N8N_TEST_META_ACCESS_TOKEN`
- `N8N_TEST_OPENAI_API_KEY`
- `N8N_TEST_WHATSAPP_ASSERTION_ISSUER`
- `N8N_TEST_WHATSAPP_ASSERTION_AUDIENCE`
- `N8N_TEST_WHATSAPP_ASSERTION_ACTIVE_KID`
- `N8N_TEST_WHATSAPP_ASSERTION_ACTIVE_KEY`
- `N8N_TEST_WHATSAPP_ASSERTION_PREVIOUS_KID`
- `N8N_TEST_WHATSAPP_ASSERTION_PREVIOUS_KEY`
- `N8N_TEST_BOT_INTERNAL_CREDENTIAL`

## Contrato del bot
- Operaciones permitidas:
  - `availability`
  - `create`
  - `list`
  - `detail`
  - `update`
  - `cancel`
  - `occasionCatalog`
  - `handoff`
- Campos obligatorios por request:
  - `operation`
  - exactamente un cuerpo tipado compatible con la operación
- Headers obligatorios:
  - `X-OpenResto-Channel-Assertion`
  - `X-Correlation-Id`
- No se permiten:
  - URLs arbitrarias
  - nombres arbitrarios de herramientas
  - rutas privadas directas de OpenResto

## Orden y deduplicación durable
- Ledger de dedupe:
  - `/data/channel-state/dedupe/{meta-message-id}.json`
- Watermark de orden:
  - `/data/channel-state/ordering/{wa-id-normalizado}.json`
- Sesiones de confirmación:
  - `/data/channel-state/sessions/{wa-id-normalizado}.json`
- Replay saneado:
  - `/data/channel-state/replay/{correlationId}.json`
- Correlación saliente:
  - `/data/channel-state/outbound/{correlationId}.json`

## Assertion de WhatsApp
- Se emite solo después de:
  - HMAC Meta válida
  - remitente verificado y normalizado
  - dedupe/orden aceptados
  - decisión del estado de confirmación
- Claims requeridos:
  - `iss`
  - `aud`
  - `sub`
  - `jti`
  - `scope`
  - `openresto:channel_action`
  - `iat`
  - `exp`
- El header debe incluir `kid`.

## Flujo dual-key
1. OpenResto carga la nueva clave de verificación.
2. `n8n` cambia `N8N_TEST_WHATSAPP_ASSERTION_ACTIVE_KID` y `N8N_TEST_WHATSAPP_ASSERTION_ACTIVE_KEY`.
3. Se espera el TTL máximo de assertions.
4. OpenResto retira `PreviousKid` y `PreviousSigningKey`.

## LLM y plantillas
- El prompt y la respuesta operan en `es-CO`.
- La salida del LLM debe cumplir un JSON Schema estricto con:
  - `intent`
  - `confidence`
  - `confirmationNeeded`
  - `handoff`
  - `handoffReason`
  - `entities`
- Las respuestas al cliente usan mensajes `es-CO` cerrados:
  - confirmación
  - disponibilidad
  - creación
  - actualización
  - cancelación
  - catálogo
  - handoff
  - intent no soportado

## Límites de esta fase
- El workflow exportado es importable y versionado.
- La activación real en Meta/WABA queda `USER/AWAITING`.
- El edge público de `n8n-test.joypaw.tech` y `reservation-bot-test.joypaw.tech` sigue diferido a la Fase 6.
