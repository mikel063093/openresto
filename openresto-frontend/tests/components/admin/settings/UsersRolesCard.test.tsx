/**
 * @jest-environment jsdom
 */
import React from "react";
import { fireEvent, screen, waitFor } from "@testing-library/react-native";
import { UsersRolesCard } from "@/components/admin/settings/UsersRolesCard";
import { renderWithProviders } from "@/tests/helpers/renderWithProviders";
import * as adminApi from "@/api/admin";

jest.mock("@/api/admin", () => ({
  createAdminUser: jest.fn(),
  deactivateAdminUser: jest.fn(),
  getAdminUsers: jest.fn(),
  updateAdminUser: jest.fn(),
}));

jest.mock("@/components/common/AnimatedAccordion", () => ({
  AnimatedAccordion: ({ expanded, children }: { expanded: boolean; children: React.ReactNode }) =>
    expanded ? children : null,
}));

describe("UsersRolesCard", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (adminApi.getAdminUsers as jest.Mock).mockResolvedValue([]);
    (adminApi.createAdminUser as jest.Mock).mockResolvedValue({
      id: 2,
      email: "editor@example.com",
      role: "BookingEditor",
      isActive: true,
    });
  });

  it("surfaces the backend validation message when createAdminUser rejects", async () => {
    (adminApi.createAdminUser as jest.Mock).mockRejectedValueOnce(
      new Error("Invalid role. Allowed values: SuperAdmin, BookingViewer, BookingEditor.")
    );

    renderWithProviders(<UsersRolesCard borderColor="#ccc" mutedColor="#666" cardBg="#fff" />);

    fireEvent.press(screen.getByLabelText("Toggle users and roles"));
    fireEvent.changeText(screen.getByPlaceholderText("email@example.com"), "editor@example.com");
    fireEvent.changeText(
      screen.getByPlaceholderText("Temporary password (6+ characters)"),
      "password"
    );
    fireEvent.press(screen.getByText("Add user"));

    await waitFor(() =>
      expect(
        screen.getByText("Invalid role. Allowed values: SuperAdmin, BookingViewer, BookingEditor.")
      ).toBeTruthy()
    );
  });
});
