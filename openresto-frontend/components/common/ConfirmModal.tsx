import { Modal, Pressable, StyleSheet, View, TouchableWithoutFeedback } from "react-native";
import * as Haptics from "expo-haptics";
import { ThemedText } from "@/components/themed-text";
import { theme } from "@/theme/theme";
import { useAppTheme } from "@/hooks/use-app-theme";
import { useI18n } from "@/context/I18nContext";

interface ConfirmModalProps {
  visible: boolean;
  title?: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  destructive?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export default function ConfirmModal({
  visible,
  title,
  message,
  confirmLabel,
  cancelLabel,
  destructive = false,
  onConfirm,
  onCancel,
}: ConfirmModalProps) {
  const { t } = useI18n();
  const { colors, primaryColor } = useAppTheme();
  const resolvedTitle = title ?? t("common.confirm");
  const resolvedConfirmLabel = confirmLabel ?? t("common.confirm");
  const resolvedCancelLabel = cancelLabel ?? t("common.cancel");

  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={onCancel}>
      <Pressable style={styles.backdrop} onPress={onCancel}>
        <TouchableWithoutFeedback>
          <View style={[styles.card, { backgroundColor: colors.card, borderColor: colors.border }]}>
            <ThemedText type="h3">{resolvedTitle}</ThemedText>
            <ThemedText style={[styles.message, { color: colors.muted }]}>{message}</ThemedText>
            <View style={[styles.actions, { borderTopColor: colors.border }]}>
              <Pressable
                style={[styles.btn, styles.cancelBtn, { borderColor: colors.border }]}
                onPress={() => {
                  Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Light);
                  onCancel();
                }}
              >
                <ThemedText style={[styles.btnText, { color: colors.muted }]}>
                  {resolvedCancelLabel}
                </ThemedText>
              </Pressable>
              <Pressable
                style={[
                  styles.btn,
                  {
                    backgroundColor: destructive ? theme.colors.error : primaryColor,
                  },
                ]}
                onPress={() => {
                  Haptics.notificationAsync(
                    destructive
                      ? Haptics.NotificationFeedbackType.Warning
                      : Haptics.NotificationFeedbackType.Success
                  );
                  onConfirm();
                }}
              >
                <ThemedText style={styles.confirmBtnText}>{resolvedConfirmLabel}</ThemedText>
              </Pressable>
            </View>
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
    maxWidth: 400,
    gap: theme.spacing.md,
    ...theme.shadows.popup,
  },
  message: {
    fontSize: 14,
    lineHeight: 20,
  },
  actions: {
    flexDirection: "row",
    gap: theme.spacing.xsm,
    marginTop: theme.spacing.sm,
    paddingTop: theme.spacing.lg,
    borderTopWidth: 1,
  },
  btn: {
    flex: 1,
    ...theme.buttonSizes.secondary,
    borderRadius: theme.borderRadius.lg,
    alignItems: "center",
  },
  cancelBtn: {
    borderWidth: 1,
  },
  btnText: {
    ...theme.typography.bodyBold,
  },
  confirmBtnText: {
    color: theme.colors.white,
    ...theme.typography.bodyBold,
    fontWeight: "700",
  },
});
