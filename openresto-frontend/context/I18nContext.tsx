import { createContext, type ReactNode, useContext, useMemo, useState } from "react";
import { detectLocale, getBrowserLanguages, type Locale } from "@/i18n/locale";
import { translate, type MessageKey, type MessageValues } from "@/i18n/messages";
import { StorageService } from "@/services/storage";

const STORAGE_KEY = "openresto-language";

type I18nContextValue = {
  locale: Locale;
  setLocale: (locale: Locale) => void;
  useBrowserLanguage: () => void;
  t: (key: MessageKey, values?: MessageValues) => string;
};

const I18nContext = createContext<I18nContextValue>({
  locale: "en",
  setLocale: () => {},
  useBrowserLanguage: () => {},
  t: (key, values) => translate("en", key, values),
});

function readStoredLocale(): string | null {
  return StorageService.getItem(STORAGE_KEY);
}

export function I18nProvider({
  children,
  initialLocale,
}: {
  children: ReactNode;
  initialLocale?: Locale;
}) {
  const [locale, setLocaleState] = useState<Locale>(() =>
    initialLocale ??
    detectLocale({ storedLocale: readStoredLocale(), browserLanguages: getBrowserLanguages() })
  );

  const value = useMemo<I18nContextValue>(
    () => ({
      locale,
      setLocale: (nextLocale) => {
        StorageService.setItem(STORAGE_KEY, nextLocale);
        setLocaleState(nextLocale);
      },
      useBrowserLanguage: () => {
        StorageService.removeItem(STORAGE_KEY);
        setLocaleState(detectLocale({ browserLanguages: getBrowserLanguages() }));
      },
      t: (key, values) => translate(locale, key, values),
    }),
    [locale]
  );

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}

export function useI18n() {
  return useContext(I18nContext);
}
