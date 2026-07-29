import React from "react";
import { act, fireEvent, render, screen, waitFor } from "@testing-library/react-native";
import { OccasionCatalogCard } from "@/components/admin/settings/OccasionCatalogCard";
import * as adminApi from "@/api/admin";

jest.mock("@/api/admin", () => ({
  adminGetRestaurants: jest.fn(),
  getOccasionCatalog: jest.fn(),
  createOccasionCatalogItem: jest.fn(),
  updateOccasionCatalogItem: jest.fn(),
  deleteOccasionCatalogItem: jest.fn(),
}));

const baseProps = {
  borderColor: "#ddd",
  mutedColor: "#888",
  cardBg: "#fff",
};

describe("OccasionCatalogCard", () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (adminApi.adminGetRestaurants as jest.Mock).mockResolvedValue([{ id: 7, name: "Centro" }]);
    (adminApi.getOccasionCatalog as jest.Mock).mockResolvedValue([
      {
        id: 1,
        name: "Cumpleaños",
        description: "Decoración sencilla",
        estimatedPriceCop: 80000,
        isActive: true,
        sortOrder: 0,
      },
    ]);
    (adminApi.createOccasionCatalogItem as jest.Mock).mockResolvedValue({
      id: 2,
      name: "Aniversario",
      description: "Flores",
      estimatedPriceCop: 120000,
      isActive: true,
      sortOrder: 1,
    });
    (adminApi.updateOccasionCatalogItem as jest.Mock).mockResolvedValue({
      id: 1,
      name: "Cumpleaños premium",
      description: "Decoración especial",
      estimatedPriceCop: 90000,
      isActive: false,
      sortOrder: 0,
    });
    (adminApi.deleteOccasionCatalogItem as jest.Mock).mockResolvedValue(undefined);
  });

  it("creates a new catalog item", async () => {
    render(<OccasionCatalogCard {...baseProps} />);

    await waitFor(() => expect(screen.getByText("Catálogo de ocasiones")).toBeTruthy());

    fireEvent.changeText(screen.getByPlaceholderText("Cumpleaños"), "Aniversario");
    fireEvent.changeText(screen.getByPlaceholderText("Decoración y mensaje especial"), "Flores");
    fireEvent.changeText(screen.getByPlaceholderText("85000"), "120000");

    await act(async () => {
      fireEvent.press(screen.getByText("Agregar ítem"));
    });

    expect(adminApi.createOccasionCatalogItem).toHaveBeenCalledWith(7, {
      name: "Aniversario",
      description: "Flores",
      estimatedPriceCop: 120000,
      isActive: true,
    });
    await waitFor(() => expect(screen.getByText("Ítem agregado al catálogo.")).toBeTruthy());
  });

  it("updates and deletes an existing catalog item", async () => {
    render(<OccasionCatalogCard {...baseProps} />);

    await waitFor(() => expect(screen.getByText("Cumpleaños")).toBeTruthy());

    fireEvent.press(screen.getByText("Editar"));
    fireEvent.changeText(screen.getByDisplayValue("Cumpleaños"), "Cumpleaños premium");
    fireEvent.changeText(screen.getByDisplayValue("Decoración sencilla"), "Decoración especial");
    fireEvent.changeText(screen.getByDisplayValue("80000"), "90000");
    const switches = screen.getAllByRole("switch");
    fireEvent.press(switches[switches.length - 1]);

    await act(async () => {
      fireEvent.press(screen.getByText("Guardar"));
    });

    expect(adminApi.updateOccasionCatalogItem).toHaveBeenCalledWith(7, 1, {
      name: "Cumpleaños premium",
      description: "Decoración especial",
      estimatedPriceCop: 90000,
      isActive: false,
    });

    await act(async () => {
      fireEvent.press(screen.getByText("Eliminar"));
    });

    expect(adminApi.deleteOccasionCatalogItem).toHaveBeenCalledWith(7, 1);
  });
});
