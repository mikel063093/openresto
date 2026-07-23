import { Pressable, StyleSheet, View } from "react-native";
import { useI18n } from "@/context/I18nContext";
import { ThemedText } from "@/components/themed-text";
import { useAppTheme } from "@/hooks/use-app-theme";

export default function LanguageSelector() {
  const { locale, setLocale, t } = useI18n();
  const { colors, primaryColor } = useAppTheme();

  return (
    <View accessibilityRole="radiogroup" accessibilityLabel={t("language.label")} style={styles.container}>
      {(["en", "es-CO"] as const).map((option) => {
        const selected = locale === option;
        const label = option === "en" ? t("language.english") : t("language.spanish");
        const accessibilityLabel =
          option === "en" ? t("language.switchToEnglish") : t("language.switchToSpanish");
        return (
          <Pressable
            key={option}
            accessibilityRole="radio"
            accessibilityState={{ selected }}
            accessibilityLabel={accessibilityLabel}
            onPress={() => setLocale(option)}
            style={[
              styles.option,
              { borderColor: selected ? primaryColor : colors.border },
              selected && { backgroundColor: primaryColor },
            ]}
          >
            <ThemedText style={[styles.label, { color: selected ? "#fff" : colors.muted }]}>
              {label}
            </ThemedText>
          </Pressable>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: "row",
    gap: 4,
    alignItems: "center",
  },
  option: {
    minHeight: 32,
    justifyContent: "center",
    paddingHorizontal: 9,
    borderWidth: 1,
    borderRadius: 8,
  },
  label: {
    fontSize: 12,
    fontWeight: "700",
  },
});
