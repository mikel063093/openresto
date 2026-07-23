import React, { useEffect, useState } from "react";
import { Platform, StyleSheet, useWindowDimensions, View } from "react-native";
import { Slot, Stack, useRouter, usePathname, useSegments } from "expo-router";
import { ThemedView } from "@/components/themed-view";
import { ThemedText } from "@/components/themed-text";
import { checkSession } from "@/api/auth";
import AdminSidebar from "@/components/layout/AdminSidebar";
import { Ionicons } from "@expo/vector-icons";
import { theme } from "@/theme/theme";
import { useBrand } from "@/context/BrandContext";
import PageLoader from "@/components/common/PageLoader";
import { useKeyboardShortcuts } from "@/hooks/useKeyboardShortcuts";
import { focusTarget } from "@/utils/focusRegistry";
import KeyboardShortcutsHelp from "@/components/common/KeyboardShortcutsHelp";
import { useI18n } from "@/context/I18nContext";

const MIN_WIDTH = 600;

function DesktopOnlyWall() {
  const { t } = useI18n();
  return (
    <ThemedView style={styles.wall}>
      <Ionicons name="desktop-outline" size={48} color={theme.colors.primary} />
      <ThemedText style={styles.wallTitle}>{t("admin.desktopOnlyTitle")}</ThemedText>
      <ThemedText style={styles.wallBody}>{t("admin.desktopOnlyBody")}</ThemedText>
    </ThemedView>
  );
}

export default function AdminLayout() {
  const { width } = useWindowDimensions();
  const showWall = Platform.OS === "web" && width < MIN_WIDTH;

  // AdminLayoutInner stays mounted even when the wall is shown so that auth
  // state is preserved — unmounting it would reset authState to "loading" and
  // trigger an unnecessary session re-check (which can bounce the user to login).
  return (
    <View style={{ flex: 1 }}>
      {showWall && <DesktopOnlyWall />}
      <View style={[{ flex: 1 }, showWall && { display: "none" as const }]}>
        <AdminLayoutInner />
      </View>
    </View>
  );
}

function AdminLayoutInner() {
  const router = useRouter();
  const pathname = usePathname();
  const segments = useSegments();
  // useSegments() reflects the true current URL (a plain state read, no focus-
  // event lifecycle), unlike useIsFocused() which never fires on a cold web
  // load (page.goto() straight to a route never dispatches a "focus" nav
  // event) and would leave shortcutsEnabled stuck false. This also still
  // guards against the original scope-leak bug: once the user navigates to a
  // (user) route, segments[0] flips away from "admin" even though this
  // layout instance may remain mounted (React Navigation doesn't unmount
  // sibling stack screens on navigation).
  const isAdminRouteActive = segments[0] === "admin";
  const brand = useBrand();
  const { t } = useI18n();
  const [authState, setAuthState] = useState<"loading" | "authenticated" | "unauthenticated">(
    "loading"
  );

  useEffect(() => {
    /* istanbul ignore next */
    if (Platform.OS !== "web") return;

    /* istanbul ignore next */
    const PAGE_TITLES: Record<string, string> = {
      "/admin/dashboard": t("admin.dashboardTitle"),
      "/admin/settings": t("admin.settings"),
      "/admin/bookings": t("admin.bookings"),
      "/admin/bookings/new": t("admin.newWalkInTitle"),
      "/admin/locations": t("admin.locations"),
      "/admin/login": t("admin.loginTitle"),
    };
    /* istanbul ignore next */
    const title =
      PAGE_TITLES[pathname] ??
      (/^\/admin\/bookings\/\d+$/.test(pathname) ? t("admin.bookingDetailTitle") : undefined);
    /* istanbul ignore next */
    if (title) document.title = `${title} | ${brand.appName}`;
  }, [pathname, brand.appName, t]);

  useEffect(() => {
    const onLoginScreen = pathname === "/admin/login";
    if (onLoginScreen) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setAuthState("authenticated");
      return;
    }

    if (authState === "authenticated") return;

    checkSession().then((session) => {
      if (session === "rate-limited") {
        // We're being rate limited, but we likely have a session.
        // Don't log out, just assume we're still authenticated.
        setAuthState("authenticated");
      } else if (session) {
        setAuthState("authenticated");
      } else {
        setAuthState("unauthenticated");
        router.replace("/admin/login");
      }
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pathname]);

  const [showShortcutsHelp, setShowShortcutsHelp] = useState(false);
  const shortcutsEnabled =
    authState === "authenticated" && pathname !== "/admin/login" && isAdminRouteActive;

  useKeyboardShortcuts(
    shortcutsEnabled
      ? {
          "g d": () => router.push("/admin/dashboard"),
          "g b": () => router.push("/admin/bookings"),
          "g l": () => router.push("/admin/locations"),
          "g s": () => router.push("/admin/settings"),
          "/": (e) => {
            e.preventDefault();
            focusTarget("admin-lookup");
          },
          c: () => router.push({ pathname: "/admin/bookings", params: { create: "1" } }),
          "?": () => setShowShortcutsHelp((v) => !v),
        }
      : {}
  );

  if (authState === "loading") return <PageLoader />;

  /* istanbul ignore next */
  if (Platform.OS === "web") {
    const onLoginScreen = pathname === "/admin/login";
    if (onLoginScreen) {
      return (
        <ThemedView style={{ flex: 1 }}>
          <Slot />
        </ThemedView>
      );
    }

    if (authState !== "authenticated") return null;

    return (
      <ThemedView style={{ flex: 1, flexDirection: "row" }}>
        <AdminSidebar />
        <View style={{ flex: 1 }}>
          <Slot />
        </View>
        <KeyboardShortcutsHelp
          visible={showShortcutsHelp}
          scope="admin"
          onClose={() => setShowShortcutsHelp(false)}
        />
      </ThemedView>
    );
  }

  if (authState !== "authenticated") {
    const onLoginScreen = pathname === "/admin/login";
    /* istanbul ignore else */
    if (!onLoginScreen) return null;
  }

  return (
    <Stack>
      <Stack.Screen
        name="login"
        options={{ title: t("admin.loginTitle"), headerBackVisible: false }}
      />
      <Stack.Screen
        name="dashboard"
        options={{ title: t("admin.dashboardTitle"), headerBackVisible: false }}
      />
      <Stack.Screen name="bookings/index" options={{ title: t("admin.bookings") }} />
      <Stack.Screen
        name="bookings/[id]"
        options={{ title: t("admin.bookingDetailTitle") }}
      />
      <Stack.Screen name="locations" options={{ title: t("admin.locations") }} />
      <Stack.Screen name="settings" options={{ title: t("admin.settings") }} />
    </Stack>
  );
}

const styles = StyleSheet.create({
  wall: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
    gap: 16,
    padding: 32,
  },
  wallTitle: {
    fontSize: 22,
    fontWeight: "800",
    letterSpacing: -0.5,
  },
  wallBody: {
    fontSize: 15,
    textAlign: "center",
    opacity: 0.6,
    lineHeight: 24,
  },
});
