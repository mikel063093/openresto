import { Pressable, StyleSheet, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { ThemedText } from "@/components/themed-text";
import { ThemedView } from "@/components/themed-view";
import { useAppTheme } from "@/hooks/use-app-theme";
import { useI18n } from "@/context/I18nContext";

interface ErrorScreenProps {
  /** Defaults to "Something went wrong". */
  title?: string;
  /** Defaults to "An unexpected error occurred. Try again.". */
  message?: string;
  /** When provided, a "Try again" action is shown that calls this. */
  retry?: () => void;
  /** When provided, a "Go to home" action is shown that calls this. */
  onGoHome?: () => void;
}

/**
 * Full-screen fallback UI for unrecoverable render errors. Presentational —
 * no data, no side effects. Modeled on the existing +not-found.tsx visual
 * language (faded icon, title, muted message, primary-color actions).
 *
 * Used by the root error.tsx boundary (Bundle 12). The retry/onGoHome props
 * are optional so the same component can serve boundaries that have no
 * navigation context.
 */
export default function ErrorScreen({
  title,
  message,
  retry,
  onGoHome,
}: ErrorScreenProps) {
  const { t } = useI18n();
  const { colors, primaryColor, isDark } = useAppTheme();
  const mutedColor = isDark ? colors.muted : "#666";
  const resolvedTitle = title ?? t("error.title");
  const resolvedMessage = message ?? t("error.message");

  return (
    <ThemedView style={styles.root}>
      <View style={styles.content}>
        <View style={[styles.iconRing, { borderColor: colors.border }]}>
          <Ionicons name="warning-outline" size={32} color={mutedColor} />
        </View>
        <ThemedText style={styles.title}>{resolvedTitle}</ThemedText>
        <ThemedText style={[styles.message, { color: mutedColor }]}>{resolvedMessage}</ThemedText>
        {(retry || onGoHome) && (
          <View style={styles.actions}>
            {retry && (
              <Pressable
                style={[styles.btn, { backgroundColor: primaryColor }]}
                onPress={retry}
                accessibilityRole="button"
                accessibilityLabel={t("error.tryAgain")}
              >
                <ThemedText style={styles.btnText}>{t("error.tryAgain")}</ThemedText>
              </Pressable>
            )}
            {onGoHome && (
              <Pressable
                style={[styles.btnOutline, { borderColor: primaryColor }]}
                onPress={onGoHome}
                accessibilityRole="button"
                accessibilityLabel={t("error.goHome")}
              >
                <ThemedText style={[styles.btnOutlineText, { color: primaryColor }]}>
                  {t("error.goHome")}
                </ThemedText>
              </Pressable>
            )}
          </View>
        )}
      </View>
    </ThemedView>
  );
}

const styles = StyleSheet.create({
  root: { flex: 1, justifyContent: "center", alignItems: "center", padding: 32 },
  content: { alignItems: "center", gap: 12, maxWidth: 420 },
  iconRing: {
    width: 64,
    height: 64,
    borderRadius: 32,
    borderWidth: 1,
    alignItems: "center",
    justifyContent: "center",
    marginBottom: 4,
  },
  title: { fontSize: 22, fontWeight: "600", letterSpacing: -0.3, textAlign: "center" },
  message: { fontSize: 14, lineHeight: 20, textAlign: "center" },
  actions: {
    flexDirection: "row",
    gap: 12,
    marginTop: 16,
    flexWrap: "wrap",
    justifyContent: "center",
  },
  btn: { paddingVertical: 11, paddingHorizontal: 20, borderRadius: 10, alignItems: "center" },
  btnText: { color: "#fff", fontWeight: "700", fontSize: 14 },
  btnOutline: {
    paddingVertical: 10,
    paddingHorizontal: 20,
    borderRadius: 10,
    borderWidth: 1,
    alignItems: "center",
  },
  btnOutlineText: { fontWeight: "600", fontSize: 14 },
});
