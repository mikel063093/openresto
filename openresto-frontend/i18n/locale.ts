export type Locale = "en" | "es-CO";

export const DEFAULT_LOCALE: Locale = "en";
export const DEFAULT_SPANISH_LOCALE: Locale = "es-CO";

export function normalizeLocale(value?: string | null): Locale {
  return value?.trim().replace("_", "-").toLowerCase().startsWith("es")
    ? DEFAULT_SPANISH_LOCALE
    : DEFAULT_LOCALE;
}

export function toIntlLocale(locale: Locale): string {
  return locale;
}

export interface LocaleDetectionInput {
  storedLocale?: string | null;
  browserLanguages?: readonly string[] | null;
}

export function detectLocale({ storedLocale, browserLanguages }: LocaleDetectionInput): Locale {
  if (storedLocale === DEFAULT_LOCALE || storedLocale === DEFAULT_SPANISH_LOCALE) {
    return storedLocale;
  }
  if (storedLocale === "es") {
    return DEFAULT_SPANISH_LOCALE;
  }

  const preferred = browserLanguages?.find(
    (language) => normalizeLocale(language) === DEFAULT_SPANISH_LOCALE
  );
  return preferred ? DEFAULT_SPANISH_LOCALE : DEFAULT_LOCALE;
}

export function getBrowserLanguages(): readonly string[] {
  if (typeof navigator === "undefined") return [];
  return navigator.languages?.length ? navigator.languages : navigator.language ? [navigator.language] : [];
}
