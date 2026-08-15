import React from "react";
import { act, fireEvent, render, screen, waitFor } from "@testing-library/react-native";
import { WhatsAppTestSettingsCard } from "@/components/admin/settings/WhatsAppTestSettingsCard";
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

describe("WhatsAppTestSettingsCard", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (adminApi.adminGetRestaurants as jest.Mock).mockResolvedValue([{ id: 7, name: "Centro" }]);
    (adminApi.getRestaurantWhatsAppSettings as jest.Mock).mockResolvedValue({
      restaurantId: 7,
      isWhatsAppTestEnabled: false,
      handoffWhatsAppE164: null,
    });
    (adminApi.updateRestaurantWhatsAppSettings as jest.Mock).mockResolvedValue({
      restaurantId: 7,
      isWhatsAppTestEnabled: false,
      handoffWhatsAppE164: null,
    });
  });

  it("blocks enabling WhatsApp when handoff is missing", async () => {
    render(<WhatsAppTestSettingsCard {...baseProps} />);

    await waitFor(() => expect(screen.getByText("WhatsApp de prueba")).toBeTruthy());
    await waitFor(() => expect(adminApi.getRestaurantWhatsAppSettings).toHaveBeenCalledWith(7));

    fireEvent.press(screen.getByRole("switch"));
    await act(async () => {
      fireEvent.press(screen.getByText("Guardar visibilidad"));
    });

    expect(screen.getByText(/Configura primero el número de handoff/i)).toBeTruthy();
    expect(adminApi.updateRestaurantWhatsAppSettings).not.toHaveBeenCalled();
  });

  it("saves the visibility toggle when handoff already exists", async () => {
    (adminApi.getRestaurantWhatsAppSettings as jest.Mock).mockResolvedValueOnce({
      restaurantId: 7,
      isWhatsAppTestEnabled: false,
      handoffWhatsAppE164: "+573001112233",
    });
    (adminApi.updateRestaurantWhatsAppSettings as jest.Mock).mockResolvedValueOnce({
      restaurantId: 7,
      isWhatsAppTestEnabled: true,
      handoffWhatsAppE164: "+573001112233",
    });

    render(<WhatsAppTestSettingsCard {...baseProps} />);

    await waitFor(() => expect(screen.getByText("WhatsApp de prueba")).toBeTruthy());
    await waitFor(() => expect(adminApi.getRestaurantWhatsAppSettings).toHaveBeenCalledWith(7));

    fireEvent.press(screen.getByRole("switch"));
    await act(async () => {
      fireEvent.press(screen.getByText("Guardar visibilidad"));
    });

    expect(adminApi.updateRestaurantWhatsAppSettings).toHaveBeenCalledWith(7, {
      isWhatsAppTestEnabled: true,
      handoffWhatsAppE164: "+573001112233",
    });
    await waitFor(() =>
      expect(screen.getByText("Visibilidad de WhatsApp actualizada.")).toBeTruthy()
    );
  });
});
