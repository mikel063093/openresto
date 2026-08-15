import { ScrollView, View, Platform } from "react-native";
import { useEffect, useState } from "react";
import { Stack } from "expo-router";
import { ThemedText } from "@/components/themed-text";
import { useAppTheme } from "@/hooks/use-app-theme";

// Components
import { BrandSettingsCard } from "@/components/admin/settings/BrandSettingsCard";
import { FooterSettingsCard } from "@/components/admin/settings/FooterSettingsCard";
import { EmailSettingsCard } from "@/components/admin/settings/EmailSettingsCard";
import { SecurityCard } from "@/components/admin/settings/SecurityCard";
import { HighlightsCard } from "@/components/admin/settings/HighlightsCard";
import { PushNotificationsCard } from "@/components/admin/settings/PushNotificationsCard";
import { UsersRolesCard } from "@/components/admin/settings/UsersRolesCard";
import { OperatorCredentialsCard } from "@/components/admin/settings/OperatorCredentialsCard";
import { WhatsAppTestSettingsCard } from "@/components/admin/settings/WhatsAppTestSettingsCard";
import { HandoffWhatsAppCard } from "@/components/admin/settings/HandoffWhatsAppCard";
import { OccasionCatalogCard } from "@/components/admin/settings/OccasionCatalogCard";
import { checkSession } from "@/api/auth";
import { styles } from "@/components/admin/settings/settings.styles";
import { useI18n } from "@/context/I18nContext";

export default function AdminSettingsScreen() {
  const { colors, isDark } = useAppTheme();
  const { t } = useI18n();
  const [role, setRole] = useState<string | null>(null);
  useEffect(() => {
    checkSession().then((session) => {
      if (session && session !== "rate-limited") setRole(session.role ?? null);
    });
  }, []);

  const borderColor = colors.border;
  const cardBg = colors.card;
  const mutedColor = colors.muted;

  return (
    <ScrollView contentContainerStyle={styles.container}>
      {Platform.OS !== "web" && <Stack.Screen options={{ title: t("admin.settings") }} />}

      {/* Page header */}
      <View style={styles.pageHeader}>
        <View>
          <ThemedText type="h1">{t("admin.settings")}</ThemedText>
          <ThemedText style={[styles.pageSub, { color: mutedColor }]}>
            {t("admin.settingsSubtitle")}
          </ThemedText>
        </View>
      </View>

      {/* Global Settings */}
      <View style={styles.section}>
        <ThemedText style={[styles.sectionHeading, { color: mutedColor }]}>
          {t("admin.globalSettings")}
        </ThemedText>
        <BrandSettingsCard borderColor={borderColor} mutedColor={mutedColor} cardBg={cardBg} />
        <FooterSettingsCard borderColor={borderColor} mutedColor={mutedColor} cardBg={cardBg} />
        <HighlightsCard borderColor={borderColor} mutedColor={mutedColor} cardBg={cardBg} />
        <EmailSettingsCard
          borderColor={borderColor}
          mutedColor={mutedColor}
          cardBg={cardBg}
          isDark={isDark}
        />
        <PushNotificationsCard />
      </View>

      {/* Account Security */}
      <View style={styles.section}>
        <ThemedText style={[styles.sectionHeading, { color: mutedColor }]}>
          {t("admin.accountSecurity")}
        </ThemedText>
        <SecurityCard borderColor={borderColor} mutedColor={mutedColor} cardBg={cardBg} />
      </View>
      {role === "SuperAdmin" && (
        <>
          <View style={styles.section}>
            <ThemedText style={[styles.sectionHeading, { color: mutedColor }]}>
              {t("admin.accessManagement")}
            </ThemedText>
            <UsersRolesCard borderColor={borderColor} mutedColor={mutedColor} cardBg={cardBg} />
          </View>
          <View style={styles.section}>
            <ThemedText style={[styles.sectionHeading, { color: mutedColor }]}>
              ACCESO MCP INTERNO
            </ThemedText>
            <OperatorCredentialsCard
              borderColor={borderColor}
              mutedColor={mutedColor}
              cardBg={cardBg}
            />
          </View>
          <View style={styles.section}>
            <ThemedText style={[styles.sectionHeading, { color: mutedColor }]}>
              WHATSAPP DE PRUEBA
            </ThemedText>
            <WhatsAppTestSettingsCard
              borderColor={borderColor}
              mutedColor={mutedColor}
              cardBg={cardBg}
            />
            <HandoffWhatsAppCard
              borderColor={borderColor}
              mutedColor={mutedColor}
              cardBg={cardBg}
            />
            <OccasionCatalogCard
              borderColor={borderColor}
              mutedColor={mutedColor}
              cardBg={cardBg}
            />
          </View>
        </>
      )}
    </ScrollView>
  );
}
