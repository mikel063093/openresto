/**
 * Shared test provider wrapper. Extracted from the 11+ identical inline copies
 * that existed across screen tests. (Bundle 13: Test Infrastructure & Fixtures.)
 *
 * Wraps UI in the SafeArea + Theme + Brand providers that screen components
 * expect, with zero insets (deterministic for layout-agnostic assertions).
 */
import React from "react";
import { render } from "@testing-library/react-native";
import { SafeAreaProvider } from "react-native-safe-area-context";
import { AppThemeProvider } from "@/context/ThemeContext";
import { BrandProvider } from "@/context/BrandContext";
import { I18nProvider } from "@/context/I18nContext";

const ZERO_INSETS = {
  frame: { x: 0, y: 0, width: 0, height: 0 },
  insets: { top: 0, left: 0, right: 0, bottom: 0 },
};

export function renderWithProviders(ui: React.ReactElement, options?: { locale?: "en" | "es-CO" }) {
  return render(
    <SafeAreaProvider initialMetrics={ZERO_INSETS}>
      <AppThemeProvider>
        <I18nProvider initialLocale={options?.locale}>
          <BrandProvider>{ui}</BrandProvider>
        </I18nProvider>
      </AppThemeProvider>
    </SafeAreaProvider>
  );
}
