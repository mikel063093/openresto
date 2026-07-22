export type Locale = "en" | "es";

export function normalizeLocale(value?: string | null): Locale {
  return value?.trim().replace("_", "-").toLowerCase().startsWith("es") ? "es" : "en";
}

export interface LocaleDetectionInput {
  storedLocale?: string | null;
  browserLanguages?: readonly string[] | null;
}

export function detectLocale({ storedLocale, browserLanguages }: LocaleDetectionInput): Locale {
  if (storedLocale === "en" || storedLocale === "es") return storedLocale;

  const preferred = browserLanguages?.find((language) => normalizeLocale(language) === "es");
  return preferred ? "es" : "en";
}

export function getBrowserLanguages(): readonly string[] {
  if (typeof navigator === "undefined") return [];
  return navigator.languages?.length ? navigator.languages : navigator.language ? [navigator.language] : [];
}
