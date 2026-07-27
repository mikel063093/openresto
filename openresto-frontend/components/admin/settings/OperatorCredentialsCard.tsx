import { useEffect, useMemo, useState } from "react";
import { Platform, Pressable, View } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { ThemedText } from "@/components/themed-text";
import Input from "@/components/common/Input";
import Button from "@/components/common/Button";
import ConfirmModal from "@/components/common/ConfirmModal";
import { useAppTheme } from "@/hooks/use-app-theme";
import { usePersistedState } from "@/hooks/use-persisted-state";
import { AnimatedAccordion } from "@/components/common/AnimatedAccordion";
import {
  adminGetRestaurants,
  getOperatorCredentials,
  issueOperatorCredential,
  IssueOperatorCredentialResponse,
  OperatorCredentialListItem,
  revokeOperatorCredential,
} from "@/api/admin";
import { styles } from "./settings.styles";

type RestaurantOption = { id: number; name: string };

export function OperatorCredentialsCard({
  borderColor,
  mutedColor,
  cardBg,
}: {
  borderColor: string;
  mutedColor: string;
  cardBg: string;
}) {
  const { primaryColor } = useAppTheme();
  const [expanded, setExpanded] = usePersistedState("settings:operator-mcp:expanded", true);
  const [restaurants, setRestaurants] = useState<RestaurantOption[]>([]);
  const [credentials, setCredentials] = useState<OperatorCredentialListItem[]>([]);
  const [identifier, setIdentifier] = useState("");
  const [ttlHours, setTtlHours] = useState("8");
  const [notes, setNotes] = useState("");
  const [selectedRestaurantIds, setSelectedRestaurantIds] = useState<number[]>([]);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ text: string; ok: boolean } | null>(null);
  const [revealedCredential, setRevealedCredential] =
    useState<IssueOperatorCredentialResponse | null>(null);
  const [pendingRevoke, setPendingRevoke] = useState<OperatorCredentialListItem | null>(null);
  const [copied, setCopied] = useState(false);

  const clearRevealedCredential = () => {
    setRevealedCredential(null);
    setCopied(false);
  };

  const load = async () => {
    const [restaurantList, credentialList] = await Promise.all([
      adminGetRestaurants(),
      getOperatorCredentials(),
    ]);
    setRestaurants(restaurantList.map((item) => ({ id: item.id, name: item.name })));
    setCredentials(credentialList);
  };

  useEffect(() => {
    load().catch((error) => {
      setMessage({
        text: error instanceof Error ? error.message : "No fue posible cargar las credenciales.",
        ok: false,
      });
    });
  }, []);

  useEffect(() => {
    if (!expanded) {
      clearRevealedCredential();
    }
  }, [expanded]);

  useEffect(() => () => clearRevealedCredential(), []);

  const selectedSet = useMemo(() => new Set(selectedRestaurantIds), [selectedRestaurantIds]);

  const toggleRestaurant = (restaurantId: number) => {
    setSelectedRestaurantIds((current) =>
      current.includes(restaurantId)
        ? current.filter((value) => value !== restaurantId)
        : [...current, restaurantId]
    );
  };

  const handleIssue = async () => {
    setSaving(true);
    setMessage(null);
    setCopied(false);
    try {
      const created = await issueOperatorCredential({
        identifier: identifier.trim(),
        restaurantIds: selectedRestaurantIds,
        ttlHours: ttlHours.trim() ? Number(ttlHours) : undefined,
        notes: notes.trim() || undefined,
      });
      setRevealedCredential(created);
      setIdentifier("");
      setTtlHours("8");
      setNotes("");
      setSelectedRestaurantIds([]);
      await load();
      setMessage({ text: "Credencial emitida correctamente.", ok: true });
    } catch (error) {
      setMessage({
        text: error instanceof Error ? error.message : "No fue posible emitir la credencial.",
        ok: false,
      });
    } finally {
      setSaving(false);
    }
  };

  const handleCopy = async () => {
    if (!revealedCredential?.plaintextToken) return;

    if (
      Platform.OS === "web" &&
      typeof navigator !== "undefined" &&
      navigator.clipboard?.writeText
    ) {
      await navigator.clipboard.writeText(revealedCredential.plaintextToken);
      setCopied(true);
    }
  };

  const confirmRevoke = async () => {
    if (!pendingRevoke) return;

    setSaving(true);
    setMessage(null);
    try {
      await revokeOperatorCredential(pendingRevoke.credentialId);
      setPendingRevoke(null);
      await load();
      setMessage({ text: "Credencial revocada de inmediato.", ok: true });
    } catch (error) {
      setMessage({
        text: error instanceof Error ? error.message : "No fue posible revocar la credencial.",
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
        accessibilityLabel="Toggle operator credentials"
        style={styles.secHeader}
        onPress={() => setExpanded((value) => !value)}
      >
        <View style={[styles.secIcon, { backgroundColor: `${primaryColor}14` }]}>
          <Ionicons name="key-outline" size={20} color={primaryColor} />
        </View>
        <View style={{ flex: 1 }}>
          <ThemedText style={styles.secTitle}>Credenciales MCP internas</ThemedText>
          <ThemedText style={[styles.secSub, { color: mutedColor }]}>
            Emite, revisa y revoca accesos de operadores internos.
          </ThemedText>
        </View>
        <Ionicons name={expanded ? "chevron-up" : "chevron-down"} size={18} color={mutedColor} />
      </Pressable>

      <AnimatedAccordion expanded={expanded}>
        <View style={[styles.secForm, { borderTopColor: borderColor }]}>
          <View style={styles.field}>
            <ThemedText style={styles.fieldLabel}>Identificador del operador</ThemedText>
            <Input
              value={identifier}
              onChangeText={setIdentifier}
              placeholder="operador@interno"
              autoCapitalize="none"
              autoCorrect={false}
            />
          </View>

          <View style={styles.field}>
            <ThemedText style={styles.fieldLabel}>Restaurantes autorizados</ThemedText>
            <View style={styles.chipsRow}>
              {restaurants.map((restaurant) => {
                const selected = selectedSet.has(restaurant.id);
                return (
                  <Pressable
                    key={restaurant.id}
                    accessibilityRole="button"
                    onPress={() => toggleRestaurant(restaurant.id)}
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
            <ThemedText style={styles.fieldLabel}>Vigencia (horas)</ThemedText>
            <Input
              value={ttlHours}
              onChangeText={setTtlHours}
              placeholder="8"
              keyboardType="number-pad"
            />
            <ThemedText style={[styles.helperText, { color: mutedColor }]}>
              Valor por defecto: 8. Máximo: 24.
            </ThemedText>
          </View>

          <View style={styles.field}>
            <ThemedText style={styles.fieldLabel}>Notas</ThemedText>
            <Input
              value={notes}
              onChangeText={setNotes}
              placeholder="Notas opcionales"
              maxLength={200}
            />
          </View>

          {message && (
            <ThemedText style={message.ok ? styles.successText : styles.errorText}>
              {message.text}
            </ThemedText>
          )}

          <Button
            onPress={handleIssue}
            disabled={saving || !identifier.trim() || selectedRestaurantIds.length === 0}
          >
            {saving ? "Emitiendo…" : "Emitir credencial"}
          </Button>

          <ThemedText style={styles.secRowTitle}>Credenciales vigentes e históricas</ThemedText>

          {credentials.map((credential) => {
            const status = getCredentialStatus(credential);
            return (
              <View
                key={credential.credentialId}
                style={[styles.secRow, { borderTopColor: borderColor, paddingHorizontal: 0 }]}
              >
                <View style={[styles.stackedMeta, { flex: 1 }]}>
                  <View style={styles.rowWrap}>
                    <ThemedText style={styles.secRowTitle}>{credential.identifier}</ThemedText>
                    <View style={[styles.statusPill, { backgroundColor: status.background }]}>
                      <ThemedText style={[styles.statusPillText, { color: status.color }]}>
                        {status.label}
                      </ThemedText>
                    </View>
                  </View>
                  <ThemedText style={[styles.secRowSub, styles.monoValue, { color: mutedColor }]}>
                    key {credential.credentialKeyId}
                  </ThemedText>
                  <ThemedText style={[styles.secRowSub, { color: mutedColor }]}>
                    Restaurantes:{" "}
                    {credential.restaurants.map((item) => item.restaurantName).join(", ")}
                  </ThemedText>
                  <ThemedText style={[styles.secRowSub, { color: mutedColor }]}>
                    Emitida: {formatUtc(credential.issuedAtUtc)} · Expira:{" "}
                    {formatUtc(credential.expiresAtUtc)}
                  </ThemedText>
                  <ThemedText style={[styles.secRowSub, { color: mutedColor }]}>
                    Último uso:{" "}
                    {credential.lastUsedAtUtc ? formatUtc(credential.lastUsedAtUtc) : "Sin uso"}
                    {credential.revokedAtUtc
                      ? ` · Revocada: ${formatUtc(credential.revokedAtUtc)}`
                      : ""}
                  </ThemedText>
                  {credential.notes ? (
                    <ThemedText style={[styles.secRowSub, { color: mutedColor }]}>
                      Notas: {credential.notes}
                    </ThemedText>
                  ) : null}
                </View>
                {!credential.revokedAtUtc && (
                  <Pressable
                    accessibilityRole="button"
                    accessibilityLabel={`Revocar credencial ${credential.credentialKeyId}`}
                    style={styles.smallBtn}
                    disabled={saving}
                    onPress={() => setPendingRevoke(credential)}
                  >
                    <ThemedText style={styles.deleteText}>Revocar</ThemedText>
                  </Pressable>
                )}
              </View>
            );
          })}
        </View>
      </AnimatedAccordion>

      <ConfirmModal
        visible={!!revealedCredential}
        title="Guarda este token ahora"
        message={
          revealedCredential
            ? [
                "Solo se muestra una vez. Después solo verás metadatos.",
                "",
                revealedCredential.identifier,
                revealedCredential.plaintextToken,
                copied ? "Copiado al portapapeles." : "Cópialo antes de continuar.",
              ].join("\n")
            : ""
        }
        confirmLabel="Entendido"
        cancelLabel={copied ? "Copiar de nuevo" : "Copiar"}
        onConfirm={clearRevealedCredential}
        onCancel={() => {
          handleCopy().catch(() => {
            setCopied(false);
          });
        }}
      />

      <ConfirmModal
        visible={!!pendingRevoke}
        title="Revocar credencial"
        message={
          pendingRevoke
            ? `Se revocará de inmediato la credencial ${pendingRevoke.credentialKeyId} de ${pendingRevoke.identifier}.`
            : ""
        }
        confirmLabel="Revocar"
        cancelLabel="Cancelar"
        destructive
        onConfirm={confirmRevoke}
        onCancel={() => setPendingRevoke(null)}
      />
    </View>
  );
}

function getCredentialStatus(credential: OperatorCredentialListItem) {
  if (credential.revokedAtUtc) {
    return { label: "Revocada", color: "#991b1b", background: "#fee2e2" };
  }

  if (new Date(credential.expiresAtUtc).getTime() <= Date.now()) {
    return { label: "Expirada", color: "#92400e", background: "#fef3c7" };
  }

  return { label: "Activa", color: "#166534", background: "#dcfce7" };
}

function formatUtc(value: string) {
  return new Date(value).toLocaleString("es-CO", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    timeZone: "UTC",
  });
}
