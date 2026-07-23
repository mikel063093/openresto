import { ThemedText } from "@/components/themed-text";
import { ThemedView } from "@/components/themed-view";
import { Ionicons } from "@expo/vector-icons";
import { useEffect, useState } from "react";
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  useWindowDimensions,
  View,
  Platform,
} from "react-native";
import { useRouter, Stack } from "expo-router";
import { getAdminDashboardStats, AdminDashboardStats, BookingSummaryDto } from "@/api/admin";
import { BookingDetailPopup } from "@/components/admin/bookings/BookingDetailPopup";
import { StatusBadge } from "@/components/admin/bookings/StatusBadge";
import { useAppTheme } from "@/hooks/use-app-theme";
import { theme, ThemeColors } from "@/theme/theme";
import RestaurantActionModal from "@/components/admin/bookings/RestaurantActionModal";
import AlertModal from "@/components/common/AlertModal";
import { useI18n } from "@/context/I18nContext";
import { fmtDateTime } from "@/utils/formatters";

export default function AdminDashboardScreen() {
  const [stats, setStats] = useState<AdminDashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const router = useRouter();
  const { width } = useWindowDimensions();
  const { colors, primaryColor, isDark } = useAppTheme();
  const { locale, t } = useI18n();

  const [selectedBookingId, setSelectedBookingId] = useState<number | null>(null);
  const [actionModalVisible, setActionModalVisible] = useState(false);
  const [actionType, setActionType] = useState<"pause" | "extend">("pause");
  const [alertVisible, setAlertVisible] = useState(false);
  const [alertMessage, setAlertMessage] = useState("");

  const isWide = Platform.OS === "web" && width >= 1024;

  const loadStats = () => {
    getAdminDashboardStats().then((data: AdminDashboardStats | null) => {
      setStats(data);
      setLoading(false);
    });
  };

  useEffect(() => {
    loadStats();
  }, []);

  const metricCards = stats
    ? [
        {
          label: t("admin.metric.todayBookings"),
          value: stats.todayCount,
          sub: t("admin.metric.todayCovers"),
          icon: "calendar-outline" as const,
          accent: "#2563eb",
        },
        {
          label: t("admin.metric.activeHolds"),
          value: stats.activeHoldsCount,
          sub: t("admin.metric.heldTables"),
          icon: "book-outline" as const,
          accent: primaryColor,
        },
        {
          label: t("admin.metric.restaurantStatus"),
          value: stats.pausedCount > 0 ? t("admin.metric.paused") : t("admin.metric.active"),
          sub:
            stats.pausedCount > 0
              ? t("admin.metric.pausedVenues", { count: stats.pausedCount })
              : t("admin.metric.acceptingBookings"),
          icon:
            stats.pausedCount > 0
              ? ("pause-circle-outline" as const)
              : ("checkmark-circle-outline" as const),
          accent: stats.pausedCount > 0 ? theme.colors.error : theme.colors.success,
        },
        {
          label: t("admin.metric.totalCovers"),
          value: new Intl.NumberFormat(locale).format(stats.totalCovers),
          sub: t("admin.metric.totalGuestsServed"),
          icon: "people-outline" as const,
          accent: "#d97706",
        },
      ]
    : [];

  const QUICK_ACTIONS = [
    {
      title: t("admin.quickAction.newBooking"),
      icon: "person-add-outline" as const,
      onPress: () => router.push({ pathname: "/admin/bookings", params: { create: "1" } }),
      primary: true,
    },
    {
      title: t("admin.quickAction.viewAllBookings"),
      icon: "list-outline" as const,
      route: "/admin/bookings" as const,
    },
    {
      title: t("admin.quickAction.pauseBookings"),
      icon: "pause-circle-outline" as const,
      onPress: () => {
        setActionType("pause");
        setActionModalVisible(true);
      },
    },
    {
      title: t("admin.quickAction.extendBookings"),
      icon: "time-outline" as const,
      onPress: () => {
        setActionType("extend");
        setActionModalVisible(true);
      },
    },
    {
      title: t("admin.quickAction.manageSettings"),
      icon: "settings-outline" as const,
      route: "/admin/settings" as const,
    },
  ];

  return (
    <ThemedView style={styles.root}>
      {Platform.OS !== "web" && <Stack.Screen options={{ title: t("admin.dashboardTitle") }} />}
      <ScrollView contentContainerStyle={styles.outer} keyboardShouldPersistTaps="handled">
        <View style={styles.header}>
          <View>
            <ThemedText type="h1">{t("admin.dashboardTitle")}</ThemedText>
            <ThemedText style={[styles.pageSub, { color: colors.muted }]}>
              {t("admin.welcomeBack")}
            </ThemedText>
          </View>
        </View>

        {loading ? (
          <ActivityIndicator
            style={styles.spinner}
            size="large"
            color={primaryColor}
            testID="dashboard-spinner"
          />
        ) : (
          <>
            <View style={[styles.metricsGrid, isWide && styles.metricsGridWide]}>
              {metricCards.map((stat) => (
                <MetricCard key={stat.label} stat={stat} colors={colors} />
              ))}
            </View>

            <View style={[styles.mainRow, isWide && styles.mainRowWide]}>
              <View
                style={[
                  styles.chartCard,
                  { backgroundColor: colors.card, borderColor: colors.border },
                  isWide && styles.chartCardWide,
                ]}
              >
                <View style={styles.chartHeader}>
                  <ThemedText style={styles.cardTitle}>{t("admin.occupancyOverview")}</ThemedText>
                  <ThemedText style={[styles.chartSub, { color: colors.muted }]}>
                    {t("admin.lastSevenDays")}
                  </ThemedText>
                </View>
                <OccupancyChart
                  primaryColor={primaryColor}
                  colors={colors}
                  isDark={isDark}
                  locale={locale}
                  t={t}
                  data={stats?.occupancyData ?? []}
                  dates={stats?.occupancyDates ?? []}
                  counts={stats?.occupancyCounts ?? []}
                />
              </View>

              <View style={[styles.actionsCol, isWide && styles.actionsColWide]}>
                {QUICK_ACTIONS.map((action) => (
                  <Pressable
                    key={action.title}
                    onPress={() => (action.route ? router.push(action.route) : action.onPress?.())}
                    style={({ hovered }: any) => [
                      styles.actionCard,
                      { backgroundColor: colors.card, borderColor: colors.border },
                      action.primary && {
                        backgroundColor: primaryColor,
                        borderColor: primaryColor,
                      },
                      hovered && { opacity: 0.9, transform: [{ scale: 0.98 }] },
                    ]}
                  >
                    <Ionicons
                      name={action.icon}
                      size={24}
                      color={action.primary ? theme.colors.white : primaryColor}
                    />
                    <ThemedText
                      style={[styles.actionTitle, action.primary && { color: theme.colors.white }]}
                    >
                      {action.title}
                    </ThemedText>
                  </Pressable>
                ))}
              </View>
            </View>

            <View
              style={[
                styles.listCard,
                { backgroundColor: colors.card, borderColor: colors.border },
              ]}
            >
              <View style={styles.listHeader}>
                <ThemedText style={styles.cardTitle}>{t("admin.recentBookingsToday")}</ThemedText>
                <Pressable onPress={() => router.push("/admin/bookings")}>
                  <ThemedText style={[styles.viewAll, { color: primaryColor }]}>
                    {t("admin.viewAllArrow")}
                  </ThemedText>
                </Pressable>
              </View>
              {stats?.recentBookings.length === 0 ? (
                <View style={styles.emptyRecent}>
                  <ThemedText style={[styles.emptyText, { color: colors.muted }]}>
                    {t("admin.noUpcomingBookingsToday")}
                  </ThemedText>
                </View>
              ) : (
                stats?.recentBookings.map((b: BookingSummaryDto) => (
                  <BookingItem
                    key={b.bookingRef}
                    booking={b}
                    colors={colors}
                    isDark={isDark}
                    locale={locale}
                    t={t}
                    onPress={() => setSelectedBookingId(b.id)}
                  />
                ))
              )}
            </View>
          </>
        )}
      </ScrollView>

      <RestaurantActionModal
        visible={actionModalVisible}
        actionType={actionType}
        onClose={() => {
          setActionModalVisible(false);
          loadStats();
        }}
        onSuccess={(msg) => {
          setAlertMessage(msg);
          setAlertVisible(true);
        }}
      />

      <AlertModal
        visible={alertVisible}
        title={t("admin.success")}
        message={alertMessage}
        onClose={() => setAlertVisible(false)}
      />

      <BookingDetailPopup
        bookingId={selectedBookingId}
        onClose={() => setSelectedBookingId(null)}
        onMutated={loadStats}
      />
    </ThemedView>
  );
}

