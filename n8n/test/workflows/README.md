# Workflows de la Fase 4

## Estado
- Esta carpeta ya contiene los exports JSON versionados de la Fase 4.
- Los archivos se regeneran con `node scripts/generate-phase4-n8n-workflows.cjs`.
- El alcance sigue siendo test-only: no se activan dominios públicos, DNS ni despliegues.

## Archivos
- `whatsapp-meta-verification.json`
- `whatsapp-inbound-router.json`
- `whatsapp-confirmation-state-machine.json`
- `whatsapp-handoff.json`
- `whatsapp-observability.json`
- `whatsapp-template-messages.json`

## Restricciones vigentes
- Usar solo credenciales placeholder por nombre.
- Mantener el único contrato de aplicación en `POST /api/internal/reservation-bot/operations`.
- No introducir llamadas directas a `/api/private/channels/whatsapp/*`.
- Mantener toda persistencia fuera de OpenResto con PII redaccionada.
