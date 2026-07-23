import { Stack } from "expo-router";
import { useI18n } from "@/context/I18nContext";
import LocationsScreen from "@/components/restaurant/LocationsScreen";

export default function LocationsIndexScreen() {
  const { t } = useI18n();

  return (
    <>
      <Stack.Screen options={{ title: t("navigation.locations") }} />
      <LocationsScreen />
    </>
  );
}
