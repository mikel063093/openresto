import { ActivityIndicator, Pressable, StyleSheet } from "react-native";
import { ThemedText } from "../themed-text";
import { ThemedView } from "../themed-view";
import { HoldStatus } from "./useTableHold";
import { useAppTheme } from "@/hooks/use-app-theme";
import { useI18n } from "@/context/I18nContext";

interface HoldStatusBannerProps {
  holdStatus: HoldStatus;
  secondsLeft: number;
  hasSelection: boolean;
  /** Specific rejection reason from the backend (e.g. past time, closed). Falls back to a generic line when absent. */
  holdMessage?: string | null;
  onRefresh?: () => void;
}

export default function HoldStatusBanner({
  holdStatus,
  secondsLeft,
  hasSelection,
  holdMessage,
  onRefresh,
}: HoldStatusBannerProps) {
  const { colors, isDark } = useAppTheme();
  const { t } = useI18n();

  if (!hasSelection) {
    return null;
  }

  switch (holdStatus) {
    case "pending":
      return (
        <ThemedView style={styles.holdRow}>
          <ActivityIndicator size="small" />
          <ThemedText style={styles.holdPending}>{t("booking.holdChecking")}</ThemedText>
        </ThemedView>
      );
    case "held": {
      const mins = Math.floor(secondsLeft / 60);
      const secs = secondsLeft % 60;
      return (
        <ThemedView style={styles.holdRow}>
          <ThemedText style={[styles.holdHeld, { color: colors.success }]}>
            ✓ {t("booking.holdHeld", { time: `${mins}:${secs.toString().padStart(2, "0")}` })}
          </ThemedText>
        </ThemedView>
      );
    }
    case "unavailable":
      return (
        <ThemedView style={styles.holdRow}>
          <ThemedText style={[styles.holdUnavailable, { color: colors.error }]}>
            ✗ {holdMessage ?? t("booking.holdUnavailableGeneric")}
          </ThemedText>
        </ThemedView>
      );
    case "expired":
      return (
        <ThemedView style={styles.expiredBox}>
          <ThemedText style={[styles.holdUnavailable, { color: colors.error }]}>
            {t("booking.holdExpired")}
          </ThemedText>
          {onRefresh && (
            <Pressable
              onPress={onRefresh}
              style={[
                styles.refreshBtn,
                { backgroundColor: isDark ? "rgba(220,38,38,0.15)" : "rgba(220,38,38,0.1)" },
              ]}
            >
              <ThemedText style={[styles.refreshBtnText, { color: colors.error }]}>
                {t("booking.refreshPage")}
              </ThemedText>
            </Pressable>
          )}
        </ThemedView>
      );
    default:
      return null;
  }
}

const styles = StyleSheet.create({
  holdRow: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8,
    marginTop: 6,
    backgroundColor: "transparent",
  },
  holdPending: {
    opacity: 0.6,
    fontSize: 13,
  },
  holdHeld: {
    fontSize: 13,
    fontWeight: "600",
  },
  holdUnavailable: {
    fontSize: 13,
  },
  expiredBox: {
    gap: 8,
    marginTop: 6,
    backgroundColor: "transparent",
  },
  refreshBtn: {
    alignSelf: "flex-start",
    paddingVertical: 6,
    paddingHorizontal: 14,
    borderRadius: 6,
  },
  refreshBtnText: {
    fontSize: 13,
    fontWeight: "600",
  },
});
