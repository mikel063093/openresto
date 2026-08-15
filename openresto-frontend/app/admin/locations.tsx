import { useEffect, useState } from "react";
import { ActivityIndicator, ScrollView, View, Platform, Pressable } from "react-native";
import { Stack } from "expo-router";
import { ThemedText } from "@/components/themed-text";
import { ThemedView } from "@/components/themed-view";
import { Ionicons } from "@expo/vector-icons";
import ConfirmModal from "@/components/common/ConfirmModal";
import { theme } from "@/theme/theme";
import { fetchRestaurants, createRestaurant, RestaurantDto } from "@/api/restaurants";
import {
  adminGetRestaurants,
  pauseRestaurantBookings,
  unpauseRestaurantBookings,
  extendRestaurantBookings,
  BookingDetailDto,
} from "@/api/admin";
import { useAppTheme } from "@/hooks/use-app-theme";
import { usePersistedState } from "@/hooks/use-persisted-state";
import { useConfirm } from "@/hooks/use-confirm";

import { LocationCard } from "@/components/admin/settings/LocationCard";
import { AddLocationForm } from "@/components/admin/locations/AddLocationForm";
import { DangerZone } from "@/components/admin/locations/DangerZone";
import { styles } from "@/components/admin/settings/settings.styles";
import { useI18n } from "@/context/I18nContext";
import { fmtDateTime } from "@/utils/formatters";