function MetricCard({
  stat,
  colors,
}: {
  stat: {
    label: string;
    value: string | number;
    sub: string;
    icon: keyof typeof Ionicons.glyphMap;
    accent: string;
  };
  colors: ThemeColors;
}) {
  return (
    <View style={[styles.metricCard, { backgroundColor: colors.card, borderColor: colors.border }]}>
      <View style={[styles.metricIconWrap, { backgroundColor: `${stat.accent}14` }]}>
        <Ionicons name={stat.icon} size={20} color={stat.accent} />
      </View>
      <ThemedText style={styles.metricValue}>{stat.value}</ThemedText>
      <ThemedText style={[styles.metricLabel, { color: colors.muted }]}>{stat.label}</ThemedText>
      <ThemedText style={[styles.metricSub, { color: colors.muted }]}>{stat.sub}</ThemedText>
    </View>
  );
}

function OccupancyChart({
  primaryColor,
  colors,
  isDark,
  locale,
  t,
  data,
  dates,
  counts,
}: {
  primaryColor: string;
  colors: ThemeColors;
  isDark: boolean;
  locale: "en" | "es-CO";
  t: (key: any, values?: Record<string, string | number>) => string;
  data: number[];
  dates?: string[];
  counts?: number[];
}) {
  const chartData = data?.length > 0 ? data : [0, 0, 0, 0, 0, 0, 0];
  const [labelMode, setLabelMode] = useState<"relative" | "calendar">("relative");

  const relativeLabels = ["T-6", "T-5", "T-4", "T-3", "T-2", "T-1", t("admin.todayShort")];
  const formatCalendar = (iso: string) =>
    fmtDateTime(new Date(iso), locale, { month: "short", day: "numeric" });

  const labelFor = (i: number) => {
    if (labelMode === "calendar" && dates && dates[i]) {
      return i === 6 ? t("admin.todayShort") : formatCalendar(dates[i]);
    }
    return relativeLabels[i];
  };

  const hasCounts = !!counts && counts.length > 0;
  const peakCount = hasCounts ? Math.max(...(counts as number[])) : 0;
  const peakIndex = hasCounts ? (counts as number[]).indexOf(peakCount) : -1;
  const totalBookings = hasCounts ? (counts as number[]).reduce((a, b) => a + b, 0) : 0;
  const peakWeekday =
    peakIndex >= 0 && dates && dates[peakIndex]
      ? fmtDateTime(new Date(dates[peakIndex]), locale, { weekday: "short" })
      : peakIndex >= 0
        ? relativeLabels[peakIndex]
        : "";

  const summary =
    totalBookings > 0
      ? t("admin.occupancySummary", {
          total: totalBookings,
          average: (totalBookings / 7).toFixed(1),
          peak: peakWeekday,
        })
      : t("admin.noBookingsLastSevenDays");

  return (
    <View style={styles.chartArea}>
      <View style={styles.chartHeaderRow}>
        <ThemedText
          testID="occupancy-summary"
          style={[styles.summaryText, { color: colors.muted }]}
        >
          {summary}
        </ThemedText>
        <View style={styles.toggleRow}>
          <Pressable
            testID="occupancy-toggle-relative"
            onPress={() => setLabelMode("relative")}
            style={[
              styles.toggleSegment,
              labelMode === "relative"
                ? { backgroundColor: primaryColor }
                : { backgroundColor: `${colors.muted}14` },
            ]}
          >
            <ThemedText
              style={[
                styles.toggleText,
                { color: labelMode === "relative" ? theme.colors.white : colors.muted },
              ]}
            >
              {t("admin.toggle.relative")}
            </ThemedText>
          </Pressable>
          <Pressable
            testID="occupancy-toggle-calendar"
            onPress={() => setLabelMode("calendar")}
            style={[
              styles.toggleSegment,
              labelMode === "calendar"
                ? { backgroundColor: primaryColor }
                : { backgroundColor: `${colors.muted}14` },
            ]}
          >
            <ThemedText
              style={[
                styles.toggleText,
                { color: labelMode === "calendar" ? theme.colors.white : colors.muted },
              ]}
            >
              {t("admin.toggle.dates")}
            </ThemedText>
          </Pressable>
        </View>
      </View>
      <View style={styles.chartBars}>
        {chartData.map((val, i) => {
          const isPeak = i === peakIndex;
          const count = hasCounts ? (counts as number[])[i] : null;
          return (
            <View key={i} style={styles.barContainer}>
              <ThemedText
                testID={`occupancy-count-${i}`}
                style={[
                  styles.countLabel,
                  { color: isPeak ? primaryColor : colors.muted },
                  isPeak && styles.countLabelPeak,
                ]}
              >
                {count === null ? "–" : count}
              </ThemedText>
              <View style={styles.barTrack}>
                <View
                  style={[
                    styles.barFill,
                    {
                      backgroundColor: isPeak
                        ? primaryColor
                        : isDark
                          ? `${primaryColor}99`
                          : `${primaryColor}55`,
                      height: `${Math.max(2, val)}%` as `${number}%`,
                    },
                  ]}
                />
              </View>
              <ThemedText style={[styles.barLabel, { color: colors.muted }]}>
                {labelFor(i)}
              </ThemedText>
            </View>
          );
        })}
      </View>
    </View>
  );
}

