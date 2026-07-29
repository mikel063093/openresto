import React from "react";
import { act, fireEvent, render, screen, waitFor } from "@testing-library/react-native";
import { HandoffWhatsAppCard } from "@/components/admin/settings/HandoffWhatsAppCard";
import * as adminApi from "@/api/admin";

jest.mock("@/api/admin", () => ({
  adminGetRestaurants: jest.fn(),
  getRestaurantWhatsAppSettings: jest.fn(),
  updateRestaurantWhatsAppSettings: jest.fn(),
}));

const baseProps = {
  borderColor: "#ddd",
  mutedColor: "#888",
  cardBg: "#fff",
};

describe("HandoffWhatsAppCard", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (adminApi.adminGetRestaurants as jest.Mock).mockResolvedValue([{ id: 7, name: "Centro" }]);
    (adminApi.getRestaurantWhatsAppSettings as jest.Mock).mockResolvedValue({
      restaurantId: 7,
      isWhatsAppTestEnabled: true,
      handoffWhatsAppE164: "+573001112233",
    });
    (adminApi.updateRestaurantWhatsAppSettings as jest.Mock).mockResolvedValue({
      restaurantId: 7,
      isWhatsAppTestEnabled: true,
      handoffWhatsAppE164: "+573009998877",
    });
  });

  it("requires a handoff number when WhatsApp test is enabled", async () => {
    render(<HandoffWhatsAppCard {...baseProps} />);

    await waitFor(() => expect(screen.getByText("Número de handoff")).toBeTruthy());
    await waitFor(() => expect(screen.getByDisplayValue("+573001112233")).toBeTruthy());
    fireEvent.changeText(screen.getByPlaceholderText("+57 300 123 4567"), "");

    await act(async () => {
      fireEvent.press(screen.getByText("Guardar handoff"));
    });

    expect(screen.getByText(/número de handoff es obligatorio/i)).toBeTruthy();
    expect(adminApi.updateRestaurantWhatsAppSettings).not.toHaveBeenCalled();
  });

  it("saves the handoff number", async () => {
    render(<HandoffWhatsAppCard {...baseProps} />);

    await waitFor(() => expect(screen.getByText("Número de handoff")).toBeTruthy());
    await waitFor(() => expect(screen.getByDisplayValue("+573001112233")).toBeTruthy());
    fireEvent.changeText(screen.getByPlaceholderText("+57 300 123 4567"), "+57 300 999 8877");

    await act(async () => {
      fireEvent.press(screen.getByText("Guardar handoff"));
    });

    expect(adminApi.updateRestaurantWhatsAppSettings).toHaveBeenCalledWith(7, {
      isWhatsAppTestEnabled: true,
      handoffWhatsAppE164: "+57 300 999 8877",
    });
    await waitFor(() => expect(screen.getByText("Número de handoff actualizado.")).toBeTruthy());
  });
});