export default function AdminLocationsScreen() {
  const [restaurants, setRestaurants] = useState<RestaurantDto[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [persistedSelectedId, setPersistedSelectedId] = usePersistedState<number | null>(
    "locations:selectedId",
    null
  );
  const [loading, setLoading] = useState(true);
  const [addingLocation, setAddingLocation] = useState(false);
  const [newLocationName, setNewLocationName] = useState("");
  const [savingLocation, setSavingLocation] = useState(false);
  const [allRestaurants, setAllRestaurants] = useState<
    {
      id: number;
      name: string;
      isArchived?: boolean;
      bookingsPausedUntil?: string;
      activeBookingsCount?: number;
    }[]
  >([]);
  const [pausing, setPausing] = useState(false);
  const [extending, setExtending] = useState(false);
  const [extendedBookings, setExtendedBookings] = useState<BookingDetailDto[] | null>(null);
  const [extendNoActive, setExtendNoActive] = useState(false);
  const { state: confirmState, confirm: confirmAction, handleConfirm, handleCancel } = useConfirm();

  const { colors, isDark, primaryColor } = useAppTheme();
  const { locale, t } = useI18n();
  const borderColor = colors.border;
  const cardBg = colors.card;
  const mutedColor = colors.muted;

  function patchRestaurant(id: number, patch: Partial<RestaurantDto>) {
    setRestaurants((prev) => prev.map((r) => (r.id === id ? { ...r, ...patch } : r)));
  }

  useEffect(() => {
    let cancelled = false;
    Promise.all([fetchRestaurants(), adminGetRestaurants()]).then(([active, all]) => {
      if (cancelled) return;
      setRestaurants(active);
      const persistedMatch =
        persistedSelectedId != null ? active.find((r) => r.id === persistedSelectedId) : undefined;
      const nextId = persistedMatch ? persistedMatch.id : (active[0]?.id ?? null);
      if (nextId !== null) setSelectedId(nextId);
      setPersistedSelectedId(nextId);
      setAllRestaurants(all);
      setLoading(false);
    });
    /* istanbul ignore next */
    return () => {
      cancelled = true;
    };
    // persistedSelectedId seeds the initial selection only; omitting it avoids a refetch loop.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const selectedRestaurant = restaurants.find((r) => r.id === selectedId) ?? null;
  const selectedAdminData = allRestaurants.find((r) => r.id === selectedId) ?? null;
  const activeCount = selectedAdminData?.activeBookingsCount ?? 0;
  const isPaused = selectedAdminData?.bookingsPausedUntil
    ? new Date(selectedAdminData.bookingsPausedUntil) > new Date()
    : false;
  const pausedUntilText =
    isPaused && selectedAdminData?.bookingsPausedUntil
      ? fmtDateTime(new Date(selectedAdminData.bookingsPausedUntil), locale, {
          hour: "2-digit",
          minute: "2-digit",
        })
      : null;
  // Keeps the transient selectedId and its persisted counterpart in sync so the
  // next visit lands on the restaurant the admin was last viewing.
  const selectLocation = (id: number | null) => {
    setSelectedId(id);
    setPersistedSelectedId(id);
  };

  function handleSelectLocation(id: number) {
    selectLocation(id);
    setExtendedBookings(null);
    setExtendNoActive(false);
  }

  if (loading) {
    return (
      <ThemedView style={styles.center}>
        <ActivityIndicator size="large" color={primaryColor} />
      </ThemedView>
    );
  }

  return (
    <ScrollView contentContainerStyle={styles.container}>
      {Platform.OS !== "web" && <Stack.Screen options={{ title: t("admin.locationsTitle") }} />}

      {/* ── Page header ─────────────────────────────────────────────── */}
      <View style={styles.pageHeader}>
        <View style={{ gap: 2 }}>
          <ThemedText type="h1">{t("admin.locationsTitle")}</ThemedText>
          <ThemedText style={[styles.pageSub, { color: mutedColor }]}>
            {restaurants.length === 0
              ? t("admin.locationsSummaryNone")
              : t("admin.locationsSummaryActive", { count: restaurants.length })}
          </ThemedText>
        </View>
        <Pressable
          onPress={() => setAddingLocation(true)}
          disabled={addingLocation}
          style={{
            flexDirection: "row",
            alignItems: "center",
            gap: 7,
            paddingHorizontal: 14,
            paddingVertical: 10,
            borderRadius: theme.borderRadius.md,
            backgroundColor: primaryColor,
            minHeight: 44,
            opacity: addingLocation ? 0.5 : 1,
          }}
        >
          <Ionicons name="add" size={16} color="#fff" />
          <ThemedText style={{ fontSize: 14, fontWeight: "600", color: "#fff" }}>
            {t("admin.addLocation")}
          </ThemedText>
        </Pressable>
      </View>

      {/* ── Add location form ────────────────────────────────────────── */}
      {addingLocation && (
        <AddLocationForm
          value={newLocationName}
          saving={savingLocation}
          isDark={isDark}
          mutedColor={mutedColor}
          primaryColor={primaryColor}
          onValueChange={setNewLocationName}
          onSubmit={async () => {
            if (!newLocationName.trim()) return;
            setSavingLocation(true);
            const created = await createRestaurant(newLocationName.trim());
            setSavingLocation(false);
            if (created) {
              setRestaurants((prev) => [...prev, { ...created, sections: [] }]);
              selectLocation(created.id);
            }
            setNewLocationName("");
            setAddingLocation(false);
          }}
          onCancel={() => {
            setAddingLocation(false);
            setNewLocationName("");
          }}
        />
      )}

      {/* ── Location selector pills ──────────────────────────────────── */}
      {restaurants.length > 0 && (
        <ScrollView
          horizontal
          showsHorizontalScrollIndicator={false}
          contentContainerStyle={{ flexDirection: "row", gap: 6, alignItems: "center" }}
        >
          {restaurants.map((r) => {
            const active = selectedId === r.id;
            return (
              <Pressable
                key={r.id}
                onPress={() => handleSelectLocation(r.id)}
                style={{
                  flexDirection: "row",
                  alignItems: "center",
                  gap: 7,
                  paddingHorizontal: 12,
                  paddingVertical: 6,
                  borderRadius: 9999,
                  borderWidth: 1,
                  borderColor: active ? primaryColor : borderColor,
                  backgroundColor: active ? primaryColor : "transparent",
                }}
              >
                <ThemedText
                  style={{
                    fontSize: 13,
                    fontWeight: "600",
                    color: active ? "#fff" : mutedColor,
                  }}
                >
                  {r.name}
                </ThemedText>
              </Pressable>
            );
          })}
        </ScrollView>
      )}

      {/* ── Booking action buttons ──────────────────────────────────── */}
      {selectedRestaurant && (
        <View style={{ flexDirection: "row", gap: 8 }}>
          <Pressable
            disabled={pausing}
            onPress={async () => {
              setPausing(true);
              if (isPaused) {
                await unpauseRestaurantBookings(selectedRestaurant.id);
                setAllRestaurants((prev) =>
                  prev.map((r) =>
                    r.id === selectedRestaurant.id ? { ...r, bookingsPausedUntil: undefined } : r
                  )
                );
              } else {
                await pauseRestaurantBookings(selectedRestaurant.id, 60);
                setAllRestaurants((prev) =>
                  prev.map((r) =>
                    r.id === selectedRestaurant.id
                      ? {
                          ...r,
                          bookingsPausedUntil: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
                        }
                      : r
                  )
                );
              }
              setPausing(false);
            }}
            style={{
              flex: 1,
              flexDirection: "row",
              alignItems: "center",
              justifyContent: "center",
              gap: 6,
              paddingVertical: 10,
              borderRadius: theme.borderRadius.md,
              borderWidth: 1,
              borderColor: isPaused ? "#16a34a" : "#ca8a04",
              backgroundColor: isPaused ? "rgba(22,163,74,0.08)" : "rgba(234,179,8,0.08)",
              opacity: pausing ? 0.5 : 1,
              minHeight: 44,
            }}
          >
            <Ionicons
              name={isPaused ? "play-circle-outline" : "pause-circle-outline"}
              size={15}
              color={isPaused ? "#16a34a" : "#ca8a04"}
            />
            <ThemedText
              style={{ fontSize: 13, fontWeight: "600", color: isPaused ? "#16a34a" : "#ca8a04" }}
            >
              {pausing
                ? t("admin.pauseSaving")
                : isPaused
                  ? t("admin.resumeNewBookingsNow", { time: pausedUntilText ?? "" })
                  : t("admin.pauseNewBookingsFor", { minutes: 60 })}
            </ThemedText>
          </Pressable>

          <Pressable
            disabled={extending || extendedBookings !== null || extendNoActive || activeCount === 0}
            onPress={async () => {
              setExtending(true);
              const result = await extendRestaurantBookings(selectedRestaurant.id, 60);
              setExtending(false);
              if (result.ok) {
                if (result.extendedBookings.length > 0) {
                  setExtendedBookings(result.extendedBookings);
                } else {
                  setExtendNoActive(true);
                }
              }
            }}
            style={{
              flex: 1,
              flexDirection: "row",
              alignItems: "center",
              justifyContent: "center",
              gap: 6,
              paddingVertical: 10,
              borderRadius: theme.borderRadius.md,
              borderWidth: 1,
              borderColor: borderColor,
              opacity:
                extending || extendedBookings !== null || extendNoActive || activeCount === 0
                  ? 0.4
                  : 1,
              minHeight: 44,
            }}
          >
            <Ionicons
              name="timer-outline"
              size={15}
              color={
                extendedBookings !== null || extendNoActive || activeCount === 0
                  ? mutedColor
                  : primaryColor
              }
            />
            <ThemedText
              style={{
                fontSize: 13,
                fontWeight: "600",
                color: extendedBookings !== null || extendNoActive ? mutedColor : primaryColor,
              }}
            >
              {extending
                ? t("admin.extending")
                : extendedBookings !== null
                  ? t("admin.extendedActiveBookings", {
                      count: extendedBookings.length,
                      minutes: 60,
                    })
                  : extendNoActive
                    ? t("admin.noActiveBookingsToExtend")
                    : activeCount > 0
                      ? t("admin.extendActiveBookingsBy", {
                          count: activeCount,
                          minutes: 60,
                        })
                      : t("admin.noActiveBookings")}
            </ThemedText>
          </Pressable>
        </View>
      )}

      {/* ── Location detail or empty state ───────────────────────────── */}
      {restaurants.length === 0 ? (
        <View
          style={{
            alignItems: "center",
            justifyContent: "center",
            paddingVertical: 72,
            gap: 12,
          }}
        >
          <View
            style={{
              width: 64,
              height: 64,
              borderRadius: 32,
              borderWidth: 1,
              borderColor,
              alignItems: "center",
              justifyContent: "center",
              marginBottom: 4,
            }}
          >
            <Ionicons name="storefront-outline" size={28} color={mutedColor} />
          </View>
          <ThemedText style={{ fontSize: 16, fontWeight: "700", textAlign: "center" }}>
            {t("admin.noLocationsYet")}
          </ThemedText>
          <ThemedText
            style={{
              fontSize: 14,
              color: mutedColor,
              textAlign: "center",
              maxWidth: 280,
              lineHeight: 22,
            }}
          >
            {t("admin.firstLocationBody")}
          </ThemedText>
        </View>
      ) : selectedRestaurant ? (
        <View style={[styles.secCard, { backgroundColor: cardBg, borderColor }]}>
          <LocationCard
            key={selectedRestaurant.id}
            restaurant={selectedRestaurant}
            onSaved={(patch) => patchRestaurant(selectedRestaurant.id, patch)}
            isDark={isDark}
            borderColor={borderColor}
            mutedColor={mutedColor}
            confirmAction={confirmAction}
          />
        </View>
      ) : null}

      {/* ── Archive / Delete (decomposed into <DangerZone/>) ───────── */}
      <DangerZone
        restaurants={allRestaurants}
        borderColor={borderColor}
        cardBg={cardBg}
        mutedColor={mutedColor}
        isDark={isDark}
        onArchived={async (id, archived) => {
          // Patch the all-restaurants list in place (faithful to the original).
          setAllRestaurants((prev) =>
            prev.map((r) => (r.id === id ? { ...r, isArchived: archived } : r))
          );
          if (archived) {
            // Archiving: drop from active list; relocate selection if it was the deleted one.
            setRestaurants((prev) => {
              const remaining = prev.filter((r) => r.id !== id);
              if (selectedId === id) {
                selectLocation(remaining.length > 0 ? remaining[0].id : null);
              }
              return remaining;
            });
          } else {
            // Restoring: re-fetch active list + select the restored restaurant.
            fetchRestaurants().then((active) => {
              setRestaurants(active);
              selectLocation(id);
            });
          }
        }}
        onDeleted={async (id) => {
          setAllRestaurants((prev) => prev.filter((r) => r.id !== id));
          setRestaurants((prev) => {
            const remaining = prev.filter((r) => r.id !== id);
            selectLocation(remaining.length > 0 ? remaining[0].id : null);
            return remaining;
          });
        }}
      />

      <ConfirmModal
        visible={!!confirmState}
        title={t("common.confirm")}
        message={confirmState?.message ?? ""}
        confirmLabel={t("common.delete")}
        cancelLabel={t("common.cancel")}
        destructive
        onConfirm={handleConfirm}
        onCancel={handleCancel}
      />
    </ScrollView>
  );
}
