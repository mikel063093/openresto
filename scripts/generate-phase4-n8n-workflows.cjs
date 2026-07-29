#!/usr/bin/env node

const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

const repoRoot = path.resolve(__dirname, "..");
const workflowDir = path.join(repoRoot, "n8n", "test", "workflows");
const workflowVersion = "phase-4-v1";

function stableId(value) {
  return crypto.createHash("sha1").update(value).digest("hex").slice(0, 16);
}

function node(name, type, position, parameters = {}, extra = {}) {
  return {
    id: stableId(name),
    name,
    type,
    typeVersion: extra.typeVersion ?? 1,
    position,
    parameters,
    ...Object.fromEntries(
      Object.entries(extra).filter(([key]) => key !== "typeVersion"),
    ),
  };
}

function workflow(name, nodes, connections, extra = {}) {
  return {
    name,
    nodes,
    connections,
    pinData: {},
    settings: {
      executionOrder: "v1",
      saveDataErrorExecution: "none",
      saveDataSuccessExecution: "none",
      saveDataProgressExecution: false,
      saveManualExecutions: false,
    },
    staticData: null,
    tags: [],
    triggerCount: 0,
    versionId: stableId(`${name}:${workflowVersion}`),
    meta: {
      openrestoWorkflowVersion: workflowVersion,
      templateCredsSetupCompleted: true,
      ...extra.meta,
    },
    active: false,
  };
}

function writeWorkflow(fileName, definition) {
  const outputPath = path.join(workflowDir, fileName);
  fs.writeFileSync(outputPath, `${JSON.stringify(definition, null, 2)}\n`, "utf8");
  console.log(`wrote ${path.relative(repoRoot, outputPath)}`);
}

const verificationNodes = [
  node(
    "Meta Verify Webhook",
    "n8n-nodes-base.webhook",
    [-560, 0],
    {
      httpMethod: "GET",
      path: "whatsapp/meta",
      responseMode: "responseNode",
      options: {},
    },
    { typeVersion: 2.1, webhookId: stableId("meta-verify-webhook") },
  ),
  node(
    "Validate Verify Query",
    "n8n-nodes-base.code",
    [-280, 0],
    {
      mode: "runOnceForAllItems",
      jsCode: `const query = $json.query ?? {};\nconst mode = query["hub.mode"] ?? query.mode ?? null;\nconst challenge = query["hub.challenge"] ?? query.challenge ?? "";\nconst token = query["hub.verify_token"] ?? query.verify_token ?? "";\nconst expected = $env.N8N_TEST_META_VERIFY_TOKEN ?? "";\nconst verified = mode === "subscribe" && token.length > 0 && token === expected;\nreturn [{\n  json: {\n    verified,\n    responseCode: verified ? 200 : 403,\n    responseBody: verified ? String(challenge) : "forbidden",\n    contractVersion: "${workflowVersion}",\n    credentialPlaceholders: [\n      "Meta Verify Token (placeholder)"\n    ]\n  }\n}];`,
    },
    { typeVersion: 2 },
  ),
  node(
    "Respond Verify Challenge",
    "n8n-nodes-base.respondToWebhook",
    [0, 0],
    {
      respondWith: "text",
      responseBody: "={{ $json.responseBody }}",
      options: {
        responseCode: "={{ $json.responseCode }}",
      },
    },
    { typeVersion: 1.1 },
  ),
];

const verificationConnections = {
  "Meta Verify Webhook": {
    main: [[{ node: "Validate Verify Query", type: "main", index: 0 }]],
  },
  "Validate Verify Query": {
    main: [[{ node: "Respond Verify Challenge", type: "main", index: 0 }]],
  },
};

