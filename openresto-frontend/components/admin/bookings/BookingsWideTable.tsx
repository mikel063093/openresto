import { Pressable, View, type ViewStyle } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { ThemedText } from "@/components/themed-text";
import { BookingDetailDto } from "@/api/admin";
import { theme } from "@/theme/theme";
import { fmtDateTime, initials } from "@/utils/formatters";
import { isPast, StatusBadge } from "@/components/admin/bookings/StatusBadge";
import { styles } from "@/components/admin/bookings/bookings.styles";
import { focusedRowHighlight, rowA11yProps } from "@/components/admin/bookings/bookingRowProps";
import type { SortKey, SortState } from "@/components/admin/bookings/sorting";
import { useI18n } from "@/context/I18nContext";

export interface BookingsWideTableProps {
  bookings: BookingDetailDto[];
  focusedRowId: number | null;
  onOpenBooking: (id: number) => void;
  onCancelBooking: (booking: BookingDetailDto) => void;
  /** Active sort (drives header indicator). */
  sort: SortState;
  onSortChange: (key: SortKey) => void;
  /** Theme values passed from the orchestrating screen (presentational). */
  borderColor: string;
  cardBg: string;
  mutedColor: string;
  isDark: boolean;
  primaryColor: string;
}

/**
 * Wide (desktop/tablet) bookings list — a bordered table with one row per
 * booking. Extracted from the bookings screen for decomposition; presentational,
 * owns no state (the screen drives focus + open/cancel callbacks + sort).
 *
 * TIME / GUEST / PARTY / TABLE headers are pressable and drive sort state;
 * STATUS reflects booking state and isn't a sort axis.
 */
