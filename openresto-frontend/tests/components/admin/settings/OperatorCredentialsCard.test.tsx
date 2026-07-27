import React from "react";
import { act, fireEvent, render, screen, waitFor } from "@testing-library/react-native";
import { OperatorCredentialsCard } from "@/components/admin/settings/OperatorCredentialsCard";
import * as adminApi from "@/api/admin";

jest.mock("@/api/admin", () => ({
  adminGetRestaurants: jest.fn(),
  getOperatorCredentials: jest.fn(),
  issueOperatorCredential: jest.fn(),
  revokeOperatorCredential: jest.fn(),
}));

jest.mock("@/hooks/use-persisted-state", () => ({
  usePersistedState: (_key: string, defaultValue: unknown) => {
    const { useState } = jest.requireActual("react") as typeof import("react");
    return useState(defaultValue);
  },
}));

jest.mock("@/components/common/ConfirmModal", () =>
  jest.requireActual("../../../../jest-mocks/ConfirmModal")
);

const baseProps = {
  borderColor: "#ddd",
  mutedColor: "#888",
  cardBg: "#fff",
};

describe("OperatorCredentialsCard", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (adminApi.adminGetRestaurants as jest.Mock).mockResolvedValue([{ id: 7, name: "Centro" }]);
    (adminApi.getOperatorCredentials as jest.Mock).mockResolvedValue([]);
    (adminApi.issueOperatorCredential as jest.Mock).mockResolvedValue({
      credentialId: 11,
      identifier: "operador@test.com",
      credentialKeyId: "abc123",
      issuedAtUtc: "2026-07-27T10:00:00Z",
      expiresAtUtc: "2026-07-27T18:00:00Z",
      revokedAtUtc: null,
      lastUsedAtUtc: null,
      notes: "Turno tarde",
      restaurants: [{ restaurantId: 7, restaurantName: "Centro" }],
      plaintextToken: "ormcp.abc123.secret",
    });
    (adminApi.revokeOperatorCredential as jest.Mock).mockResolvedValue(undefined);
  });

  it("shows preset options, uses the default preset, and posts the fixed request payload", async () => {
    render(<OperatorCredentialsCard {...baseProps} />);

    await waitFor(() => expect(screen.getByText("Credenciales MCP internas")).toBeTruthy());
    expect(screen.queryByPlaceholderText("8")).toBeNull();
    expect(screen.queryByDisplayValue("8")).toBeNull();
    expect(screen.getByText("8 horas")).toBeTruthy();
    expect(screen.getByText("1 día")).toBeTruthy();
    expect(screen.getByText("7 días")).toBeTruthy();
    expect(screen.getByText("1 mes")).toBeTruthy();
    expect(screen.getByText("3 meses")).toBeTruthy();
    expect(screen.getByText("6 meses")).toBeTruthy();
    expect(screen.getByText("1 año")).toBeTruthy();
    expect(screen.getByText("2 años")).toBeTruthy();
    expect(screen.getByText("No expira")).toBeTruthy();

    fireEvent.changeText(screen.getByPlaceholderText("operador@interno"), "operador@test.com");
    fireEvent.changeText(screen.getByPlaceholderText("Notas opcionales"), "Turno tarde");
    fireEvent.press(screen.getByText("Centro"));
    fireEvent.press(screen.getByText("1 día"));

    await act(async () => {
      fireEvent.press(screen.getByText("Emitir credencial"));
    });

    expect(adminApi.issueOperatorCredential).toHaveBeenCalledWith({
      identifier: "operador@test.com",
      restaurantIds: [7],
      expirationPreset: "one_day",
      notes: "Turno tarde",
    });

    await waitFor(() => expect(screen.getByText(/Solo se muestra una vez\./)).toBeTruthy());
    expect(screen.getByText(/ormcp\.abc123\.secret/)).toBeTruthy();
    expect(screen.getByText(/operador@test\.com/)).toBeTruthy();
  });

  it("clears the plaintext token after acknowledgement", async () => {
    render(<OperatorCredentialsCard {...baseProps} />);

    await waitFor(() => expect(screen.getByText("Credenciales MCP internas")).toBeTruthy());
    fireEvent.changeText(screen.getByPlaceholderText("operador@interno"), "operador@test.com");
    fireEvent.press(screen.getByText("Centro"));

    await act(async () => {
      fireEvent.press(screen.getByText("Emitir credencial"));
    });

    expect(await screen.findByText(/ormcp\.abc123\.secret/)).toBeTruthy();
    fireEvent.press(screen.getByText("Entendido"));

    await waitFor(() => expect(screen.queryByText(/ormcp\.abc123\.secret/)).toBeNull());
  });

  it("renders no-expiry credentials with clear label and active status", async () => {
    (adminApi.getOperatorCredentials as jest.Mock).mockResolvedValue([
      {
        credentialId: 11,
        identifier: "operador@test.com",
        credentialKeyId: "abc123",
        issuedAtUtc: "2026-07-27T10:00:00Z",
        expiresAtUtc: null,
        revokedAtUtc: null,
        lastUsedAtUtc: null,
        notes: null,
        restaurants: [{ restaurantId: 7, restaurantName: "Centro" }],
      },
    ]);

    render(<OperatorCredentialsCard {...baseProps} />);

    await waitFor(() => expect(screen.getByText("operador@test.com")).toBeTruthy());
    expect(screen.getByText(/Expira: No expira/)).toBeTruthy();
    expect(screen.getByText("Activa")).toBeTruthy();
  });

  it("shows the no-expiry warning and posts the never preset", async () => {
    render(<OperatorCredentialsCard {...baseProps} />);

    await waitFor(() => expect(screen.getByText("Credenciales MCP internas")).toBeTruthy());
    fireEvent.changeText(screen.getByPlaceholderText("operador@interno"), "operador@test.com");
    fireEvent.press(screen.getByText("Centro"));
    fireEvent.press(screen.getByText("No expira"));

    expect(screen.getByText(/alto riesgo/i)).toBeTruthy();
    expect(screen.getByText(/revoca de inmediato/i)).toBeTruthy();

    await act(async () => {
      fireEvent.press(screen.getByText("Emitir credencial"));
    });

    expect(adminApi.issueOperatorCredential).toHaveBeenCalledWith({
      identifier: "operador@test.com",
      restaurantIds: [7],
      expirationPreset: "never",
      notes: undefined,
    });
  });

  it("confirms and revokes a listed credential", async () => {
    (adminApi.getOperatorCredentials as jest.Mock).mockResolvedValue([
      {
        credentialId: 11,
        identifier: "operador@test.com",
        credentialKeyId: "abc123",
        issuedAtUtc: "2026-07-27T10:00:00Z",
        expiresAtUtc: "2026-07-27T18:00:00Z",
        revokedAtUtc: null,
        lastUsedAtUtc: null,
        notes: "Turno tarde",
        restaurants: [{ restaurantId: 7, restaurantName: "Centro" }],
      },
    ]);

    render(<OperatorCredentialsCard {...baseProps} />);

    await waitFor(() => expect(screen.getByText("operador@test.com")).toBeTruthy());
    fireEvent.press(screen.getByLabelText("Revocar credencial abc123"));
    await act(async () => {
      const revokeButtons = screen.getAllByText("Revocar");
      fireEvent.press(revokeButtons[revokeButtons.length - 1]);
    });

    await waitFor(() => expect(adminApi.revokeOperatorCredential).toHaveBeenCalledWith(11));
  });
});
