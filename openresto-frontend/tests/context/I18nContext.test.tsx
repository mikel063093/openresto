/** @jest-environment jsdom */
import React from "react";
import { Text, TouchableOpacity, Platform } from "react-native";
import { fireEvent, render } from "@testing-library/react-native";
import { I18nProvider, useI18n } from "@/context/I18nContext";

Object.defineProperty(Platform, "OS", {
  get: jest.fn(() => "web"),
  configurable: true,
});

function Consumer() {
  const { locale, setLocale, t } = useI18n();
  return (
    <>
      <Text testID="locale">{locale}</Text>
      <Text testID="label">{t("navigation.locations")}</Text>
      <TouchableOpacity testID="english" onPress={() => setLocale("en")} />
    </>
  );
}

describe("I18nContext", () => {
  beforeEach(() => {
    localStorage.clear();
    Object.defineProperty(navigator, "languages", { configurable: true, value: ["es-CO", "en-US"] });
  });

  it("defaults to the browser's Spanish preference and persists an explicit language choice", () => {
    const { getByTestId } = render(
      <I18nProvider>
        <Consumer />
      </I18nProvider>
    );

    expect(getByTestId("locale").props.children).toBe("es");
    expect(getByTestId("label").props.children).toBe("Ubicaciones");

    fireEvent.press(getByTestId("english"));

    expect(getByTestId("locale").props.children).toBe("en");
    expect(localStorage.getItem("openresto-language")).toBe("en");
  });
});
