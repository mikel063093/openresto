import { Pressable, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { ThemedText } from "@/components/themed-text";
import { styles } from "@/components/admin/bookings/bookings.styles";
import type { SortKey, SortState } from "@/components/admin/bookings/sorting";
import { useI18n } from "@/context/I18nContext";

export interface BookingsSortControlProps {
  sort: SortState;
  onSortChange: (key: SortKey) => void;
  /** Theme values passed from the orchestrating screen (presentational). */
  borderColor: string;
  cardBg: string;
  mutedColor: string;
  primaryColor: string;
}

const COLUMNS: { key: SortKey; labelKey: string }[] = [
  { key: "date", labelKey: "booking.time" },
  { key: "guest", labelKey: "booking.guestLabel" },
  { key: "seats", labelKey: "booking.party" },
  { key: "table", labelKey: "booking.table" },
  { key: "status", labelKey: "booking.status" },
];

/**
 * Sort affordance for the mobile card list — column headers don't apply to a
 * card layout, so this offers the same sort axes as the wide table via a row of
 * chips, each with a direction toggle (tap the active chip to flip asc/desc).
 * Presentational; the screen owns the sort state.
 */
export function BookingsSortControl({
  sort,
  onSortChange,
  borderColor,
  cardBg,
  mutedColor,
  primaryColor,
}: BookingsSortControlProps) {
  const { t } = useI18n();
  return (
    <View style={[styles.sortControl, { borderColor, backgroundColor: cardBg }]}>
      <ThemedText style={[styles.sortControlLabel, { color: mutedColor }]}>
        {t("admin.sort")}
      </ThemedText>
      <View style={styles.sortControlChips}>
        {COLUMNS.map(({ key, labelKey }) => {
          const label = t(labelKey as never);
          const isActive = sort.key === key;
          const dirLabel = isActive
            ? sort.dir === "asc"
              ? t("admin.sortDirection.ascending")
              : t("admin.sortDirection.descending")
            : t("admin.sortDirection.notSorted");
          return (
            <Pressable
              key={key}
              testID={`sort-chip-${key}`}
              accessibilityRole="button"
              accessibilityLabel={t("admin.sortBy", { label, direction: dirLabel })}
              style={[
                styles.sortChip,
                { borderColor: isActive ? primaryColor : borderColor },
                isActive && { backgroundColor: primaryColor },
              ]}
              onPress={() => onSortChange(key)}
            >
              <ThemedText
                style={[
                  styles.sortChipText,
                  { color: isActive ? "#fff" : mutedColor },
                  isActive && styles.sortChipTextActive,
                ]}
              >
                {label}
              </ThemedText>
              <Ionicons
                name={
                  !isActive
                    ? "swap-vertical-outline"
                    : sort.dir === "asc"
                      ? "chevron-up-outline"
                      : "chevron-down-outline"
                }
                size={11}
                color={isActive ? "#fff" : mutedColor}
              />
            </Pressable>
          );
        })}
      </View>
    </View>
  );
}
