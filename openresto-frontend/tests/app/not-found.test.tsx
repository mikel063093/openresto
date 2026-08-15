/**
 * @jest-environment jsdom
 */
import React from "react";
import { render, screen } from "@testing-library/react-native";
import NotFoundScreen from "@/app/+not-found";
import { AppThemeProvider } from "@/context/ThemeContext";
import { BrandProvider } from "@/context/BrandContext";
import { I18nProvider } from "@/context/I18nContext";

global.fetch = jest.fn(() =>
  Promise.resolve({
    ok: true,
    json: () => Promise.resolve({ appName: "Open Resto", primaryColor: "#0a7ea4" }),
  })
) as jest.Mock;

jest.mock("expo-router", () => ({
  usePathname: jest.fn(() => "/some/missing/page"),
  Link: ({ children, href: _href, style }: any) =>
    require("react").createElement("Text", { style }, children),
}));

jest.mock("@expo/vector-icons", () => ({
  Ionicons: () => null,
}));

describe("NotFoundScreen", () => {
  const renderScreen = (locale: "en" | "es-CO" = "en") =>
    render(
      <AppThemeProvider>
        <I18nProvider initialLocale={locale}>
          <BrandProvider>
            <NotFoundScreen />
          </BrandProvider>
        </I18nProvider>
      </AppThemeProvider>
    );

  it("renders 404 code", () => {
    renderScreen();
    expect(screen.getByText("404")).toBeTruthy();
  });

  it("renders page not found title", () => {
    renderScreen();
    expect(screen.getByText("Page not found")).toBeTruthy();
  });

  it("renders localized Spanish copy", () => {
    renderScreen("es-CO");
    expect(screen.getByText("Página no encontrada")).toBeTruthy();
    expect(screen.getByText("Ir al inicio")).toBeTruthy();
  });

  it("renders the current pathname", () => {
    renderScreen();
    expect(screen.getByText("/some/missing/page")).toBeTruthy();
  });

  it("renders Go to home link", () => {
    renderScreen();
    expect(screen.getByText("Go to home")).toBeTruthy();
  });
});