const inboundNodes = [
  node(
    "Meta Inbound Webhook",
    "n8n-nodes-base.webhook",
    [-1820, 0],
    {
      httpMethod: "POST",
      path: "whatsapp/meta",
      responseMode: "responseNode",
      options: {
        rawBody: true,
      },
    },
    { typeVersion: 2.1, webhookId: stableId("meta-inbound-webhook") },
  ),
  node(
    "Extract Raw Envelope",
    "n8n-nodes-base.code",
    [-1540, 0],
    {
      mode: "runOnceForAllItems",
      jsCode: `const headerBag = Object.fromEntries(Object.entries($json.headers ?? {}).map(([key, value]) => [String(key).toLowerCase(), value]));\nconst rawBody = $json.bodyRaw ?? $json.rawBody ?? JSON.stringify($json.body ?? $json);\nreturn [{\n  json: {\n    headers: headerBag,\n    rawBody,\n    receivedAtUtc: new Date().toISOString(),\n    contractVersion: "${workflowVersion}"\n  }\n}];`,
    },
    { typeVersion: 2 },
  ),
  node(
    "Verify Meta HMAC Before Parse",
    "n8n-nodes-base.code",
    [-1260, 0],
    {
      mode: "runOnceForAllItems",
      jsCode: `const crypto = require("crypto");\nconst appSecret = $env.N8N_TEST_META_APP_SECRET ?? "";\nconst signatureHeader = String($json.headers["x-hub-signature-256"] ?? "");\nconst signature = signatureHeader.startsWith("sha256=") ? signatureHeader.slice(7) : "";\nconst expected = crypto.createHmac("sha256", appSecret).update($json.rawBody, "utf8").digest("hex");\nconst valid = Boolean(appSecret) && Boolean(signature) && crypto.timingSafeEqual(Buffer.from(expected, "hex"), Buffer.from(signature || "00", "hex"));\nreturn [{\n  json: {\n    ...$json,\n    metaSignatureValid: valid,\n    signatureAlgorithm: "sha256",\n    invalidReason: valid ? null : "invalid-meta-signature"\n  }\n}];`,
    },
    { typeVersion: 2 },
  ),
  node(
    "Parse Meta Envelope",
    "n8n-nodes-base.code",
    [-980, 0],
    {
      mode: "runOnceForAllItems",
      jsCode: `if (!$json.metaSignatureValid) {\n  return [{ json: { ...$json, shouldStop: true, responseCode: 401, responseBody: "invalid signature" } }];\n}\nconst envelope = JSON.parse($json.rawBody);\nconst entry = envelope.entry?.[0] ?? {};\nconst change = entry.changes?.[0] ?? {};\nconst message = change.value?.messages?.[0] ?? null;\nconst contact = change.value?.contacts?.[0] ?? null;\nconst waId = String(contact?.wa_id ?? message?.from ?? "").trim();\nconst normalized = waId.replace(/\\D/g, "");\nconst correlationId = [message?.id ?? "meta", normalized || "unknown", change.value?.metadata?.phone_number_id ?? "phone"].join(":").slice(0, 64);\nreturn [{\n  json: {\n    ...$json,\n    shouldStop: !message || !normalized,\n    responseCode: !message || !normalized ? 200 : 202,\n    responseBody: !message || !normalized ? "ignored" : "accepted",\n    envelope,\n    message,\n    verifiedSender: {\n      waId,\n      normalized,\n      displayName: contact?.profile?.name ?? null\n    },\n    conversation: {\n      eventId: message?.id ?? correlationId,\n      eventTimestamp: Number(message?.timestamp ?? 0),\n      phoneNumberId: change.value?.metadata?.phone_number_id ?? "",\n      correlationId\n    },\n    inboundText: message?.text?.body ?? "",\n    inboundType: message?.type ?? "unsupported"\n  }\n}];`,
    },
    { typeVersion: 2 },
  ),
  node(
    "Dedupe And Ordering Gate",
    "n8n-nodes-base.executeCommand",
    [-700, 0],
    {
      command: `node -e "const fs=require('fs'); const path=require('path'); const payload=JSON.parse(process.env.STATE_PAYLOAD); const dedupeDir='/data/channel-state/dedupe'; const orderingDir='/data/channel-state/ordering'; fs.mkdirSync(dedupeDir,{recursive:true}); fs.mkdirSync(orderingDir,{recursive:true}); const dedupeKey=payload.eventId.replace(/[^a-zA-Z0-9._-]/g,'_'); const orderKey=payload.verifiedSenderNormalized.replace(/[^a-zA-Z0-9._-]/g,'_') || 'unknown'; const dedupePath=path.join(dedupeDir,dedupeKey + '.json'); const orderingPath=path.join(orderingDir,orderKey + '.json'); const duplicate=fs.existsSync(dedupePath); let outOfOrder=false; const nextTs=Number(payload.eventTimestamp || 0); if (fs.existsSync(orderingPath)) { const previous=JSON.parse(fs.readFileSync(orderingPath,'utf8')); outOfOrder=nextTs > 0 && Number(previous.lastTimestamp || 0) > nextTs; } if (!duplicate) { fs.writeFileSync(dedupePath, JSON.stringify({ eventId: payload.eventId, correlationId: payload.correlationId, storedAtUtc: new Date().toISOString() })); } if (!outOfOrder && nextTs > 0) { fs.writeFileSync(orderingPath, JSON.stringify({ sender: payload.verifiedSenderNormalized, lastTimestamp: nextTs, correlationId: payload.correlationId, storedAtUtc: new Date().toISOString() })); } console.log(JSON.stringify({ duplicate, outOfOrder }));"`,
      environmentVariablesUi: {
        parameter: [
          {
            name: "STATE_PAYLOAD",
            value:
              "={{ JSON.stringify({ eventId: $json.conversation.eventId, eventTimestamp: $json.conversation.eventTimestamp, verifiedSenderNormalized: $json.verifiedSender.normalized, correlationId: $json.conversation.correlationId }) }}",
          },
        ],
      },
    },
    { typeVersion: 1 },
  ),
  node(
    "Interpret Dedupe Gate",
    "n8n-nodes-base.code",
    [-420, 0],
    {
      mode: "runOnceForAllItems",
      jsCode: `const commandStdout = String($json.stdout ?? "{}").trim();\nconst gate = JSON.parse(commandStdout || "{}");\nconst duplicate = Boolean(gate.duplicate);\nconst outOfOrder = Boolean(gate.outOfOrder);\nreturn [{\n  json: {\n    ...$json,\n    duplicate,\n    outOfOrder,\n    shouldStop: $json.shouldStop || duplicate || outOfOrder,\n    responseCode: $json.shouldStop ? $json.responseCode : 200,\n    responseBody: $json.shouldStop ? $json.responseBody : "EVENT_RECEIVED"\n  }\n}];`,
    },
    { typeVersion: 2 },
  ),
  node(
    "Build LLM Request",
    "n8n-nodes-base.set",
    [-140, 0],
    {
      keepOnlySet: false,
      values: {
        string: [
          {
            name: "llmSystemPrompt",
            value:
              "Eres un clasificador de intenciones de reservas por WhatsApp para OpenResto. Responde solo JSON válido en español colombiano y nunca llames herramientas.",
          },
          {
            name: "llmUserPrompt",
            value:
              "={{ 'Mensaje del cliente: ' + $json.inboundText + '\\nTipo de mensaje: ' + $json.inboundType + '\\nDebes devolver intent, confidence, entities, confirmationNeeded y handoffReason.' }}",
          },
        ],
      },
      options: {},
    },
    { typeVersion: 3.4 },
  ),
  node(
    "LLM Provider Abstraction",
    "n8n-nodes-base.httpRequest",
    [140, 0],
    {
      method: "POST",
      url: "https://llm-provider.invalid/v1/responses",
      sendHeaders: true,
      headerParameters: {
        parameters: [
          {
            name: "Content-Type",
            value: "application/json",
          },
        ],
      },
      sendBody: true,
      contentType: "json",
      bodyParametersJson:
        "={{ JSON.stringify({ model: 'provider-placeholder', input: [{ role: 'system', content: $json.llmSystemPrompt }, { role: 'user', content: $json.llmUserPrompt }], tools: [], text: { format: { type: 'json_schema', name: 'whatsapp_intent', strict: true, schema: { type: 'object', additionalProperties: false, required: ['intent', 'confidence', 'confirmationNeeded', 'handoff', 'entities'], properties: { intent: { type: 'string', enum: ['availability', 'create', 'list', 'detail', 'update', 'cancel', 'occasionCatalog', 'handoff', 'unsupported'] }, confidence: { type: 'number' }, confirmationNeeded: { type: 'boolean' }, handoff: { type: 'boolean' }, handoffReason: { type: ['string', 'null'] }, entities: { type: 'object', additionalProperties: false, properties: { restaurantId: { type: ['integer', 'null'] }, reservationId: { type: ['integer', 'null'] }, date: { type: ['string', 'null'] }, seats: { type: ['integer', 'null'] }, customerEmail: { type: ['string', 'null'] }, customerName: { type: ['string', 'null'] }, occasionCatalogItemIds: { type: ['array', 'null'], items: { type: 'integer' } }, expectedConcurrencyToken: { type: ['integer', 'null'] }, specialRequests: { type: ['string', 'null'] } } } } } } } }) }}",
      options: {
        timeout: 15000,
      },
    },
    {
      typeVersion: 4.2,
      credentials: {
        httpHeaderAuth: {
          id: stableId("llm-provider-credential"),
          name: "LLM Provider API Key (placeholder)",
        },
      },
    },
  ),
  node(
    "Validate Structured LLM Output",
    "n8n-nodes-base.code",
    [420, 0],
    {
      mode: "runOnceForAllItems",
      jsCode: `const raw = $json.output?.[0]?.content?.[0]?.text ?? $json.body?.output?.[0]?.content?.[0]?.text ?? $json.response?.output_text ?? $json.output_text ?? $json.text ?? "{}";\nconst parsed = typeof raw === "string" ? JSON.parse(raw) : raw;\nconst allowed = new Set(["availability", "create", "list", "detail", "update", "cancel", "occasionCatalog", "handoff", "unsupported"]);\nif (!allowed.has(parsed.intent)) {\n  throw new Error("schema-reject: intent no permitido");\n}\nreturn [{\n  json: {\n    ...$json,\n    llmResult: parsed,\n    shouldStop: false\n  }\n}];`,
    },
    { typeVersion: 2 },
  ),
  node(
    "Confirmation State Machine",
    "n8n-nodes-base.executeWorkflow",
    [700, 0],
    {
      workflowId: "OpenResto WhatsApp Confirmation State Machine v1",
      mode: "each",
      options: {
        waitForSubWorkflow: true,
      },
      input: "={{ { verifiedSender: $json.verifiedSender, conversation: $json.conversation, llmResult: $json.llmResult, inboundText: $json.inboundText } }}",
    },
    { typeVersion: 1.1 },
  ),
  node(
    "Issue Assertion After Verified Sender",
    "n8n-nodes-base.code",
    [980, 0],
    {
      mode: "runOnceForAllItems",
      jsCode: `const crypto = require("crypto");\nconst activeKid = $env.N8N_TEST_WHATSAPP_ASSERTION_ACTIVE_KID ?? "active-placeholder";\nconst header = Buffer.from(JSON.stringify({ alg: "HS256", typ: "JWT", kid: activeKid })).toString("base64url");\nconst now = Math.floor(Date.now() / 1000);\nconst action = $json.botRequest?.operation === "availability" || $json.botRequest?.operation === "list" || $json.botRequest?.operation === "detail" || $json.botRequest?.operation === "occasionCatalog"\n  ? "reservations.read"\n  : "reservations.mutate";\nconst payload = {\n  iss: $env.N8N_TEST_WHATSAPP_ASSERTION_ISSUER ?? "n8n-test",\n  aud: $env.N8N_TEST_WHATSAPP_ASSERTION_AUDIENCE ?? "openresto-whatsapp-private-api",\n  sub: $json.verifiedSender.waId,\n  jti: String($json.conversation.correlationId) + ":" + String(now),\n  scope: "openresto.whatsapp reservations",\n  'openresto:channel_action': action,\n  iat: now,\n  exp: now + 120\n};\nconst encodedPayload = Buffer.from(JSON.stringify(payload)).toString("base64url");\nconst signingInput = header + "." + encodedPayload;\nconst signature = crypto.createHmac("sha256", $env.N8N_TEST_WHATSAPP_ASSERTION_ACTIVE_KEY ?? "placeholder-signing-key").update(signingInput).digest("base64url");\nreturn [{ json: { ...$json, issuedAssertion: signingInput + "." + signature, assertionKid: activeKid } }];`,
    },
    { typeVersion: 2 },
  ),
  node(
    "Call Reservation Bot",
    "n8n-nodes-base.httpRequest",
    [1260, 0],
    {
      method: "POST",
      url: "http://reservation-bot-test:8080/api/internal/reservation-bot/operations",
      sendHeaders: true,
      headerParameters: {
        parameters: [
          {
            name: "X-OpenResto-Channel-Assertion",
            value: "={{ $json.issuedAssertion }}",
          },
          {
            name: "X-Correlation-Id",
            value: "={{ $json.conversation.correlationId }}",
          },
          {
            name: "Content-Type",
            value: "application/json",
          },
        ],
      },
      sendBody: true,
      contentType: "json",
      bodyParametersJson: "={{ JSON.stringify($json.botRequest) }}",
      options: {
        timeout: 15000,
      },
    },
    {
      typeVersion: 4.2,
      credentials: {
        httpHeaderAuth: {
          id: stableId("reservation-bot-credential"),
          name: "Reservation Bot Internal Credential (placeholder)",
        },
      },
    },
  ),
  node(
    "Template Messages",
    "n8n-nodes-base.executeWorkflow",
    [1540, 0],
    {
      workflowId: "OpenResto WhatsApp Template Messages v1",
      mode: "each",
      options: {
        waitForSubWorkflow: true,
      },
      input:
        "={{ { verifiedSender: $json.verifiedSender, conversation: $json.conversation, llmResult: $json.llmResult, confirmation: $json.confirmation, botRequest: $json.botRequest, botResponse: $json.body ?? $json, templateContext: $json.templateContext ?? {} } }}",
    },
    { typeVersion: 1.1 },
  ),
  node(
    "Observability Workflow",
    "n8n-nodes-base.executeWorkflow",
    [1820, 0],
    {
      workflowId: "OpenResto WhatsApp Observability v1",
      mode: "each",
      options: {
        waitForSubWorkflow: true,
      },
      input:
        "={{ { verifiedSender: $json.verifiedSender, conversation: $json.conversation, inboundText: $json.inboundText, botRequest: $json.botRequest, botResponse: $json.body ?? $json, llmResult: $json.llmResult, templateContext: $json.templateContext ?? {}, replayBodyRaw: $json.rawBody } }}",
    },
    { typeVersion: 1.1 },
  ),
  node(
    "Respond To Meta",
    "n8n-nodes-base.respondToWebhook",
    [2100, 0],
    {
      respondWith: "text",
      responseBody: "={{ $json.responseBody ?? 'EVENT_RECEIVED' }}",
      options: {
        responseCode: "={{ $json.responseCode ?? 200 }}",
      },
    },
    { typeVersion: 1.1 },
  ),
];

