#!/usr/bin/env node
/**
 * Extract every reviewable frontend string candidate with TypeScript's AST.
 * This intentionally over-includes technical strings so reviewers never miss
 * a user-visible interpolation, ternary, title, accessibility label, or toast.
 */
const fs = require("fs");
const path = require("path");
const ts = require(path.resolve(__dirname, "../openresto-frontend/node_modules/typescript"));

const root = path.resolve(__dirname, "..");
const frontend = path.join(root, "openresto-frontend");
const outputDir = path.join(root, "i18n", "inventory");
const groups = {
  "public-ui": ["app/(user)", "components/booking", "components/restaurant", "components/layout", "components/common", "constants", "utils"],
  "admin-ui": ["app/admin", "components/admin"],
};
const userFacingAttributes = new Set(["accessibilityLabel", "accessibilityHint", "placeholder", "title", "label", "message", "headerTitle"]);

function filesIn(relativeDir) {
  const absolute = path.join(frontend, relativeDir);
  if (!fs.existsSync(absolute)) return [];
  const results = [];
  for (const entry of fs.readdirSync(absolute, { withFileTypes: true })) {
    const child = path.join(absolute, entry.name);
    if (entry.isDirectory()) results.push(...filesIn(path.relative(frontend, child)));
    else if (/\.(ts|tsx)$/.test(entry.name) && !child.includes(`${path.sep}tests${path.sep}`)) results.push(child);
  }
  return results;
}

function lineOf(file, position) {
  return file.getLineAndCharacterOfPosition(position).line + 1;
}

function sourceOf(node, file) {
  return node.getText(file).replace(/\s+/g, " ").slice(0, 260);
}

function isPotentialCopy(value) {
  return value.length > 1 && /[A-Za-zÁÉÍÓÚÑáéíóúñ]/.test(value) && !/^https?:\/\//.test(value);
}

function classify(node) {
  const parent = node.parent;
  if (ts.isJsxAttribute(parent) && ts.isIdentifier(parent.name) && userFacingAttributes.has(parent.name.text)) return "user-facing-attribute";
  if (ts.isJsxExpression(parent) || ts.isJsxElement(parent) || ts.isJsxSelfClosingElement(parent)) return "jsx-expression";
  if (ts.isConditionalExpression(parent) || ts.isBinaryExpression(parent) || ts.isTemplateExpression(parent)) return "dynamic-copy-candidate";
  return "review-required-string";
}

function extractFile(filePath) {
  const text = fs.readFileSync(filePath, "utf8");
  const file = ts.createSourceFile(filePath, text, ts.ScriptTarget.Latest, true, filePath.endsWith(".tsx") ? ts.ScriptKind.TSX : ts.ScriptKind.TS);
  const records = [];
  const seen = new Set();
  function add(node, raw, kind) {
    const value = raw.replace(/\s+/g, " ").trim();
    if (!isPotentialCopy(value)) return;
    const line = lineOf(file, node.getStart(file));
    const relative = path.relative(root, filePath);
    const fingerprint = `${relative}:${line}:${kind}:${value}`;
    if (seen.has(fingerprint)) return;
    seen.add(fingerprint);
    records.push({ id: null, sourceLocale: "en", targetLocale: "es-CO", status: "needs-review", file: relative, line, kind, source: value, translation: "", context: sourceOf(node.parent ?? node, file) });
  }
  function visit(node) {
    if (ts.isJsxText(node)) add(node, node.getText(file), "jsx-text");
    else if (ts.isStringLiteral(node) || ts.isNoSubstitutionTemplateLiteral(node)) {
      if (!ts.isImportDeclaration(node.parent) && !ts.isExportDeclaration(node.parent) && !ts.isModuleDeclaration(node.parent)) add(node, node.text, classify(node));
    } else if (ts.isTemplateExpression(node)) {
      const head = node.head.text.trim();
      if (isPotentialCopy(head)) add(node.head, head, "dynamic-template-fragment");
    }
    ts.forEachChild(node, visit);
  }
  visit(file);
  return records;
}

fs.mkdirSync(outputDir, { recursive: true });
const manifest = {};
for (const [batch, dirs] of Object.entries(groups)) {
  const collected = dirs.flatMap(filesIn).flatMap(extractFile).sort((a, b) => a.file.localeCompare(b.file) || a.line - b.line || a.source.localeCompare(b.source));
  collected.forEach((entry, index) => entry.id = `${batch}-${String(index + 1).padStart(4, "0")}`);
  const fileName = `${batch}-ast.json`;
  fs.writeFileSync(path.join(outputDir, fileName), JSON.stringify({ batch, sourceLocale: "en", targetLocale: "es-CO", entries: collected }, null, 2) + "\n");
  manifest[batch] = { file: `i18n/inventory/${fileName}`, entries: collected.length };
}
fs.writeFileSync(path.join(outputDir, "ast-manifest.json"), JSON.stringify(manifest, null, 2) + "\n");
console.log(JSON.stringify(manifest, null, 2));