export function BookingsWideTable({
  bookings,
  focusedRowId,
  onOpenBooking,
  onCancelBooking,
  sort,
  onSortChange,
  borderColor,
  cardBg,
  mutedColor,
  isDark,
  primaryColor,
}: BookingsWideTableProps) {
  const { locale, t } = useI18n();
  const headerBg = isDark ? "#28292b" : "#f8f8f9";
  // Subtle alternating row tint for scannability. Translucent so the focused-
  // row highlight (`primaryColor` tint) still reads on top of it.
  const zebraBg = isDark ? "rgba(255,255,255,0.022)" : "rgba(0,0,0,0.018)";

  const renderSortHeader = (key: SortKey, label: string, columnStyle: ViewStyle) => {
    const isActive = sort.key === key;
    const dirLabel = isActive
      ? sort.dir === "asc"
        ? t("admin.sortDirection.ascending")
        : t("admin.sortDirection.descending")
      : t("admin.sortDirection.notSorted");
    const icon = !isActive
      ? "swap-vertical-outline"
      : sort.dir === "asc"
        ? "chevron-up-outline"
        : "chevron-down-outline";
    return (
      <Pressable
        testID={`sort-header-${key}`}
        accessibilityRole="button"
        accessibilityLabel={t("admin.sortBy", { label, direction: dirLabel })}
        style={[styles.thSortBtn, columnStyle, { alignItems: "center" }]}
        onPress={() => onSortChange(key)}
      >
        <ThemedText
          style={[
            styles.thCell,
            { color: isActive ? primaryColor : mutedColor },
            isActive && styles.thCellActive,
          ]}
        >
          {label}
        </ThemedText>
        <Ionicons name={icon} size={11} color={isActive ? primaryColor : mutedColor} />
      </Pressable>
    );
  };

  return (
    <View style={[styles.tableCard, { backgroundColor: cardBg, borderColor }]}>
      <View
        style={[
          styles.tableHeader,
          { backgroundColor: headerBg, borderBottomWidth: 1, borderBottomColor: borderColor },
        ]}
      >
        {renderSortHeader("date", t("booking.time"), styles.colTime)}
        {renderSortHeader("guest", t("booking.guestLabel"), styles.colGuest)}
        {renderSortHeader("seats", t("booking.party"), styles.colParty)}
        {renderSortHeader("table", t("booking.table"), styles.colTable)}
        {renderSortHeader("status", t("booking.status"), styles.colStatus)}
        <View style={styles.colAction} />
      </View>

      {bookings.map((b, i) => (
        <Pressable
          key={b.id}
          {...rowA11yProps(b.id, focusedRowId)}
          style={[
            styles.tableRow,
            i > 0 && { borderTopWidth: 1, borderTopColor: borderColor },
            { cursor: "pointer" } as const,
            // Even rows get the faint zebra tint unless they're focused.
            i % 2 === 1 && !focusedRowHighlight(b.id, focusedRowId, primaryColor)
              ? { backgroundColor: zebraBg }
              : null,
            focusedRowHighlight(b.id, focusedRowId, primaryColor),
          ]}
          onPress={() => onOpenBooking(b.id)}
        >
          {/* Avatar + time */}
          <View style={[styles.colTime, { flexDirection: "row", alignItems: "center", gap: 8 }]}>
            <View
              style={{
                width: 32,
                height: 32,
                borderRadius: 16,
                backgroundColor: `${primaryColor}18`,
                alignItems: "center",
                justifyContent: "center",
              }}
            >
              <ThemedText style={{ fontSize: 11, fontWeight: "700", color: primaryColor }}>
                {initials(b.customerName ?? b.customerEmail)}
              </ThemedText>
            </View>
            <View>
              <ThemedText style={styles.tdTime}>
                {fmtDateTime(new Date(b.date), locale, {
                  hour: "2-digit",
                  minute: "2-digit",
                })}
              </ThemedText>
              <ThemedText style={[styles.tdDate, { color: mutedColor }]}>
                {fmtDateTime(new Date(b.date), locale, {
                  month: "short",
                  day: "numeric",
                })}
              </ThemedText>
            </View>
          </View>

          <View style={styles.colGuest}>
            <ThemedText style={styles.tdGuest} numberOfLines={1}>
              {b.customerName ?? b.customerEmail}
            </ThemedText>
            {b.customerName ? (
              <ThemedText style={[styles.tdNotes, { color: mutedColor }]} numberOfLines={1}>
                {b.customerEmail}
              </ThemedText>
            ) : null}
            {b.bookingRef && (
              <ThemedText style={[styles.tdNotes, { color: mutedColor }]} numberOfLines={1}>
                {b.bookingRef}
              </ThemedText>
            )}
          </View>

          <View style={styles.colParty}>
            <View style={styles.partyPill}>
              <Ionicons name="people-outline" size={12} color={mutedColor} />
              <ThemedText style={[styles.tdParty, { color: mutedColor }]}>{b.seats}</ThemedText>
            </View>
          </View>

          <ThemedText style={[styles.tdTableNum, styles.colTable, { color: mutedColor }]}>
            {b.tableName}
          </ThemedText>

          <View style={styles.colStatus}>
            {b.isCancelled ? (
              <View
                style={[
                  styles.badge,
                  { backgroundColor: theme.status.cancelled.bg[isDark ? "dark" : "light"] },
                ]}
              >
                <ThemedText style={[styles.badgeText, { color: theme.status.cancelled.text }]}>
                  {t("admin.cancelled")}
                </ThemedText>
              </View>
            ) : (
              <StatusBadge date={b.date} isDark={isDark} />
            )}
          </View>

          <View style={styles.colAction}>
            {!b.isCancelled && !isPast(b.date) && (
              <Pressable
                accessibilityLabel={t("booking.cancelBooking")}
                style={[
                  styles.rowActionBtn,
                  { backgroundColor: theme.status.cancelled.bg[isDark ? "dark" : "light"] },
                ]}
                onPress={(e) => {
                  // stopPropagation is present on web mouse events but not RN's
                  // GestureResponderEvent — guard both the event and the method.
                  (e as { stopPropagation?: () => void } | undefined)?.stopPropagation?.();
                  onCancelBooking(b);
                }}
              >
                <Ionicons name="close-outline" size={14} color={theme.status.cancelled.text} />
              </Pressable>
            )}
          </View>
        </Pressable>
      ))}
    </View>
  );
}