const inboundConnections = {
  "Meta Inbound Webhook": {
    main: [[{ node: "Extract Raw Envelope", type: "main", index: 0 }]],
  },
  "Extract Raw Envelope": {
    main: [[{ node: "Verify Meta HMAC Before Parse", type: "main", index: 0 }]],
  },
  "Verify Meta HMAC Before Parse": {
    main: [[{ node: "Parse Meta Envelope", type: "main", index: 0 }]],
  },
  "Parse Meta Envelope": {
    main: [[{ node: "Dedupe And Ordering Gate", type: "main", index: 0 }]],
  },
  "Dedupe And Ordering Gate": {
    main: [[{ node: "Interpret Dedupe Gate", type: "main", index: 0 }]],
  },
  "Interpret Dedupe Gate": {
    main: [[{ node: "Build LLM Request", type: "main", index: 0 }]],
  },
  "Build LLM Request": {
    main: [[{ node: "LLM Provider Abstraction", type: "main", index: 0 }]],
  },
  "LLM Provider Abstraction": {
    main: [[{ node: "Validate Structured LLM Output", type: "main", index: 0 }]],
  },
  "Validate Structured LLM Output": {
    main: [[{ node: "Confirmation State Machine", type: "main", index: 0 }]],
  },
  "Confirmation State Machine": {
    main: [[{ node: "Issue Assertion After Verified Sender", type: "main", index: 0 }]],
  },
  "Issue Assertion After Verified Sender": {
    main: [[{ node: "Call Reservation Bot", type: "main", index: 0 }]],
  },
  "Call Reservation Bot": {
    main: [[{ node: "Template Messages", type: "main", index: 0 }]],
  },
  "Template Messages": {
    main: [[{ node: "Observability Workflow", type: "main", index: 0 }]],
  },
  "Observability Workflow": {
    main: [[{ node: "Respond To Meta", type: "main", index: 0 }]],
  },
};

