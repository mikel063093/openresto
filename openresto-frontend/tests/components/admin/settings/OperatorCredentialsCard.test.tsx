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

  it("shows one-time token reveal after issuing and then lists metadata without leaking old token state", async () => {
    render(<OperatorCredentialsCard {...baseProps} />);

    await waitFor(() => expect(screen.getByText("Credenciales MCP internas")).toBeTruthy());
    fireEvent.changeText(screen.getByPlaceholderText("operador@interno"), "operador@test.com");
    fireEvent.changeText(screen.getByPlaceholderText("8"), "6");
    fireEvent.changeText(screen.getByPlaceholderText("Notas opcionales"), "Turno tarde");
    fireEvent.press(screen.getByText("Centro"));

    await act(async () => {
      fireEvent.press(screen.getByText("Emitir credencial"));
    });

    expect(adminApi.issueOperatorCredential).toHaveBeenCalledWith({
      identifier: "operador@test.com",
      restaurantIds: [7],
      ttlHours: 6,
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
