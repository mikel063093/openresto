# Aprovisionamiento de WhatsApp Business — solo prueba

## Estado y límite

- Este runbook sirve exclusivamente al entorno aislado de prueba de OpenResto.
- Cada paso externo está marcado `USER/AWAITING`: requiere una persona con acceso a Meta Business Manager, WhatsApp Business Account (WABA), DNS o el entorno de prueba.
- No autoriza despliegue productivo, cambios de DNS/router de producción, push, creación de secretos en Git ni impresión de secretos.
- La topología versionada y validada localmente está en [phase6-topology.md](../../n8n/test/docs/phase6-topology.md). Seguir este runbook no convierte esa definición en un despliegue.

## Fronteras que no se deben relajar

| Superficie | Estado requerido |
| --- | --- |
| `https://test-rest.joypaw.tech` | Aplicación OpenResto de prueba existente. |
| `https://n8n-test.joypaw.tech/webhook/*` | Único nuevo ingreso público permitido para Meta. |
| `https://n8n-test.joypaw.tech/webhook-test/*` | Permitido solo para pruebas manuales controladas. |
| Editor/API/REST/metrics de n8n | No público. |
| `reservation-bot-test` | Privado; no crear host, router ni DNS público. |
| `/api/private/channels/whatsapp/*` | Privado; los proxies públicos deben devolver `404`. |

La referencia histórica a `reservation-bot-test.joypaw.tech` es solo una comprobación de que **no** se publica. No solicitar ni crear un registro DNS o router para ese host.

## 1. Meta Business y WABA

- [ ] `USER/AWAITING` Confirmar acceso administrativo al Business Manager correcto, sin usar una cuenta de producción por defecto.
- [ ] `USER/AWAITING` Crear o seleccionar una aplicación Meta aislada de prueba y habilitar el producto WhatsApp.
- [ ] `USER/AWAITING` Crear o seleccionar una WABA de prueba asociada exclusivamente a esa aplicación.
- [ ] `USER/AWAITING` Registrar un número de teléfono de prueba en la WABA y anotar su identificador de teléfono y WABA en el almacén operativo autorizado, no en Git.
- [ ] `USER/AWAITING` Confirmar que las personas que probarán están autorizadas por la configuración de prueba de Meta.
- [ ] `USER/AWAITING` Generar/obtener el token de acceso de prueba con el menor alcance y vida útil compatibles con el envío de mensajes. Guardarlo solo como `N8N_TEST_META_ACCESS_TOKEN` en la inyección de secretos de `n8n-test`.

No cargar secretos de Meta en `reservation-bot-test`, el backend, archivos `appsettings*.json`, workflows exportados, fixtures, logs o tickets.

## 2. DNS y edge de prueba

- [ ] `USER/AWAITING` Confirmar que `test-rest.joypaw.tech` resuelve al edge de prueba esperado y no a infraestructura productiva.
- [ ] `USER/AWAITING` Crear o confirmar únicamente el registro de prueba para `n8n-test.joypaw.tech`, apuntado al edge de prueba que consume `traefik/test-rest.yml`.
- [ ] `USER/AWAITING` Verificar que el certificado TLS para `n8n-test.joypaw.tech` es válido antes de registrar el webhook en Meta.
- [ ] `USER/AWAITING` Confirmar que no existe registro DNS, router ni servicio público para `reservation-bot-test.joypaw.tech`.
- [ ] `USER/AWAITING` Tras un despliegue de prueba autorizado, comprobar desde fuera que `/webhook` y `/webhook-test` responden según el workflow y que `/`, `/rest/*` y `/metrics` no se exponen.
- [ ] `USER/AWAITING` Comprobar desde el edge público que `https://test-rest.joypaw.tech/api/private/channels/whatsapp/` sigue devolviendo `404`.

El único valor público que se configura en n8n es `N8N_TEST_WEBHOOK_BASE_URL=https://n8n-test.joypaw.tech`. No es un secreto.

## 3. Inyección de secretos por frontera

Los nombres están documentados en `.env.example`; los valores reales continúan `USER/AWAITING`.

| Destino | Variables permitidas |
| --- | --- |
| `n8n-test` | `N8N_TEST_POSTGRES_PASSWORD`, `N8N_TEST_ENCRYPTION_KEY`, `N8N_TEST_BASIC_AUTH_USER`, `N8N_TEST_BASIC_AUTH_PASSWORD`, `N8N_TEST_META_VERIFY_TOKEN`, `N8N_TEST_META_APP_SECRET`, `N8N_TEST_META_ACCESS_TOKEN`, `N8N_TEST_OPENAI_API_KEY`, `N8N_TEST_WHATSAPP_ASSERTION_*`, `N8N_TEST_BOT_INTERNAL_CREDENTIAL` |
| `reservation-bot-test` | `ReservationBot__InternalCredential`, `ReservationBot__OpenResto__PrivateApiCredential` |
| `test-rest-backend` | `WhatsAppChannel__InternalCallerCredential` y configuración de verificación `WhatsAppChannel__Assertion__*` |