const confirmationNodes = [
  node(
    "Execute Workflow Trigger",
    "n8n-nodes-base.executeWorkflowTrigger",
    [-620, 0],
    {},
    { typeVersion: 1 },
  ),
  node(
    "Evaluate Confirmation State",
    "n8n-nodes-base.code",
    [-260, 0],
    {
      mode: "runOnceForAllItems",
      jsCode: `const payload = $json;\nconst intent = payload.llmResult?.intent ?? "unsupported";\nconst entities = payload.llmResult?.entities ?? {};\nconst text = String(payload.inboundText ?? "").trim().toLowerCase();\nconst affirmative = ["si", "sí", "confirmo", "dale", "listo", "acepto"];\nconst negative = ["no", "cancelar", "detener"];\nconst isAffirmative = affirmative.some(value => text === value || text.includes(value));\nconst isNegative = negative.some(value => text === value || text.includes(value));\nconst mutationIntents = new Set(["create", "update", "cancel", "handoff"]);\nconst requiresConfirmation = mutationIntents.has(intent);\nconst idempotencyBase = String(payload.conversation.correlationId);\nconst confirmation = {\n  intent,\n  requiresConfirmation,\n  confirmed: requiresConfirmation ? isAffirmative : true,\n  denied: requiresConfirmation ? isNegative : false,\n  prompt: null,\n  status: "ready"\n};\nif (requiresConfirmation && !isAffirmative && !isNegative) {\n  confirmation.status = "awaiting-confirmation";\n  confirmation.prompt = intent === "create"\n    ? "Vas a reservar para " + String(entities.seats ?? "?") + " personas en el restaurante " + String(entities.restaurantId ?? "seleccionado") + " el " + String(entities.date ?? "la fecha indicada") + ". Responde SI para confirmar o NO para cancelar."\n    : intent === "update"\n      ? "Voy a cambiar tu reserva " + String(entities.reservationId ?? "") + ". Responde SI para confirmar o NO para cancelar."\n      : intent === "cancel"\n        ? "Voy a cancelar tu reserva " + String(entities.reservationId ?? "") + ". Responde SI para confirmar o NO para cancelar."\n        : "Voy a pasarte con una persona del restaurante. Responde SI para continuar o NO para volver al menú.";\n}\nif (confirmation.denied) {\n  confirmation.status = "cancelled-by-user";\n}\nconst operationBodies = {\n  availability: { operation: "availability", availability: { restaurantId: entities.restaurantId, date: entities.date, seats: entities.seats } },\n  create: { operation: "create", create: { restaurantId: entities.restaurantId, date: entities.date, seats: entities.seats, customerEmail: entities.customerEmail, customerName: entities.customerName, specialRequests: entities.specialRequests ?? null, confirmed: confirmation.confirmed, idempotencyKey: idempotencyBase + ":create", occasionCatalogItemIds: entities.occasionCatalogItemIds ?? [] } },\n  list: { operation: "list", list: {} },\n  detail: { operation: "detail", detail: { reservationId: entities.reservationId } },\n  update: { operation: "update", update: { reservationId: entities.reservationId, date: entities.date, seats: entities.seats, confirmed: confirmation.confirmed, idempotencyKey: idempotencyBase + ":update", expectedConcurrencyToken: entities.expectedConcurrencyToken ?? 0 } },\n  cancel: { operation: "cancel", cancel: { reservationId: entities.reservationId, confirmed: confirmation.confirmed, idempotencyKey: idempotencyBase + ":cancel", expectedConcurrencyToken: entities.expectedConcurrencyToken ?? 0, reason: "Solicitud del cliente por WhatsApp" } },\n  occasionCatalog: { operation: "occasionCatalog", occasionCatalog: { restaurantId: entities.restaurantId } },\n  handoff: { operation: "handoff", handoff: { restaurantId: entities.restaurantId, bookingId: entities.reservationId ?? null, summary: payload.llmResult?.handoffReason ?? payload.inboundText, confirmed: confirmation.confirmed, idempotencyKey: idempotencyBase + ":handoff" } }\n};\nconst templateKey = confirmation.status === "awaiting-confirmation"\n  ? "confirmation_prompt"\n  : confirmation.status === "cancelled-by-user"\n    ? "confirmation_cancelled"\n    : intent === "unsupported"\n      ? "unsupported"\n      : intent + "_result";\nreturn [{ json: { ...payload, confirmation, botRequest: operationBodies[intent] ?? operationBodies.handoff, templateContext: { templateKey, confirmationPrompt: confirmation.prompt } } }];`,
    },
    { typeVersion: 2 },
  ),
  node(
    "Persist Session State",
    "n8n-nodes-base.executeCommand",
    [80, 0],
    {
      command: `node -e "const fs=require('fs'); const path=require('path'); const payload=JSON.parse(process.env.SESSION_PAYLOAD); const dir='/data/channel-state/sessions'; fs.mkdirSync(dir,{recursive:true}); const key=(payload.sender || 'unknown').replace(/[^a-zA-Z0-9._-]/g,'_'); const out=path.join(dir, key + '.json'); fs.writeFileSync(out, JSON.stringify(payload, null, 2)); console.log(JSON.stringify({ persisted: true, path: out }));"`,
      environmentVariablesUi: {
        parameter: [
          {
            name: "SESSION_PAYLOAD",
            value:
              "={{ JSON.stringify({ sender: $json.verifiedSender.normalized, correlationId: $json.conversation.correlationId, intent: $json.llmResult.intent, confirmation: $json.confirmation, templateContext: $json.templateContext, updatedAtUtc: new Date().toISOString() }) }}",
          },
        ],
      },
    },
    { typeVersion: 1 },
  ),
];

