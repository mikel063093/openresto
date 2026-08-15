# Secretos y credenciales de `n8n-test`

## Regla principal
- Esta carpeta documenta puntos de inyección.
- No se guardan secretos reales en Git.

## Secretos `USER/AWAITING`
- `N8N_TEST_POSTGRES_PASSWORD`
- `N8N_TEST_ENCRYPTION_KEY`
- `N8N_TEST_BASIC_AUTH_USER`
- `N8N_TEST_BASIC_AUTH_PASSWORD`
- `N8N_TEST_META_VERIFY_TOKEN`
- `N8N_TEST_META_APP_SECRET`
- `N8N_TEST_META_ACCESS_TOKEN`
- `N8N_TEST_OPENAI_API_KEY`
- `N8N_TEST_WHATSAPP_ASSERTION_ACTIVE_KID`
- `N8N_TEST_WHATSAPP_ASSERTION_ACTIVE_KEY`
- `N8N_TEST_WHATSAPP_ASSERTION_PREVIOUS_KID`
- `N8N_TEST_WHATSAPP_ASSERTION_PREVIOUS_KEY`
- `N8N_TEST_BOT_INTERNAL_CREDENTIAL`

## Propiedad por componente
- `n8n-test`
  - Meta verify token
  - Meta app secret
  - Meta access token
  - OpenAI API key
  - assertion active/previous key material
  - credencial interna para llamar al bot
- `reservation-bot-test`
  - solo `ReservationBot__InternalCredential`
  - solo `ReservationBot__OpenResto__PrivateApiCredential`
- `test-rest-backend`
  - solo verificación del canal privado WhatsApp ya existente

## Reglas operativas
- No copiar secretos de Meta/OpenAI a `OpenRestoReservationBot/appsettings*.json`.
- No documentar valores reales en issues, commits, planes ni exports de workflows.
- No usar secretos reales en fixtures o pruebas automatizadas versionadas.

## Rotación esperada para assertions
1. Cargar nueva clave de verificación en OpenResto.
2. Cambiar en `n8n` el `kid` y la clave activa.
3. Esperar el TTL máximo de assertions.
4. Retirar la clave anterior de OpenResto.
