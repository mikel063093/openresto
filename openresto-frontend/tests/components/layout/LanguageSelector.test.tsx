/** @jest-environment jsdom */
import React from "react";
import { Platform } from "react-native";
import { fireEvent, render } from "@testing-library/react-native";
import LanguageSelector from "@/components/layout/LanguageSelector";
import { I18nProvider } from "@/context/I18nContext";

Object.defineProperty(Platform, "OS", {
  get: jest.fn(() => "web"),
  configurable: true,
});

describe("LanguageSelector", () => {
  beforeEach(() => {
    localStorage.clear();
    Object.defineProperty(navigator, "languages", { configurable: true, value: ["es-CO"] });
  });

  it("shows the detected language and persists an explicit English selection", () => {
    const { getByLabelText, getByText } = render(
      <I18nProvider>
        <LanguageSelector />
      </I18nProvider>
    );

    expect(getByText("Español")).toBeTruthy();
    fireEvent.press(getByLabelText("Cambiar idioma a English"));

    expect(localStorage.getItem("openresto-language")).toBe("en");
    expect(getByText("English")).toBeTruthy();
  });
});