const confirmationConnections = {
  "Execute Workflow Trigger": {
    main: [[{ node: "Evaluate Confirmation State", type: "main", index: 0 }]],
  },
  "Evaluate Confirmation State": {
    main: [[{ node: "Persist Session State", type: "main", index: 0 }]],
  },
};

const handoffNodes = [
  node(
    "Execute Workflow Trigger",
    "n8n-nodes-base.executeWorkflowTrigger",
    [-820, 0],
    {},
    { typeVersion: 1 },
  ),
  node(
    "Sanitize Handoff Summary",
    "n8n-nodes-base.code",
    [-520, 0],
    {
      mode: "runOnceForAllItems",
      jsCode: `const redact = (value) => String(value ?? "")\n  .replace(/[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,}/gi, "[correo-redactado]")\n  .replace(/\\+?\\d[\\d\\s-]{6,}\\d/g, "[telefono-redactado]")\n  .slice(0, 600);\nconst summary = redact($json.summary ?? $json.inboundText ?? "");\nreturn [{ json: { ...$json, sanitizedSummary: summary } }];`,
    },
    { typeVersion: 2 },
  ),
  node(
    "Call Reservation Bot Handoff",
    "n8n-nodes-base.httpRequest",
    [-220, 0],
    {
      method: "POST",
      url: "http://reservation-bot-test:8080/api/internal/reservation-bot/operations",
      sendHeaders: true,
      headerParameters: {
        parameters: [
          {
            name: "X-OpenResto-Channel-Assertion",
            value: "={{ $json.issuedAssertion }}",
          },
          {
            name: "X-Correlation-Id",
            value: "={{ $json.conversation.correlationId }}",
          },
          {
            name: "Content-Type",
            value: "application/json",
          },
        ],
      },
      sendBody: true,
      contentType: "json",
      bodyParametersJson:
        "={{ JSON.stringify({ operation: 'handoff', handoff: { restaurantId: $json.restaurantId ?? $json.llmResult?.entities?.restaurantId, bookingId: $json.bookingId ?? $json.llmResult?.entities?.reservationId ?? null, summary: $json.sanitizedSummary, confirmed: true, idempotencyKey: String($json.conversation.correlationId) + ':handoff' } }) }}",
      options: {
        timeout: 15000,
      },
    },
    {
      typeVersion: 4.2,
      credentials: {
        httpHeaderAuth: {
          id: stableId("reservation-bot-credential"),
          name: "Reservation Bot Internal Credential (placeholder)",
        },
      },
    },
  ),
  node(
    "Send Human Summary Via Meta",
    "n8n-nodes-base.httpRequest",
    [80, 0],
    {
      method: "POST",
      url: "https://graph.facebook.com/v23.0/{{$json.meta.phoneNumberId}}/messages",
      sendHeaders: true,
      headerParameters: {
        parameters: [
          {
            name: "Content-Type",
            value: "application/json",
          },
        ],
      },
      sendBody: true,
      contentType: "json",
      bodyParametersJson:
        "={{ JSON.stringify({ messaging_product: 'whatsapp', to: $json.body?.handoff?.result?.handoffWhatsAppE164 ?? $json.handoff?.result?.handoffWhatsAppE164, type: 'text', text: { body: 'Handoff OpenResto ' + String($json.conversation.correlationId) + ': ' + String($json.sanitizedSummary) } }) }}",
      options: {
        timeout: 15000,
      },
    },
    {
      typeVersion: 4.2,
      credentials: {
        httpHeaderAuth: {
          id: stableId("meta-access-token-credential"),
          name: "Meta Access Token (placeholder)",
        },
      },
    },
  ),
];

