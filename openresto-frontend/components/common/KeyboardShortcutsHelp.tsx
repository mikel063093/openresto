import { Modal, Pressable, StyleSheet, View, TouchableWithoutFeedback } from "react-native";
import { ThemedText } from "@/components/themed-text";
import { theme } from "@/theme/theme";
import { useAppTheme } from "@/hooks/use-app-theme";
import { SHORTCUTS_BY_SCOPE, ShortcutScope } from "@/constants/keyboardShortcuts";
import { useI18n } from "@/context/I18nContext";

interface KeyboardShortcutsHelpProps {
  visible: boolean;
  scope: ShortcutScope;
  onClose: () => void;
}

export default function KeyboardShortcutsHelp({
  visible,
  scope,
  onClose,
}: KeyboardShortcutsHelpProps) {
  const { t } = useI18n();
  const { colors } = useAppTheme();
  const shortcuts = SHORTCUTS_BY_SCOPE[scope];

  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={onClose}>
      <Pressable testID="keyboard-shortcuts-backdrop" style={styles.backdrop} onPress={onClose}>
        <TouchableWithoutFeedback>
          <View style={[styles.card, { backgroundColor: colors.card, borderColor: colors.border }]}>
            <ThemedText type="h3">{t("shortcuts.title")}</ThemedText>
            <View style={styles.list}>
              {shortcuts.map((s) => (
                <View key={s.keys} style={styles.row}>
                  <View
                    style={[
                      styles.keyBadge,
                      { borderColor: colors.border, backgroundColor: colors.input },
                    ]}
                  >
                    <ThemedText style={styles.keyText}>{s.keys}</ThemedText>
                  </View>
                  <ThemedText style={[styles.description, { color: colors.muted }]}>
                    {s.description}
                  </ThemedText>
                </View>
              ))}
            </View>
            <Pressable
              testID="keyboard-shortcuts-close"
              style={[styles.closeBtn, { borderColor: colors.border }]}
              onPress={onClose}
            >
              <ThemedText style={[styles.closeBtnText, { color: colors.muted }]}>
                {t("common.closeShort")}
              </ThemedText>
            </Pressable>
          </View>
        </TouchableWithoutFeedback>
      </Pressable>
    </Modal>
  );
}

const styles = StyleSheet.create({
  backdrop: {
    flex: 1,
    backgroundColor: "rgba(0,0,0,0.5)",
    justifyContent: "center",
    alignItems: "center",
    padding: theme.spacing.xxl,
  },
  card: {
    borderRadius: theme.borderRadius.modal,
    borderWidth: 1,
    padding: theme.spacing.xxl,
    width: "100%",
    maxWidth: 420,
    gap: theme.spacing.md,
    ...theme.shadows.popup,
  },
  list: {
    gap: theme.spacing.sm,
  },
  row: {
    flexDirection: "row",
    alignItems: "center",
    gap: theme.spacing.md,
  },
  keyBadge: {
    minWidth: 56,
    paddingHorizontal: theme.spacing.sm,
    paddingVertical: theme.spacing.xxs,
    borderRadius: theme.borderRadius.md,
    borderWidth: 1,
    alignItems: "center",
  },
  keyText: {
    ...theme.typography.captionSmall,
    fontWeight: "700",
  },
  description: {
    ...theme.typography.caption,
    flex: 1,
  },
  closeBtn: {
    paddingVertical: 11,
    borderRadius: theme.borderRadius.lg,
    alignItems: "center",
    marginTop: theme.spacing.sm,
    borderWidth: 1,
  },
  closeBtnText: {
    ...theme.typography.bodyBold,
  },
});
