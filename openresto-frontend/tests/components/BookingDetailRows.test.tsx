import React from "react";
import { render, screen } from "@testing-library/react-native";
import BookingDetailRows from "@/components/booking/BookingDetailRows";
import { BookingDto } from "@/api/bookings";
import { I18nProvider } from "@/context/I18nContext";

jest.mock("@expo/vector-icons", () => ({
  Ionicons: () => null,
}));

describe("BookingDetailRows", () => {
  const mockBooking: BookingDto = {
    id: 1,
    tableId: 101,
    sectionId: 10,
    restaurantId: 1,
    isHeld: false,
    customerEmail: "test@test.com",
    date: "2026-10-10T19:00:00Z",
    seats: 2,
    bookingRef: "REF",
    tableName: "Table 5",
    sectionName: "Garden",
    tableSeats: 4,
    specialRequests: "Birthday",
  };

  const mockRestaurant = {
    name: "Resto 1",
    address: "123 Main St",
  };

  function renderWithI18n(ui: React.ReactElement) {
    return render(<I18nProvider>{ui}</I18nProvider>);
  }

  it("renders all rows when data is complete", () => {
    renderWithI18n(
      <BookingDetailRows
        booking={mockBooking}
        restaurant={mockRestaurant as any}
        mutedColor="gray"
        borderColor="black"
      />
    );

    expect(screen.getByText("Restaurant")).toBeTruthy();
    expect(screen.getByText("Resto 1")).toBeTruthy();
    expect(screen.getByText("Address")).toBeTruthy();
    expect(screen.getByText("123 Main St")).toBeTruthy();
    expect(screen.getByText("Email")).toBeTruthy();
    expect(screen.getByText("test@test.com")).toBeTruthy();
    expect(screen.getByText("Birthday")).toBeTruthy();
    expect(screen.getByText(/Table for 4/)).toBeTruthy();
  });

  it("handles missing restaurant and optional fields", () => {
    const incompleteBooking: BookingDto = {
      ...mockBooking,
      tableName: undefined,
      specialRequests: undefined,
    };
    renderWithI18n(
      <BookingDetailRows
        booking={incompleteBooking}
        restaurant={null}
        mutedColor="gray"
        borderColor="black"
      />
    );

    expect(screen.queryByText("Restaurant")).toBeNull();
    expect(screen.queryByText("Address")).toBeNull();
    expect(screen.getByText("Requests")).toBeTruthy();
    expect(screen.getByText("None")).toBeTruthy();
    expect(screen.queryByText("Table")).toBeNull();
  });

  it("renders table name without section if section missing", () => {
    const noSectionBooking: BookingDto = {
      ...mockBooking,
      sectionName: undefined,
    };
    renderWithI18n(
      <BookingDetailRows
        booking={noSectionBooking}
        restaurant={null}
        mutedColor="gray"
        borderColor="black"
      />
    );
    expect(screen.getByText("Table 5")).toBeTruthy();
  });
});