const handoffConnections = {
  "Execute Workflow Trigger": {
    main: [[{ node: "Sanitize Handoff Summary", type: "main", index: 0 }]],
  },
  "Sanitize Handoff Summary": {
    main: [[{ node: "Call Reservation Bot Handoff", type: "main", index: 0 }]],
  },
  "Call Reservation Bot Handoff": {
    main: [[{ node: "Send Human Summary Via Meta", type: "main", index: 0 }]],
  },
};

const observabilityNodes = [
  node(
    "Execute Workflow Trigger",
    "n8n-nodes-base.executeWorkflowTrigger",
    [-720, 0],
    {},
    { typeVersion: 1 },
  ),
  node(
    "PII Redaction",
    "n8n-nodes-base.code",
    [-420, 0],
    {
      mode: "runOnceForAllItems",
      jsCode: `const redact = (value) => String(value ?? "")\n  .replace(/[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,}/gi, "[correo-redactado]")\n  .replace(/\\+?\\d[\\d\\s-]{6,}\\d/g, "[telefono-redactado]");\nconst maskedSender = $json.verifiedSender?.normalized ? String($json.verifiedSender.normalized).slice(0, 4) + "***" : null;\nconst sanitized = {\n  correlationId: $json.conversation?.correlationId,\n  sender: maskedSender,\n  inboundText: redact($json.inboundText),\n  rawBody: redact($json.replayBodyRaw),\n  llmResult: $json.llmResult,\n  botRequest: $json.botRequest,\n  botResponse: $json.botResponse,\n  storedAtUtc: new Date().toISOString()\n};\nreturn [{ json: { ...$json, redactedReplay: sanitized } }];`,
    },
    { typeVersion: 2 },
  ),
  node(
    "Persist Redacted Replay",
    "n8n-nodes-base.executeCommand",
    [-120, 0],
    {
      command: `node -e "const fs=require('fs'); const path=require('path'); const payload=JSON.parse(process.env.REPLAY_PAYLOAD); const dir='/data/channel-state/replay'; fs.mkdirSync(dir,{recursive:true}); const key=(payload.correlationId || 'unknown').replace(/[^a-zA-Z0-9._-]/g,'_'); const out=path.join(dir, key + '.json'); fs.writeFileSync(out, JSON.stringify(payload, null, 2)); console.log(JSON.stringify({ persisted: true, path: out }));"`,
      environmentVariablesUi: {
        parameter: [
          {
            name: "REPLAY_PAYLOAD",
            value: "={{ JSON.stringify($json.redactedReplay) }}",
          },
        ],
      },
    },
    { typeVersion: 1 },
  ),
  node(
    "Persist Outbound Correlation",
    "n8n-nodes-base.executeCommand",
    [180, 0],
    {
      command: `node -e "const fs=require('fs'); const path=require('path'); const payload=JSON.parse(process.env.OUTBOUND_PAYLOAD); const dir='/data/channel-state/outbound'; fs.mkdirSync(dir,{recursive:true}); const key=(payload.correlationId || 'unknown').replace(/[^a-zA-Z0-9._-]/g,'_'); const out=path.join(dir, key + '.json'); fs.writeFileSync(out, JSON.stringify(payload, null, 2)); console.log(JSON.stringify({ persisted: true, path: out }));"`,
      environmentVariablesUi: {
        parameter: [
          {
            name: "OUTBOUND_PAYLOAD",
            value:
              "={{ JSON.stringify({ correlationId: $json.conversation.correlationId, sender: $json.verifiedSender.normalized, templateKey: $json.templateContext?.templateKey ?? null, storedAtUtc: new Date().toISOString() }) }}",
          },
        ],
      },
    },
    { typeVersion: 1 },
  ),
];

