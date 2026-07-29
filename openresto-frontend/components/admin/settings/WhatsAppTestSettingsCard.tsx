import { useEffect, useState } from "react";
import { Pressable, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { ThemedText } from "@/components/themed-text";
import Button from "@/components/common/Button";
import { AnimatedAccordion } from "@/components/common/AnimatedAccordion";
import { ToggleSwitch } from "./settingsShared";
import { useAppTheme } from "@/hooks/use-app-theme";
import {
  adminGetRestaurants,
  getRestaurantWhatsAppSettings,
  updateRestaurantWhatsAppSettings,
} from "@/api/admin";
import { styles } from "./settings.styles";

type RestaurantOption = {
  id: number;
  name: string;
};

export function WhatsAppTestSettingsCard({
  borderColor,
  mutedColor,
  cardBg,
}: {
  borderColor: string;
  mutedColor: string;
  cardBg: string;
}) {
  const { primaryColor } = useAppTheme();
  const [expanded, setExpanded] = useState(true);
  const [restaurants, setRestaurants] = useState<RestaurantOption[]>([]);
  const [selectedRestaurantId, setSelectedRestaurantId] = useState<number | null>(null);
  const [enabled, setEnabled] = useState(false);
  const [handoffWhatsAppE164, setHandoffWhatsAppE164] = useState<string | null>(null);
  const [loadingSettings, setLoadingSettings] = useState(false);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ text: string; ok: boolean } | null>(null);

  useEffect(() => {
    adminGetRestaurants()
      .then((items) => {
        setRestaurants(items.map((item) => ({ id: item.id, name: item.name })));
        setSelectedRestaurantId((current) => current ?? items[0]?.id ?? null);
      })
      .catch(() => {
        setMessage({ text: "No fue posible cargar los restaurantes.", ok: false });
      });
  }, []);

  useEffect(() => {
    if (!selectedRestaurantId) return;
    setLoadingSettings(true);
    getRestaurantWhatsAppSettings(selectedRestaurantId)
      .then((settings) => {
        setEnabled(settings.isWhatsAppTestEnabled);
        setHandoffWhatsAppE164(settings.handoffWhatsAppE164);
      })
      .catch((error) => {
        setMessage({
          text: error instanceof Error ? error.message : "No fue posible cargar la configuración.",
          ok: false,
        });
      })
      .finally(() => {
        setLoadingSettings(false);
      });
  }, [selectedRestaurantId]);

  const handleSave = async () => {
    if (!selectedRestaurantId) return;
    if (enabled && !handoffWhatsAppE164?.trim()) {
      setMessage({
        text: "Configura primero el número de handoff antes de habilitar WhatsApp de prueba.",
        ok: false,
      });
      return;
    }

    setSaving(true);
    setMessage(null);
    try {
      const updated = await updateRestaurantWhatsAppSettings(selectedRestaurantId, {
        isWhatsAppTestEnabled: enabled,
        handoffWhatsAppE164,
      });
      setEnabled(updated.isWhatsAppTestEnabled);
      setHandoffWhatsAppE164(updated.handoffWhatsAppE164);
      setMessage({ text: "Visibilidad de WhatsApp actualizada.", ok: true });
    } catch (error) {
      setMessage({
        text: error instanceof Error ? error.message : "No fue posible guardar la configuración.",
        ok: false,
      });
    } finally {
      setSaving(false);
    }
  };

  return (
    <View style={[styles.secCard, { backgroundColor: cardBg, borderColor }]}>
      <Pressable
        accessibilityRole="button"
        accessibilityLabel="Mostrar configuración de visibilidad de WhatsApp"
        style={styles.secHeader}
        onPress={() => setExpanded((value) => !value)}
      >
        <View style={[styles.secIcon, { backgroundColor: `${primaryColor}14` }]}>
          <Ionicons name="logo-whatsapp" size={20} color={primaryColor} />
        </View>
        <View style={{ flex: 1 }}>
          <ThemedText style={styles.secTitle}>WhatsApp de prueba</ThemedText>
          <ThemedText style={[styles.secSub, { color: mutedColor }]}>
            Controla qué restaurante aparece en el canal interno de prueba.
          </ThemedText>
        </View>
        <Ionicons name={expanded ? "chevron-up" : "chevron-down"} size={18} color={mutedColor} />
      </Pressable>

      <AnimatedAccordion expanded={expanded}>
        <View style={[styles.secForm, { borderTopColor: borderColor }]}>
          <View style={styles.field}>
            <ThemedText style={styles.fieldLabel}>Restaurante</ThemedText>
            <View style={styles.chipsRow}>
              {restaurants.map((restaurant) => {
                const selected = restaurant.id === selectedRestaurantId;
                return (
                  <Pressable
                    key={restaurant.id}
                    accessibilityRole="button"
                    onPress={() => setSelectedRestaurantId(restaurant.id)}
                    style={[
                      styles.chip,
                      {
                        borderColor,
                        backgroundColor: selected ? `${primaryColor}18` : "transparent",
                      },
                    ]}
                  >
                    <ThemedText style={{ color: selected ? primaryColor : mutedColor }}>
                      {restaurant.name}
                    </ThemedText>
                  </Pressable>
                );
              })}
            </View>
          </View>

          <View style={[styles.secRow, { borderTopColor: borderColor, paddingHorizontal: 0 }]}>
            <View style={{ flex: 1 }}>
              <ThemedText style={styles.secRowTitle}>Visible en WhatsApp de prueba</ThemedText>
              <ThemedText style={[styles.secRowSub, { color: mutedColor }]}>
                Cuando está activo, el restaurante puede usarse desde el flujo de reservas de
                WhatsApp en ambiente de prueba.
              </ThemedText>
            </View>
            <ToggleSwitch
              checked={enabled}
              onChange={setEnabled}
              primaryColor={primaryColor}
              borderColor={borderColor}
              disabled={!selectedRestaurantId || saving || loadingSettings}
            />
          </View>

          <ThemedText style={[styles.helperText, { color: mutedColor }]}>
            OpenResto sigue siendo la única autoridad de reservas. Esta pantalla no expone
            credenciales de Meta ni acceso directo a `n8n`.
          </ThemedText>

          {message ? (
            <ThemedText style={message.ok ? styles.successText : styles.errorText}>
              {message.text}
            </ThemedText>
          ) : null}

          <Button
            onPress={handleSave}
            disabled={!selectedRestaurantId || saving || loadingSettings}
          >
            Guardar visibilidad
          </Button>
        </View>
      </AnimatedAccordion>
    </View>
  );
}
