/**
 * @jest-environment jsdom
 */
import React from "react";
import { screen, waitFor } from "@testing-library/react-native";
import AdminSettingsScreen from "@/app/admin/settings";
import { renderWithProviders } from "@/tests/helpers/renderWithProviders";
import { checkSession } from "@/api/auth";

jest.mock("expo-router", () => ({
  Stack: { Screen: () => null },
}));

jest.mock("@/components/admin/settings/BrandSettingsCard", () => ({
  BrandSettingsCard: () => null,
}));
jest.mock("@/components/admin/settings/EmailSettingsCard", () => ({
  EmailSettingsCard: () => null,
}));
jest.mock("@/components/admin/settings/SecurityCard", () => ({ SecurityCard: () => null }));
jest.mock("@/components/admin/settings/HighlightsCard", () => ({ HighlightsCard: () => null }));
jest.mock("@/components/admin/settings/FooterSettingsCard", () => ({
  FooterSettingsCard: () => null,
}));
jest.mock("@/components/admin/settings/PushNotificationsCard", () => ({
  PushNotificationsCard: () => null,
}));
jest.mock("@/components/admin/settings/UsersRolesCard", () => ({
  UsersRolesCard: () => null,
}));
jest.mock("@/components/admin/settings/OperatorCredentialsCard", () => ({
  OperatorCredentialsCard: () => null,
}));
jest.mock("@/api/auth", () => ({
  checkSession: jest.fn(),
}));

describe("AdminSettingsScreen", () => {
  beforeEach(() => {
    (checkSession as jest.Mock).mockResolvedValue({
      email: "admin@test.com",
      role: "BookingEditor",
    });
  });

  it("renders global settings and security sections", async () => {
    renderWithProviders(<AdminSettingsScreen />);
    await waitFor(() => {
      expect(screen.getByText("Settings")).toBeTruthy();
      expect(screen.getByText("GLOBAL SETTINGS")).toBeTruthy();
      expect(screen.getByText("ACCOUNT SECURITY")).toBeTruthy();
    });
  });

  it("shows updated subtitle", async () => {
    renderWithProviders(<AdminSettingsScreen />);
    await waitFor(() =>
      expect(screen.getByText("Manage brand, email, and security.")).toBeTruthy()
    );
  });

  it("shows MCP access section only for SuperAdmin", async () => {
    (checkSession as jest.Mock).mockResolvedValueOnce({
      email: "root@test.com",
      role: "SuperAdmin",
    });
    renderWithProviders(<AdminSettingsScreen />);
    await waitFor(() => expect(screen.getByText("ACCESO MCP INTERNO")).toBeTruthy());
  });

  it("hides MCP access section for non-SuperAdmin", async () => {
    renderWithProviders(<AdminSettingsScreen />);
    await waitFor(() => expect(screen.getByText("Settings")).toBeTruthy());
    expect(screen.queryByText("ACCESO MCP INTERNO")).toBeNull();
  });
});
