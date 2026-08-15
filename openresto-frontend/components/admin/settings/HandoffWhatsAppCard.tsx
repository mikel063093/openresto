import { useEffect, useState } from "react";
import { Pressable, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { ThemedText } from "@/components/themed-text";
import Button from "@/components/common/Button";
import Input from "@/components/common/Input";
import { AnimatedAccordion } from "@/components/common/AnimatedAccordion";
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

export function HandoffWhatsAppCard({
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
  const [handoffNumber, setHandoffNumber] = useState("");
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
        setHandoffNumber(settings.handoffWhatsAppE164 ?? "");
      })
      .catch((error) => {
        setMessage({
          text: error instanceof Error ? error.message : "No fue posible cargar el handoff.",
          ok: false,
        });
      })
      .finally(() => {
        setLoadingSettings(false);
      });
  }, [selectedRestaurantId]);

  const handleSave = async () => {
    if (!selectedRestaurantId) return;
    const trimmed = handoffNumber.trim();
    if (enabled && !trimmed) {
      setMessage({
        text: "El número de handoff es obligatorio mientras WhatsApp de prueba esté habilitado.",
        ok: false,
      });
      return;
    }

    setSaving(true);
    setMessage(null);
    try {
      const updated = await updateRestaurantWhatsAppSettings(selectedRestaurantId, {
        isWhatsAppTestEnabled: enabled,
        handoffWhatsAppE164: trimmed || null,
      });
      setEnabled(updated.isWhatsAppTestEnabled);
      setHandoffNumber(updated.handoffWhatsAppE164 ?? "");
      setMessage({ text: "Número de handoff actualizado.", ok: true });
    } catch (error) {
      setMessage({
        text: error instanceof Error ? error.message : "No fue posible guardar el handoff.",
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
        accessibilityLabel="Mostrar configuración de handoff de WhatsApp"
        style={styles.secHeader}
        onPress={() => setExpanded((value) => !value)}
      >
        <View style={[styles.secIcon, { backgroundColor: `${primaryColor}14` }]}>
          <Ionicons name="call-outline" size={20} color={primaryColor} />
        </View>
        <View style={{ flex: 1 }}>
          <ThemedText style={styles.secTitle}>Número de handoff</ThemedText>
          <ThemedText style={[styles.secSub, { color: mutedColor }]}>
            Define el destino humano al que `n8n` puede reenviar el resumen sanitizado.
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

          <View style={styles.field}>
            <ThemedText style={styles.fieldLabel}>WhatsApp del equipo humano</ThemedText>
            <Input
              value={handoffNumber}
              onChangeText={setHandoffNumber}
              placeholder="+57 300 123 4567"
              keyboardType="phone-pad"
              autoCapitalize="none"
              autoCorrect={false}
              editable={!loadingSettings && !saving}
            />
            <ThemedText style={[styles.helperText, { color: mutedColor }]}>
              Guarda el número en formato internacional. El backend normaliza y valida el E.164.
            </ThemedText>
          </View>

          {message ? (
            <ThemedText style={message.ok ? styles.successText : styles.errorText}>
              {message.text}
            </ThemedText>
          ) : null}

          <Button
            onPress={handleSave}
            disabled={!selectedRestaurantId || saving || loadingSettings}
          >
            Guardar handoff
          </Button>
        </View>
      </AnimatedAccordion>
    </View>
  );
}
