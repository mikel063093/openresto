import React from "react";
import { render, screen, fireEvent } from "@testing-library/react-native";
import HoldStatusBanner from "@/components/booking/HoldStatusBanner";
import { I18nProvider } from "@/context/I18nContext";

jest.mock("@/hooks/use-color-scheme", () => ({
  useColorScheme: () => "light",
}));

describe("HoldStatusBanner", () => {
  function renderBanner(ui: React.ReactElement, locale: "en" | "es-CO" = "en") {
    return render(<I18nProvider initialLocale={locale}>{ui}</I18nProvider>);
  }

  it("returns null when hasSelection is false", () => {
    const { toJSON } = renderBanner(
      <HoldStatusBanner holdStatus="idle" secondsLeft={0} hasSelection={false} />
    );
    expect(toJSON()).toBeNull();
  });

  it("returns null for idle status with selection", () => {
    const { toJSON } = renderBanner(
      <HoldStatusBanner holdStatus="idle" secondsLeft={0} hasSelection={true} />
    );
    expect(toJSON()).toBeNull();
  });

  it("shows loading text for pending status", () => {
    renderBanner(<HoldStatusBanner holdStatus="pending" secondsLeft={0} hasSelection={true} />);
    expect(screen.getByText("Checking availability…")).toBeTruthy();
  });

  it("shows countdown for held status", () => {
    renderBanner(<HoldStatusBanner holdStatus="held" secondsLeft={185} hasSelection={true} />);
    expect(screen.getByText(/Table held - expires in 3:05/)).toBeTruthy();
  });

  it("shows generic unavailable message when no holdMessage is provided", () => {
    renderBanner(<HoldStatusBanner holdStatus="unavailable" secondsLeft={0} hasSelection={true} />);
    expect(screen.getByText(/Table not available/)).toBeTruthy();
  });

  it("shows the backend holdMessage when provided", () => {
    renderBanner(
      <HoldStatusBanner
        holdStatus="unavailable"
        secondsLeft={0}
        hasSelection={true}
        holdMessage="Cannot hold a table for a past time."
      />
    );
    expect(screen.getByText(/Cannot hold a table for a past time/)).toBeTruthy();
    expect(screen.queryByText(/Table not available/)).toBeNull();
  });

  it("shows expired message with refresh button", () => {
    const onRefresh = jest.fn();
    renderBanner(
      <HoldStatusBanner
        holdStatus="expired"
        secondsLeft={0}
        hasSelection={true}
        onRefresh={onRefresh}
      />
    );
    expect(screen.getByText(/table hold expired/i)).toBeTruthy();
    fireEvent.press(screen.getByText("Refresh page"));
    expect(onRefresh).toHaveBeenCalledTimes(1);
  });

  it("shows expired message without refresh button when onRefresh not provided", () => {
    renderBanner(<HoldStatusBanner holdStatus="expired" secondsLeft={0} hasSelection={true} />);
    expect(screen.getByText(/table hold expired/i)).toBeTruthy();
    expect(screen.queryByText("Refresh page")).toBeNull();
  });

  it("shows localized Spanish status copy", () => {
    renderBanner(
      <HoldStatusBanner holdStatus="held" secondsLeft={125} hasSelection={true} />,
      "es-CO"
    );
    expect(screen.getByText(/Mesa retenida - vence en 2:05/)).toBeTruthy();
  });
});