const observabilityConnections = {
  "Execute Workflow Trigger": {
    main: [[{ node: "PII Redaction", type: "main", index: 0 }]],
  },
  "PII Redaction": {
    main: [[{ node: "Persist Redacted Replay", type: "main", index: 0 }]],
  },
  "Persist Redacted Replay": {
    main: [[{ node: "Persist Outbound Correlation", type: "main", index: 0 }]],
  },
};

const templateNodes = [
  node(
    "Execute Workflow Trigger",
    "n8n-nodes-base.executeWorkflowTrigger",
    [-680, 0],
    {},
    { typeVersion: 1 },
  ),
  node(
    "Build es-CO Reply",
    "n8n-nodes-base.code",
    [-360, 0],
    {
      mode: "runOnceForAllItems",
      jsCode: `const key = $json.templateContext?.templateKey ?? "unsupported";\nconst templates = {\n  confirmation_prompt: $json.templateContext?.confirmationPrompt ?? "Necesito tu confirmación explícita para continuar.",\n  confirmation_cancelled: "Entendido. No hice ningún cambio en tu reserva.",\n  availability_result: "Ya revisé la disponibilidad. En seguida te comparto las opciones confirmadas.",\n  create_result: "Tu solicitud de reserva quedó registrada. Te comparto el detalle enseguida.",\n  update_result: "Tu cambio quedó procesado. Revisa por favor el detalle actualizado.",\n  cancel_result: "La cancelación quedó registrada correctamente.",\n  list_result: "Estas son las reservas asociadas a tu número verificado.",\n  detail_result: "Aquí tienes el detalle de tu reserva.",\n  occasionCatalog_result: "Estas son las ocasiones disponibles para tu reserva.",\n  unsupported: "No pude resolver esa solicitud de forma segura. Te voy a pasar con una persona del restaurante.",\n  handoff_result: "Te voy a comunicar con una persona del restaurante para continuar por este mismo WhatsApp."\n};\nreturn [{ json: { ...$json, outboundMessage: templates[key] ?? templates.unsupported } }];`,
    },
    { typeVersion: 2 },
  ),
  node(
    "Send Meta Reply",
    "n8n-nodes-base.httpRequest",
    [-40, 0],
    {
      method: "POST",
      url: "https://graph.facebook.com/v23.0/{{$json.conversation.phoneNumberId}}/messages",
      sendHeaders: true,
      headerParameters: {
        parameters: [
          {
            name: "Content-Type",
            value: "application/json",
          },
        ],
      },
      sendBody: true,
      contentType: "json",
      bodyParametersJson:
        "={{ JSON.stringify({ messaging_product: 'whatsapp', recipient_type: 'individual', to: $json.verifiedSender.waId, type: 'text', text: { preview_url: false, body: $json.outboundMessage } }) }}",
      options: {
        timeout: 15000,
      },
    },
    {
      typeVersion: 4.2,
      credentials: {
        httpHeaderAuth: {
          id: stableId("meta-access-token-credential"),
          name: "Meta Access Token (placeholder)",
        },
      },
    },
  ),
];

