import { StyleSheet, View } from "react-native";
import { ThemedText } from "@/components/themed-text";
import { Ionicons } from "@expo/vector-icons";
import { BookingDto } from "@/api/bookings";
import { RestaurantDto } from "@/api/restaurants";
import { useI18n } from "@/context/I18nContext";
import { type Locale } from "@/i18n/locale";
import { fmtDateTime } from "@/utils/formatters";

interface BookingDetailRowsProps {
  booking: BookingDto;
  restaurant: RestaurantDto | null;
  mutedColor: string;
  borderColor: string;
}

type Translate = ReturnType<typeof useI18n>["t"];

type RowData = {
  icon: React.ComponentProps<typeof Ionicons>["name"];
  label: string;
  value: string;
};

function buildRows(
  booking: BookingDto,
  restaurant: RestaurantDto | null,
  locale: Locale,
  t: Translate
): RowData[] {
  const rows: RowData[] = [];

  if (restaurant) {
    rows.push({ icon: "restaurant-outline", label: t("booking.restaurant"), value: restaurant.name });
    if (restaurant.address) {
      rows.push({ icon: "location-outline", label: t("booking.address"), value: restaurant.address });
    }
  }

  if (booking.customerName) {
    rows.push({ icon: "person-outline", label: t("booking.name"), value: booking.customerName });
  }
  rows.push({ icon: "mail-outline", label: t("booking.email"), value: booking.customerEmail });

  rows.push({
    icon: "calendar-outline",
    label: t("booking.date"),
    value: fmtDateTime(new Date(booking.date), locale, {
      weekday: "long",
      year: "numeric",
      month: "long",
      day: "numeric",
    }),
  });

  rows.push({
    icon: "time-outline",
    label: t("booking.time"),
    value: fmtDateTime(new Date(booking.date), locale, {
      hour: "2-digit",
      minute: "2-digit",
    }),
  });

  rows.push({
    icon: "people-outline",
    label: t("booking.guests"),
    value: `${booking.seats}${booking.tableSeats ? ` (${t("booking.tableFor", { seats: booking.tableSeats })})` : ""}`,
  });

  if (booking.sectionName) {
    rows.push({ icon: "layers-outline", label: t("booking.section"), value: booking.sectionName });
  }

  if (booking.tableName) {
    rows.push({ icon: "grid-outline", label: t("booking.table"), value: booking.tableName });
  }

  rows.push({
    icon: "chatbubble-outline",
    label: t("booking.requests"),
    value: booking.specialRequests || t("common.none"),
  });

  return rows;
}

export default function BookingDetailRows({
  booking,
  restaurant,
  mutedColor,
  borderColor,
}: BookingDetailRowsProps) {
  const { locale, t } = useI18n();
  const rows = buildRows(booking, restaurant, locale, t);

  return (
    <>
      {rows.map(({ icon, label, value }, i) => (
        <View key={label}>
          {i > 0 && <View style={[styles.divider, { backgroundColor: borderColor }]} />}
          <View style={styles.row}>
            <Ionicons name={icon} size={15} color={mutedColor} />
            <View style={styles.content}>
              <ThemedText style={[styles.label, { color: mutedColor }]}>{label}</ThemedText>
              <ThemedText style={styles.value}>{value}</ThemedText>
            </View>
          </View>
        </View>
      ))}
    </>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
    paddingHorizontal: 16,
    paddingVertical: 13,
  },
  content: { flex: 1, gap: 2 },
  label: { fontSize: 11, fontWeight: "600", letterSpacing: 0.4, textTransform: "uppercase" },
  value: { fontSize: 15, fontWeight: "500" },
  divider: { height: 1 },
});
