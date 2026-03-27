import { useMemo } from "react";
import { useAuthStore } from "@/entities/auth/model/use-auth-store";
import { IntercityTripForm } from "@/features/trips/ui/intercity-trip-form";
import { LocalTripCreateForm } from "@/features/trips/ui/local-trip-create-form";
import { MyTripsList } from "@/features/trips/ui/my-trips-list";
import { NearbyTripsList } from "@/features/trips/ui/nearby-trips-list";
import { useSettingsStore } from "@/features/settings/model/use-settings-store";
import { AppLayout } from "@/widgets/app-layout/ui/app-layout";

export function TripsPage() {
  const role = useAuthStore((state) => state.role);
  const tripMode = useSettingsStore((state) => state.tripMode);

  const isDriver = useMemo(
    () => String(role ?? "").toLowerCase() === "driver",
    [role],
  );

  const tripType = tripMode === "local" ? "Local" : "Intercity";

  return (
    <AppLayout>
      <div className="space-y-4">
        {tripMode === "local" ? (
          isDriver ? (
            <NearbyTripsList tripType={tripType} />
          ) : (
            <LocalTripCreateForm />
          )
        ) : (
          <IntercityTripForm isDriver={isDriver} />
        )}

        <MyTripsList />
      </div>
    </AppLayout>
  );
}
