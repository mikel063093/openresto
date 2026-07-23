import { detectLocale, normalizeLocale } from "@/i18n/locale";

describe("locale resolution", () => {
  it.each([
    ["es", "es-CO"],
    ["es-CO", "es-CO"],
    ["es_MX", "es-CO"],
    ["en-US", "en"],
    ["fr-FR", "en"],
    [undefined, "en"],
  ] as const)("normalizes %s to %s", (input, expected) => {
    expect(normalizeLocale(input)).toBe(expected);
  });

  it("uses a valid stored override before browser language preferences", () => {
    expect(detectLocale({ storedLocale: "en", browserLanguages: ["es-CO", "en-US"] })).toBe("en");
  });

  it("uses the first supported browser language when no override exists", () => {
    expect(detectLocale({ storedLocale: null, browserLanguages: ["fr-FR", "es-CO", "en-US"] })).toBe(
      "es-CO"
    );
  });

  it("upgrades a legacy stored `es` override to `es-CO`", () => {
    expect(detectLocale({ storedLocale: "es", browserLanguages: ["en-US"] })).toBe("es-CO");
  });
});
