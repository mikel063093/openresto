import { useState } from "react";
import { Pressable, View } from "react-native";
import { ThemedText } from "@/components/themed-text";
import Button from "@/components/common/Button";
import Input from "@/components/common/Input";
import type { OccasionCatalogItemDto, UpsertOccasionCatalogItemRequest } from "@/api/admin";
import { ToggleSwitch } from "./settingsShared";
import { useAppTheme } from "@/hooks/use-app-theme";
import { styles } from "./settings.styles";

export function OccasionCatalogRow({
  item,
  borderColor,
  mutedColor,
  onSave,
  onDelete,
}: {
  item: OccasionCatalogItemDto;
  borderColor: string;
  mutedColor: string;
  onSave: (request: UpsertOccasionCatalogItemRequest) => Promise<void>;
  onDelete: () => Promise<void>;
}) {
  const { primaryColor } = useAppTheme();
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(item.name);
  const [description, setDescription] = useState(item.description ?? "");
  const [estimatedPriceCop, setEstimatedPriceCop] = useState(String(item.estimatedPriceCop));
  const [isActive, setIsActive] = useState(item.isActive);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  const priceLabel = new Intl.NumberFormat("es-CO").format(item.estimatedPriceCop);

  const handleSave = async () => {
    const parsedPrice = Number.parseInt(estimatedPriceCop, 10);
    if (!name.trim()) {
      setMessage("El nombre de la ocasión es obligatorio.");
      return;
    }
    if (!Number.isFinite(parsedPrice)) {
      setMessage("Ingresa un precio estimado válido en COP.");
      return;
    }

    setSaving(true);
    setMessage(null);
    try {
      await onSave({
        name: name.trim(),
        description: description.trim() || null,
        estimatedPriceCop: parsedPrice,
        isActive,
      });
      setEditing(false);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "No fue posible guardar el ítem.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <View style={[styles.noteBox, { borderColor, backgroundColor: "transparent" }]}>
      {editing ? (
        <>
          <View style={styles.field}>
            <ThemedText style={styles.fieldLabel}>Nombre</ThemedText>
            <Input value={name} onChangeText={setName} placeholder="Cumpleaños" />
          </View>
          <View style={styles.field}>
            <ThemedText style={styles.fieldLabel}>Descripción</ThemedText>
            <Input
              value={description}
              onChangeText={setDescription}
              placeholder="Decoración sencilla para la mesa"
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
              <ThemedText style={styles.secRowTitle}>Disponible</ThemedText>
              <ThemedText style={[styles.secRowSub, { color: mutedColor }]}>
                Si lo desactivas, deja de ofrecerse en nuevas conversaciones.
              </ThemedText>
            </View>
            <ToggleSwitch
              checked={isActive}
              onChange={setIsActive}
              primaryColor={primaryColor}
              borderColor={borderColor}
            />
          </View>
          {message ? <ThemedText style={styles.errorText}>{message}</ThemedText> : null}
          <View style={styles.rowWrap}>
            <Button onPress={handleSave} disabled={saving}>
              Guardar
            </Button>
            <Button
              size="secondary"
              onPress={() => setEditing(false)}
              style={{ backgroundColor: mutedColor }}
            >
              Cancelar
            </Button>
          </View>
        </>
      ) : (
        <>
          <View style={{ flexDirection: "row", justifyContent: "space-between", gap: 12 }}>
            <View style={{ flex: 1, gap: 4 }}>
              <ThemedText style={styles.secRowTitle}>{item.name}</ThemedText>
              <ThemedText style={[styles.secRowSub, { color: mutedColor }]}>
                {item.description?.trim() || "Sin descripción adicional."}
              </ThemedText>
              <ThemedText style={[styles.helperText, { color: mutedColor }]}>
                Estimado: COP {priceLabel}
              </ThemedText>
            </View>
            <View
              style={[
                styles.statusPill,
                { backgroundColor: item.isActive ? `${primaryColor}18` : `${mutedColor}18` },
              ]}
            >
              <ThemedText
                style={[
                  styles.statusPillText,
                  { color: item.isActive ? primaryColor : mutedColor },
                ]}
              >
                {item.isActive ? "Activo" : "Inactivo"}
              </ThemedText>
            </View>
          </View>
          {message ? <ThemedText style={styles.errorText}>{message}</ThemedText> : null}
          <View style={styles.rowWrap}>
            <Button onPress={() => setEditing(true)}>Editar</Button>
            <Pressable onPress={() => void onDelete()}>
              <ThemedText style={styles.deleteText}>Eliminar</ThemedText>
            </Pressable>
          </View>
        </>
      )}
    </View>
  );
}
