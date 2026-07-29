import type { Locale } from "@/i18n/locale";

const en = {
  "language.label": "Language",
  "language.english": "English",
  "language.spanish": "Español",
  "language.switchToEnglish": "Switch language to English",
  "language.switchToSpanish": "Switch language to Spanish",
  "navigation.locations": "Locations",
  "navigation.myBookings": "My Bookings",
  "navigation.goBack": "Go back",
  "navigation.openMenu": "Open menu",
  "navigation.help": "Help",
  "navigation.keyboardShortcuts": "Keyboard shortcuts",
  "navigation.lightMode": "Switch to light mode",
  "navigation.darkMode": "Switch to dark mode",
  "footer.allRightsReserved": "© {year} {appName}. All rights reserved.",
  "footer.restaurantAdmin": "Restaurant admin",
  "common.close": "Close",
  "common.search": "Search",
  "common.loading": "Loading…",
  "common.cancel": "Cancel",
  "common.save": "Save",
  "home.defaultSubtitle":
    "Choose a location, select a time, enter your email address, and you are all set.",
  "home.highlights": "Restaurant highlights",
  "home.curatedByOwner": "Curated by the owner",
  "home.ourLocations": "Our locations",
  "admin.overview": "Overview",
  "admin.bookings": "Bookings",
  "admin.locations": "Locations",
  "admin.notifications": "Notifications",
  "admin.settings": "Settings",
  "admin.panel": "Admin Panel",
  "admin.managingLocations": "Managing {count} {count, plural, one {location} other {locations}}",
  "admin.lookupBooking": "Lookup booking",
  "admin.emailOrReference": "Email or reference…",
  "admin.noBookingFound": "No booking found.",
} as const;

const es: Record<keyof typeof en, string> = {
  "language.label": "Idioma",
  "language.english": "English",
  "language.spanish": "Español",
  "language.switchToEnglish": "Cambiar idioma a English",
  "language.switchToSpanish": "Cambiar idioma a Español",
  "navigation.locations": "Ubicaciones",
  "navigation.myBookings": "Mis reservas",
  "navigation.goBack": "Volver",
  "navigation.openMenu": "Abrir menú",
  "navigation.help": "Ayuda",
  "navigation.keyboardShortcuts": "Atajos de teclado",
  "navigation.lightMode": "Cambiar al modo claro",
  "navigation.darkMode": "Cambiar al modo oscuro",
  "footer.allRightsReserved": "© {year} {appName}. Todos los derechos reservados.",
  "footer.restaurantAdmin": "Administración del restaurante",
  "common.close": "Cerrar",
  "common.search": "Buscar",
  "common.loading": "Cargando…",
  "common.cancel": "Cancelar",
  "common.save": "Guardar",
  "home.defaultSubtitle":
    "Elige una ubicación, selecciona una hora, ingresa tu correo electrónico y listo.",
  "home.highlights": "Lo mejor del restaurante",
  "home.curatedByOwner": "Selección del restaurante",
  "home.ourLocations": "Nuestras ubicaciones",
  "admin.overview": "Resumen",
  "admin.bookings": "Reservas",
  "admin.locations": "Ubicaciones",
  "admin.notifications": "Notificaciones",
  "admin.settings": "Configuración",
  "admin.panel": "Panel de administración",
  "admin.managingLocations":
    "Administras {count} {count, plural, one {ubicación} other {ubicaciones}}",
  "admin.lookupBooking": "Buscar una reserva",
  "admin.emailOrReference": "Correo o referencia…",
  "admin.noBookingFound": "No se encontró ninguna reserva.",
};

export type MessageKey = keyof typeof en;
export type MessageValues = Record<string, string | number>;

const catalogs: Record<Locale, Record<MessageKey, string>> = { en, es };

function pluralize(template: string, values: MessageValues): string {
  return template.replace(
    /\{(\w+), plural, one \{([^{}]*)\} other \{([^{}]*)\}\}/g,
    (_match, name: string, one: string, other: string) => (Number(values[name]) === 1 ? one : other)
  );
}

export function translate(locale: Locale, key: MessageKey, values: MessageValues = {}): string {
  const template = catalogs[locale][key] ?? catalogs.en[key];
  return pluralize(template, values).replace(/\{(\w+)\}/g, (_match, name: string) =>
    String(values[name] ?? `{${name}}`)
  );
}