const templateConnections = {
  "Execute Workflow Trigger": {
    main: [[{ node: "Build es-CO Reply", type: "main", index: 0 }]],
  },
  "Build es-CO Reply": {
    main: [[{ node: "Send Meta Reply", type: "main", index: 0 }]],
  },
};

writeWorkflow(
  "whatsapp-meta-verification.json",
  workflow("OpenResto WhatsApp Meta Verification v1", verificationNodes, verificationConnections, {
    meta: {
      purpose: "Meta GET verification",
    },
  }),
);

writeWorkflow(
  "whatsapp-inbound-router.json",
  workflow("OpenResto WhatsApp Inbound Router v1", inboundNodes, inboundConnections, {
    meta: {
      purpose: "Meta POST inbound routing with HMAC, LLM, confirmation, assertion, and bot dispatch",
    },
  }),
);

writeWorkflow(
  "whatsapp-confirmation-state-machine.json",
  workflow("OpenResto WhatsApp Confirmation State Machine v1", confirmationNodes, confirmationConnections, {
    meta: {
      purpose: "Explicit confirmation gate and durable session state",
    },
  }),
);

writeWorkflow(
  "whatsapp-handoff.json",
  workflow("OpenResto WhatsApp Handoff v1", handoffNodes, handoffConnections, {
    meta: {
      purpose: "Unsupported intent and operator handoff",
    },
  }),
);

writeWorkflow(
  "whatsapp-observability.json",
  workflow("OpenResto WhatsApp Observability v1", observabilityNodes, observabilityConnections, {
    meta: {
      purpose: "PII-redacted replay and outbound correlation persistence",
    },
  }),
);

writeWorkflow(
  "whatsapp-template-messages.json",
  workflow("OpenResto WhatsApp Template Messages v1", templateNodes, templateConnections, {
    meta: {
      purpose: "es-CO reply templates and Meta send path",
    },
  }),
);