- [ ] `USER/AWAITING` Inyectar los valores reales solamente mediante el gestor de secretos o variables externas del entorno de prueba.
- [ ] `USER/AWAITING` Mantener `N8N_TEST_BOT_INTERNAL_CREDENTIAL` igual a `ReservationBot__InternalCredential`, sin registrarlo en salida de comandos.
- [ ] `USER/AWAITING` Mantener la credencial privada de OpenResto solo entre bot y backend; n8n no debe llamarlo directamente.
- [ ] `USER/AWAITING` Verificar que Meta, OpenAI y claves privadas de assertion no aparecen en la configuración efectiva del bot.

## 4. Registro del webhook en Meta

- [ ] `USER/AWAITING` Fijar `N8N_TEST_IMAGE_DIGEST` al digest inmutable `sha256:` de una imagen n8n auditada; no usar tags ni `latest`.
- [ ] `USER/AWAITING` Después del despliegue, comprobar que `n8n-test-workflow-init` terminó correctamente y que los workflows `OpenResto WhatsApp Meta Verification v1` y `OpenResto WhatsApp Inbound Router v1` están activos antes de registrar el callback. Si falta alguno, detenerse y no registrar Meta.
- [ ] `USER/AWAITING` Cargar `N8N_TEST_META_VERIFY_TOKEN` en el almacén de secretos de `n8n-test`.
- [ ] `USER/AWAITING` Registrar en Meta la URL exacta `https://n8n-test.joypaw.tech/webhook/<ruta-versionada-del-workflow>`; no usar el editor n8n, `/rest`, el bot ni una URL productiva.
- [ ] `USER/AWAITING` Introducir en Meta el mismo verify token sin copiarlo a documentación o salida de terminal.
- [ ] `USER/AWAITING` Cargar `N8N_TEST_META_APP_SECRET` y activar la verificación `X-Hub-Signature-256` antes de aceptar eventos.
- [ ] `USER/AWAITING` Suscribir solo los campos de webhook necesarios para mensajes de la WABA de prueba.
- [ ] `USER/AWAITING` Realizar la verificación GET de Meta y registrar solo el resultado, fecha y correlación redaccionada en la evidencia operativa.

## 5. Plantillas y mensajería

- [ ] `USER/AWAITING` Determinar en Meta si el caso de prueba enviará mensajes fuera de la ventana de servicio y, de ser así, someter las plantillas requeridas para aprobación.
- [ ] `USER/AWAITING` Mantener plantillas de prueba separadas de producción y con texto `es-CO` compatible con los mensajes versionados.
- [ ] `USER/AWAITING` Registrar los nombres/estados de aprobación en el sistema operativo autorizado, sin incluir contenido sensible ni tokens en Git.
- [ ] `USER/AWAITING` No activar mensajes salientes de prueba hasta que la ruta de handoff, redacción de PII y los límites de destinatarios estén verificados.

## 6. Rotación HMAC con `kid`

Las assertions solo las emite n8n. OpenResto verifica una clave activa y, temporalmente, una anterior.

1. [ ] `USER/AWAITING` Crear una nueva clave y un `kid` en el gestor de secretos del entorno de prueba; no usar ni mostrar el valor en terminal.
2. [ ] `USER/AWAITING` Configurar primero el backend para aceptar la nueva clave como activa y conservar la anterior como `PreviousKid`/`PreviousSigningKey` durante el solapamiento.
3. [ ] `USER/AWAITING` Cambiar n8n para firmar con `N8N_TEST_WHATSAPP_ASSERTION_ACTIVE_KID` y `N8N_TEST_WHATSAPP_ASSERTION_ACTIVE_KEY` nuevos.
4. [ ] `USER/AWAITING` Ejecutar una prueba de assertion nueva y una assertion emitida antes del cambio, dentro del TTL máximo, registrando solo resultados redaccionados.
5. [ ] `USER/AWAITING` Esperar el TTL máximo de assertions más el margen operativo aprobado.
6. [ ] `USER/AWAITING` Eliminar la clave anterior del backend y de la inyección n8n; comprobar que un `kid` retirado falla cerrado.

No rotar por sobrescritura simultánea: el orden protege solicitudes legítimas en tránsito. La migración futura a RS256/EdDSA sigue diferida; conservar el mismo contrato de claims permitiría que n8n retenga la clave privada y OpenResto solo la pública.

## 7. Puerta de activación y evidencia

Antes de cualquier sandbox externo, todas las condiciones siguientes deben estar completas:

- [ ] `USER/AWAITING` Secretos reales inyectados por frontera y ausentes de Git/logs.
- [ ] `USER/AWAITING` DNS/TLS de prueba y edge webhook verificados.
- [ ] `USER/AWAITING` GET de verificación Meta y POST firmado válidos.
- [ ] `USER/AWAITING` Número y WABA de prueba habilitados; plantillas aprobadas si aplican.
- [ ] `USER/AWAITING` Validación externa de las rutas prohibidas y del `404` de la API privada.
- [ ] `USER/AWAITING` Aprobación explícita para el despliegue de prueba, si todavía no existe.

La Fase 8 debe capturar comandos reales y resultados para disponibilidad, create, list, update, cancel, handoff, dedupe, HMAC inválida, replay de assertion, redacción de PII y observabilidad. Si falta cualquiera de las dependencias externas, el estado correcto es bloqueado, no una activación parcial.
