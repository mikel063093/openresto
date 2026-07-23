import { theme } from "@/theme/theme";
import type { NotificationType } from "@/api/notifications";
import { type Locale, toIntlLocale } from "@/i18n/locale";
import type { MessageKey } from "@/i18n/messages";

/** Decodes a VAPID public key (base64url) into a Uint8Array for PushManager.subscribe. */
export function urlBase64ToUint8Array(base64String: string): Uint8Array<ArrayBuffer> {
  const padding = "=".repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/-/g, "+").replace(/_/g, "/");
  const rawData = window.atob(base64);
  const output = new Uint8Array(rawData.length);
  for (let i = 0; i < rawData.length; i++) output[i] = rawData.charCodeAt(i);
  return output;
}

/** Encodes an ArrayBuffer to a base64 string (for push subscription keys). */
export function arrayBufferToBase64(buffer: ArrayBuffer): string {
  return btoa(String.fromCharCode(...new Uint8Array(buffer)));
}

/** Compact relative timestamp: "just now", "5m ago", "3h ago", "2d ago". */
export function relativeTime(
  iso: string,
  locale: Locale = "en",
  t?: (key: MessageKey, values?: Record<string, string | number>) => string
): string {
  const diff = Date.now() - new Date(iso).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return t ? t("admin.relativeTime.justNow") : "just now";
  if (mins < 60) return t ? t("admin.relativeTime.minutesAgo", { count: mins }) : `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return t ? t("admin.relativeTime.hoursAgo", { count: hrs }) : `${hrs}h ago`;
  const days = Math.floor(hrs / 24);
  return t ? t("admin.relativeTime.daysAgo", { count: days }) : `${days}d ago`;
}

/** Locale-aware booking date for notification meta lines. */
export function formatBookingDate(iso: string, locale: Locale = "en"): string {
  return new Date(iso).toLocaleString(toIntlLocale(locale), {
    weekday: "short",
    day: "numeric",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export const PAGE_SIZE = 20;
export const PIN_STORAGE_KEY = "openresto_pinned_notifs";

export const TYPE_LABELS: Record<NotificationType, MessageKey> = {
  BookingCreated: "admin.notificationType.bookingCreated",
  BookingCancelled: "admin.notificationType.bookingCancelled",
  RestaurantNearlyFull: "admin.notificationType.restaurantNearlyFull",
};

type TypeIcon = {
  name: "checkmark-circle-outline" | "close-circle-outline" | "warning-outline";
  color: string;
};

export const TYPE_ICONS: Record<NotificationType, TypeIcon> = {
  BookingCreated: { name: "checkmark-circle-outline", color: theme.colors.success },
  BookingCancelled: { name: "close-circle-outline", color: theme.colors.error },
  RestaurantNearlyFull: { name: "warning-outline", color: theme.colors.warning },
};

export const TYPE_FILTERS = [
  { labelKey: "admin.notificationFilter.allTypes", value: "" },
  { labelKey: "admin.notificationFilter.newBookings", value: "BookingCreated" },
  { labelKey: "admin.notificationFilter.cancelled", value: "BookingCancelled" },
  { labelKey: "admin.notificationFilter.nearlyFull", value: "RestaurantNearlyFull" },
];
