import { useMemo } from "react";
import { useProfileQuery } from "@/entities/profile/model/use-profile-query";
import { SettingsProfileSection } from "@/features/profile/ui/profile-sections";
import { Alert } from "@/shared/ui/alert";
import { AppLayout } from "@/widgets/app-layout/ui/app-layout";

export function SettingsProfilePage() {
  const profileQuery = useProfileQuery();

  const isIncomplete = useMemo(() => {
    const profile = profileQuery.data;
    if (!profile) {
      return false;
    }

    const role = String(profile.role ?? "").toLowerCase();
    const hasName = Boolean(String(profile.name ?? "").trim());

    if (!hasName) {
      return true;
    }

    if (role === "driver") {
      return !(
        String(profile.carInfo?.brand ?? "").trim() &&
        String(profile.carInfo?.model ?? "").trim() &&
        String(profile.carInfo?.color ?? "").trim() &&
        String(profile.carInfo?.plateNumber ?? "").trim()
      );
    }

    return !(
      String(profile.address?.city ?? "").trim() &&
      String(profile.address?.street ?? "").trim() &&
      String(profile.address?.house ?? "").trim()
    );
  }, [profileQuery.data]);

  return (
    <AppLayout>
      {isIncomplete && (
        <Alert className="mb-4 border-amber-500/50 text-amber-700">
          Пожалуйста, заполните профиль полностью, чтобы пользоваться всеми
          разделами приложения.
        </Alert>
      )}
      <SettingsProfileSection />
    </AppLayout>
  );
}
