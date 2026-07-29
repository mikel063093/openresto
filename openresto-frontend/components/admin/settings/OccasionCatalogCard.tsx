import { useEffect, useState } from "react";
import { Pressable, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { ThemedText } from "@/components/themed-text";
import Button from "@/components/common/Button";
import Input from "@/components/common/Input";
import { AnimatedAccordion } from "@/components/common/AnimatedAccordion";
import { ToggleSwitch } from "./settingsShared";
import { useAppTheme } from "@/hooks/use-app-theme";
import {
  adminGetRestaurants,
  createOccasionCatalogItem,
  deleteOccasionCatalogItem,
  getOccasionCatalog,
  OccasionCatalogItemDto,
  updateOccasionCatalogItem,
  UpsertOccasionCatalogItemRequest,
} from "@/api/admin";
import { OccasionCatalogRow } from "./OccasionCatalogRow";
import { styles } from "./settings.styles";

type RestaurantOption = {
  id: number;
  name: string;
};

export function OccasionCatalogCard({
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
  const [items, setItems] = useState<OccasionCatalogItemDto[]>([]);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [estimatedPriceCop, setEstimatedPriceCop] = useState("");
  const [isActive, setIsActive] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ text: string; ok: boolean } | null>(null);

  useEffect(() => {
    adminGetRestaurants()
      .then((result) => {
        setRestaurants(result.map((restaurant) => ({ id: restaurant.id, name: restaurant.name })));
        setSelectedRestaurantId((current) => current ?? result[0]?.id ?? null);
      })
      .catch(() => {
        setMessage({ text: "No fue posible cargar los restaurantes.", ok: false });
      });
  }, []);

  useEffect(() => {
    if (!selectedRestaurantId) return;
    getOccasionCatalog(selectedRestaurantId)
      .then(setItems)
      .catch((error) => {
        setMessage({
          text: error instanceof Error ? error.message : "No fue posible cargar el catálogo.",
          ok: false,
        });
      });
  }, [selectedRestaurantId]);

  const resetForm = () => {
    setName("");
    setDescription("");
    setEstimatedPriceCop("");
    setIsActive(true);
  };

  const handleCreate = async () => {
    if (!selectedRestaurantId) return;
    const parsedPrice = Number.parseInt(estimatedPriceCop, 10);
    if (!name.trim()) {
      setMessage({ text: "El nombre de la ocasión es obligatorio.", ok: false });
      return;
    }
    if (!Number.isFinite(parsedPrice)) {
      setMessage({ text: "Ingresa un precio estimado válido en COP.", ok: false });
      return;
    }

    setSaving(true);
    setMessage(null);
    try {
      const created = await createOccasionCatalogItem(selectedRestaurantId, {
        name: name.trim(),
        description: description.trim() || null,
        estimatedPriceCop: parsedPrice,
        isActive,
      });
      setItems((current) => [...current, created].sort((a, b) => a.sortOrder - b.sortOrder));
      resetForm();
      setMessage({ text: "Ítem agregado al catálogo.", ok: true });
    } catch (error) {
      setMessage({
        text: error instanceof Error ? error.message : "No fue posible crear el ítem.",
        ok: false,
      });
    } finally {
      setSaving(false);
    }
  };

  const handleUpdate = async (itemId: number, request: UpsertOccasionCatalogItemRequest) => {
    if (!selectedRestaurantId) return;
    const updated = await updateOccasionCatalogItem(selectedRestaurantId, itemId, request);
    setItems((current) => current.map((item) => (item.id === itemId ? updated : item)));
    setMessage({ text: "Ítem actualizado.", ok: true });
  };

  const handleDelete = async (itemId: number) => {
    if (!selectedRestaurantId) return;
    await deleteOccasionCatalogItem(selectedRestaurantId, itemId);
    setItems((current) => current.filter((item) => item.id !== itemId));
    setMessage({ text: "Ítem eliminado.", ok: true });
  };

  return (
    <View style={[styles.secCard, { backgroundColor: cardBg, borderColor }]}>
      <Pressable
        accessibilityRole="button"
        accessibilityLabel="Mostrar catálogo de ocasiones"
        style={styles.secHeader}
        onPress={() => setExpanded((value) => !value)}
      >
        <View style={[styles.secIcon, { backgroundColor: `${primaryColor}14` }]}>
          <Ionicons name="gift-outline" size={20} color={primaryColor} />
        </View>
        <View style={{ flex: 1 }}>
          <ThemedText style={styles.secTitle}>Catálogo de ocasiones</ThemedText>
          <ThemedText style={[styles.secSub, { color: mutedColor }]}>
            Administra los extras opcionales que pueden mostrarse en WhatsApp de prueba.
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
            <ThemedText style={styles.fieldLabel}>Nuevo ítem</ThemedText>
            <Input value={name} onChangeText={setName} placeholder="Cumpleaños" />
          </View>
          <View style={styles.field}>
            <ThemedText style={styles.fieldLabel}>Descripción</ThemedText>
            <Input
              value={description}
              onChangeText={setDescription}
              placeholder="Decoración y mensaje especial"
            />
          </View>
          <View style={styles.field}>
            <ThemedText style={styles.fieldLabel}>Precio estimado en COP</ThemedText>
            <Input
              value={estimatedPriceCop}
              onChangeText={setEstimatedPriceCop}
              placeholder="85000"
              keyboardType="number-pad"
            />
          </View>
          <View
            style={[styles.secRow, { borderTopWidth: 0, paddingHorizontal: 0, paddingVertical: 0 }]}
          >
            <View style={{ flex: 1 }}>
              <ThemedText style={styles.secRowTitle}>Disponible en nuevas reservas</ThemedText>
              <ThemedText style={[styles.secRowSub, { color: mutedColor }]}>
                Deja el ítem activo para ofrecerlo en conversaciones futuras.
              </ThemedText>
            </View>
            <ToggleSwitch
              checked={isActive}
              onChange={setIsActive}
              primaryColor={primaryColor}
              borderColor={borderColor}
            />
          </View>

          <Button onPress={handleCreate} disabled={!selectedRestaurantId || saving}>
            Agregar ítem
          </Button>

          {message ? (
            <ThemedText style={message.ok ? styles.successText : styles.errorText}>
              {message.text}
            </ThemedText>
          ) : null}

          <View style={{ gap: 12 }}>
            {items.length === 0 ? (
              <ThemedText style={[styles.helperText, { color: mutedColor }]}>
                Aún no hay ocasiones configuradas para este restaurante.
              </ThemedText>
            ) : (
              items.map((item) => (
                <OccasionCatalogRow
                  key={item.id}
                  item={item}
                  borderColor={borderColor}
                  mutedColor={mutedColor}
                  onSave={(request) => handleUpdate(item.id, request)}
                  onDelete={() => handleDelete(item.id)}
                />
              ))
            )}
          </View>
        </View>
      </AnimatedAccordion>
    </View>
  );
}
