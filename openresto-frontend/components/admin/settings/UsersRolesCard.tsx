import { useEffect, useState } from "react";
import { View, Pressable } from "react-native";
import { Ionicons } from "@expo/vector-icons";
import { ThemedText } from "@/components/themed-text";
import Input from "@/components/common/Input";
import Button from "@/components/common/Button";
import { useAppTheme } from "@/hooks/use-app-theme";
import {
  AdminRole,
  AdminUser,
  createAdminUser,
  deactivateAdminUser,
  getAdminUsers,
  updateAdminUser,
} from "@/api/admin";
import { AnimatedAccordion } from "@/components/common/AnimatedAccordion";
import { styles } from "./settings.styles";

export function UsersRolesCard({
  borderColor,
  mutedColor,
  cardBg,
}: {
  borderColor: string;
  mutedColor: string;
  cardBg: string;
}) {
  const { primaryColor } = useAppTheme();
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [expanded, setExpanded] = useState(false);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState<AdminRole>("BookingViewer");
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const load = () => getAdminUsers().then(setUsers);
  useEffect(() => {
    void load();
  }, []);
  const create = async () => {
    setSaving(true);
    setMessage(null);
    try {
      await createAdminUser({ email: email.trim(), password, role });
      setEmail("");
      setPassword("");
      await load();
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Unable to create user.");
    } finally {
      setSaving(false);
    }
  };
  const deactivate = async (id: number) => {
    setSaving(true);
    setMessage(null);
    try {
      await deactivateAdminUser(id);
      await load();
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Unable to deactivate user.");
    } finally {
      setSaving(false);
    }
  };
  const cycleRole = async (user: AdminUser) => {
    const roles: AdminRole[] = ["SuperAdmin", "BookingViewer", "BookingEditor"];
    const next = roles[(roles.indexOf(user.role) + 1) % roles.length];
    setSaving(true);
    setMessage(null);
    try {
      await updateAdminUser(user.id, { role: next });
      await load();
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Unable to update role.");
    } finally {
      setSaving(false);
    }
  };
  return (
    <View style={[styles.secCard, { backgroundColor: cardBg, borderColor }]}>
      <Pressable
        accessibilityRole="button"
        accessibilityLabel="Toggle users and roles"
        style={styles.secHeader}
        onPress={() => setExpanded((value) => !value)}
      >
        <View style={[styles.secIcon, { backgroundColor: `${primaryColor}14` }]}>
          <Ionicons name="people-outline" size={20} color={primaryColor} />
        </View>
        <View style={{ flex: 1 }}>
          <ThemedText style={styles.secTitle}>Users & Roles</ThemedText>
          <ThemedText style={[styles.secSub, { color: mutedColor }]}>
            Manage administrator access and booking permissions
          </ThemedText>
        </View>
        <Ionicons name={expanded ? "chevron-up" : "chevron-down"} size={18} color={mutedColor} />
      </Pressable>
      <AnimatedAccordion expanded={expanded}>
        <View style={[styles.secForm, { borderTopColor: borderColor }]}>
          {users.map((user) => (
            <View
              key={user.id}
              style={[
                styles.secRow,
                { borderTopColor: borderColor, paddingHorizontal: 0, paddingVertical: 10 },
              ]}
            >
              <View style={{ flex: 1 }}>
                <ThemedText style={styles.secRowTitle}>{user.email}</ThemedText>
                <ThemedText style={[styles.secRowSub, { color: mutedColor }]}>
                  {user.isActive ? "Active" : "Inactive"} · {user.role}
                </ThemedText>
              </View>
              {user.isActive && (
                <Pressable
                  accessibilityRole="button"
                  accessibilityLabel={`Change role for ${user.email}`}
                  style={[styles.secBtn, { borderColor }]}
                  onPress={() => cycleRole(user)}
                  disabled={saving}
                >
                  <ThemedText style={[styles.secBtnText, { color: primaryColor }]}>Role</ThemedText>
                </Pressable>
              )}
              {user.isActive && (
                <Pressable
                  accessibilityRole="button"
                  accessibilityLabel={`Deactivate ${user.email}`}
                  style={styles.smallBtn}
                  onPress={() => deactivate(user.id)}
                  disabled={saving}
                >
                  <ThemedText style={styles.deleteText}>Deactivate</ThemedText>
                </Pressable>
              )}
            </View>
          ))}
          <ThemedText style={styles.secRowTitle}>Add administrator</ThemedText>
          <Input
            value={email}
            onChangeText={setEmail}
            placeholder="email@example.com"
            autoCapitalize="none"
            keyboardType="email-address"
          />
          <Input
            value={password}
            onChangeText={setPassword}
            placeholder="Temporary password (6+ characters)"
            secureTextEntry
          />
          <View style={styles.roleRow}>
            {(["SuperAdmin", "BookingViewer", "BookingEditor"] as AdminRole[]).map((value) => (
              <Pressable
                key={value}
                accessibilityRole="radio"
                accessibilityState={{ selected: role === value }}
                onPress={() => setRole(value)}
                style={[
                  styles.roleChip,
                  {
                    borderColor,
                    backgroundColor: role === value ? `${primaryColor}18` : "transparent",
                  },
                ]}
              >
                <ThemedText
                  style={[styles.secBtnText, { color: role === value ? primaryColor : mutedColor }]}
                >
                  {value.replace("Booking", "Booking ")}
                </ThemedText>
              </Pressable>
            ))}
          </View>
          {message && <ThemedText style={styles.errorText}>{message}</ThemedText>}
          <Button onPress={create} disabled={saving || !email || password.length < 6}>
            {saving ? "Saving…" : "Add user"}
          </Button>
        </View>
      </AnimatedAccordion>
    </View>
  );
}