function BookingItem({
  booking,
  colors,
  isDark,
  locale,
  t,
  onPress,
}: {
  booking: BookingSummaryDto;
  colors: ThemeColors;
  isDark: boolean;
  locale: "en" | "es-CO";
  t: (key: any, values?: Record<string, string | number>) => string;
  onPress: () => void;
}) {
  const now = new Date();
  const startTime = new Date(booking.date);
  const endTime = booking.endTime
    ? new Date(booking.endTime)
    : new Date(startTime.getTime() + 60 * 60 * 1000);

  const isCancelled = !!booking.isCancelled;
  const isActive = !isCancelled && now >= startTime && now <= endTime;

  const bubbleBg = isCancelled
    ? `${theme.colors.error}1a`
    : isActive
      ? `${colors.success}18`
      : `${colors.muted}14`;

  const bubbleTextColor = isCancelled
    ? theme.colors.error
    : isActive
      ? colors.success
      : colors.muted;

  return (
    <Pressable
      onPress={onPress}
      style={({ hovered }: any) => [
        styles.bookingItem,
        { borderTopColor: colors.border },
        hovered && { backgroundColor: `${colors.muted}08` },
        isActive && { backgroundColor: `${colors.success}05` },
      ]}
    >
      <View style={[styles.bookingTime, { backgroundColor: bubbleBg }]}>
        <ThemedText style={[styles.bookingTimeText, { color: bubbleTextColor }]}>
          {fmtDateTime(startTime, locale, { hour: "2-digit", minute: "2-digit" })}
        </ThemedText>
      </View>
      <View style={styles.bookingInfo}>
        <View style={{ flexDirection: "row", alignItems: "center", gap: 8 }}>
          <ThemedText style={styles.bookingEmail} numberOfLines={1}>
            {booking.customerName ?? booking.customerEmail}
          </ThemedText>
          {isCancelled ? (
            <View style={styles.cancelledBadge}>
              <ThemedText style={styles.cancelledBadgeText}>{t("admin.cancelled")}</ThemedText>
            </View>
          ) : (
            <StatusBadge date={booking.date} isDark={isDark} />
          )}
        </View>
        {booking.customerName && (
          <ThemedText style={[styles.bookingMeta, { color: colors.muted }]} numberOfLines={1}>
            {booking.customerEmail}
          </ThemedText>
        )}
        <ThemedText style={[styles.bookingMeta, { color: colors.muted }]}>
          {t("admin.guestsAtRestaurant", {
            seats: booking.seats,
            restaurant: booking.restaurantName,
          })}
        </ThemedText>
      </View>
      <Ionicons name="chevron-forward" size={16} color={colors.muted} />
    </Pressable>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1 },
  outer: {
    padding: theme.spacing.xxl,
    paddingTop: theme.spacing.xxxl,
    paddingBottom: theme.spacing.xxxl,
    maxWidth: 1200,
    width: "100%",
    alignSelf: "center",
  },
  header: { marginBottom: theme.spacing.xxxl },
  pageTitle: { ...theme.typography.pageTitle },
  pageSub: { ...theme.typography.body, marginTop: theme.spacing.xs },
  spinner: { marginTop: 100 },
  metricsGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: theme.spacing.lg,
    marginBottom: theme.spacing.xxl,
  },
  metricsGridWide: { flexWrap: "nowrap" },
  metricCard: {
    flex: 1,
    minWidth: 200,
    padding: theme.spacing.xl,
    borderRadius: theme.borderRadius.card,
    borderWidth: 1,
  },
  metricIconWrap: {
    width: 40,
    height: 40,
    borderRadius: theme.borderRadius.xl,
    alignItems: "center",
    justifyContent: "center",
    marginBottom: theme.spacing.lg,
  },
  metricValue: { fontSize: 24, fontWeight: "800", marginBottom: theme.spacing.xs },
  metricLabel: { ...theme.typography.label },
  metricSub: { ...theme.typography.caption, marginTop: 2 },
  mainRow: { flexDirection: "column", gap: theme.spacing.xxl, marginBottom: theme.spacing.xxl },
  mainRowWide: { flexDirection: "row", alignItems: "flex-start" },
  chartCard: {
    borderRadius: theme.borderRadius.card,
    borderWidth: 1,
    padding: theme.spacing.xxl,
  },
  chartCardWide: { flex: 2 },
  chartHeader: { marginBottom: theme.spacing.lg },
  cardTitle: { ...theme.typography.h3 },
  chartSub: { ...theme.typography.body, marginTop: theme.spacing.xs },
  chartArea: { minHeight: 220 },
  chartHeaderRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: theme.spacing.md,
    marginBottom: theme.spacing.lg,
    flexWrap: "wrap",
  },
  summaryText: { ...theme.typography.caption, fontWeight: "600", flexShrink: 1 },
  toggleRow: {
    flexDirection: "row",
    borderRadius: theme.borderRadius.full,
    overflow: "hidden",
  },
  toggleSegment: { paddingHorizontal: theme.spacing.md, paddingVertical: theme.spacing.xsm },
  toggleText: { ...theme.typography.label, fontWeight: "700", fontSize: 12 },
  countLabel: { ...theme.typography.captionSmall, fontWeight: "700", fontSize: 12 },
  countLabelPeak: { fontSize: 13 },
  chartBars: {
    flexDirection: "row",
    alignItems: "flex-end",
    justifyContent: "space-between",
    gap: theme.spacing.md,
    height: 220,
  },
  barContainer: { flex: 1, alignItems: "center", gap: theme.spacing.sm },
  barTrack: {
    width: "100%",
    height: 180,
    backgroundColor: "rgba(0,0,0,0.03)",
    borderRadius: theme.borderRadius.sm,
    justifyContent: "flex-end",
    overflow: "hidden",
  },
  barFill: { width: "100%", borderRadius: theme.borderRadius.xs },
  barLabel: { ...theme.typography.caption, fontWeight: "600" },
  actionsCol: { flex: 1, gap: theme.spacing.lg },
  actionsColWide: { maxWidth: 300 },
  actionCard: {
    padding: theme.spacing.xl,
    borderRadius: theme.borderRadius.card,
    borderWidth: 1,
    flexDirection: "row",
    alignItems: "center",
    gap: theme.spacing.lg,
  },
  actionTitle: { ...theme.typography.bodyBold },
  listCard: { borderRadius: theme.borderRadius.card, borderWidth: 1, overflow: "hidden" },
  listHeader: {
    padding: theme.spacing.xl,
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
  },
  viewAll: { ...theme.typography.label },
  emptyRecent: { padding: 40, alignItems: "center" },
  emptyText: { fontStyle: "italic" },
  bookingItem: {
    padding: theme.spacing.lg,
    borderTopWidth: 1,
    flexDirection: "row",
    alignItems: "center",
    gap: theme.spacing.lg,
  },
  bookingTime: {
    paddingHorizontal: theme.spacing.xsm,
    paddingVertical: theme.spacing.xxs,
    borderRadius: theme.borderRadius.md,
  },
  bookingTimeText: { ...theme.typography.label, fontWeight: "700" },
  bookingInfo: { flex: 1, gap: 2 },
  bookingEmail: { ...theme.typography.label, fontWeight: "500" },
  bookingMeta: { ...theme.typography.caption },
  cancelledBadge: {
    backgroundColor: `${theme.colors.error}1a`,
    paddingHorizontal: theme.spacing.sm,
    paddingVertical: 3,
    borderRadius: theme.borderRadius.full,
  },
  cancelledBadgeText: {
    color: theme.colors.error,
    fontSize: 11,
    fontWeight: "700",
    letterSpacing: 0.4,
  },
});
