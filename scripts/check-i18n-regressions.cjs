#!/usr/bin/env node
const fs = require("fs");
const path = require("path");

const root = path.resolve(__dirname, "..");
const allowlists = JSON.parse(
  fs.readFileSync(path.join(root, "i18n", "regression-allowlists.json"), "utf8")
);

const frontendChecks = [
  {
    file: "openresto-frontend/app/+not-found.tsx",
    banned: ["Page not found", "Go to home"],
  },
  {
    file: "openresto-frontend/app/(user)/_layout.tsx",
    banned: ["Locations", "Book a Table", "Booking Confirmed", "Find My Booking", "Restaurant"],
  },
  {
    file: "openresto-frontend/components/booking/HoldStatusBanner.tsx",
    banned: [
      "Checking availability…",
      "Table held - expires in",
      "Table not available for this date. Please choose another.",
      "Your table hold expired. Availability may have changed.",
      "Refresh page",
    ],
  },
  {
    file: "openresto-frontend/components/restaurant/LocationListItem.tsx",
    banned: [
      "Apple Maps",
      "Open in Apple Maps",
      "Opening hours",
      "Book a table",
      "Seating & tables",
    ],
  },
];

const allowedFrontend = new Set(allowlists.frontend.allowlisted_literals || []);
const frontendFailures = [];
for (const check of frontendChecks) {
  const absolute = path.join(root, check.file);
  const text = fs.readFileSync(absolute, "utf8");
  for (const banned of check.banned) {
    if (allowedFrontend.has(`${check.file}::${banned}`)) continue;
    if (text.includes(banned)) {
      frontendFailures.push(`${check.file}: raw visible literal "${banned}"`);
    }
  }
}

const apiLocalizationSource = fs.readFileSync(
  path.join(root, "OpenRestoApi", "Infrastructure", "Localization", "ApiLocalization.cs"),
  "utf8"
);
const staticTranslations = new Set(
  [...apiLocalizationSource.matchAll(/\["([^"]+)"\]\s*=/g)].map((match) => match[1])
);
const dynamicAllowlist = (allowlists.backend.allowlisted_dynamic_patterns || []).map(
  (pattern) => new RegExp(pattern)
);

const controllerFiles = [];
const controllersDir = path.join(root, "OpenRestoApi", "Controllers");
for (const entry of fs.readdirSync(controllersDir, { withFileTypes: true })) {
  if (entry.isFile() && entry.name.endsWith(".cs")) {
    controllerFiles.push(path.join(controllersDir, entry.name));
  }
}

const backendFailures = [];
for (const file of controllerFiles) {
  const relative = path.relative(root, file);
  const text = fs.readFileSync(file, "utf8");

  for (const match of text.matchAll(/message\s*=\s*"([^"]+)"/g)) {
    if (!staticTranslations.has(match[1])) {
      backendFailures.push(`${relative}: missing ApiLocalization static translation for "${match[1]}"`);
    }
  }

  for (const match of text.matchAll(/message\s*=\s*\$"([^"]+)"/g)) {
    const normalized = match[1].replace(/\{[^}]+\}/g, ".+");
    const asRegex = new RegExp(`^${normalized}$`);
    const allowed = dynamicAllowlist.some((pattern) => pattern.source === asRegex.source || pattern.test(match[1].replace(/\{[^}]+\}/g, "sample")));
    if (!allowed) {
      backendFailures.push(`${relative}: dynamic message requires allowlist review: "${match[1]}"`);
    }
  }
}

const failures = [...frontendFailures, ...backendFailures];
if (failures.length > 0) {
  console.error("i18n regression checks failed:");
  for (const failure of failures) {
    console.error(`- ${failure}`);
  }
  process.exit(1);
}

console.log("i18n regression checks passed");
