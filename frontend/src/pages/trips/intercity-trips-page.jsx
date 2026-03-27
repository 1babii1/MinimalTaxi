import { useMemo } from "react";
import { useAuthStore } from "@/entities/auth/model/use-auth-store";
import { IntercityMyTripsList } from "@/features/trips/ui/intercity-my-trips-list";
import { AppLayout } from "@/widgets/app-layout/ui/app-layout";

export function IntercityTripsPage() {
  const role = useAuthStore((state) => state.role);

  const isDriver = useMemo(
    () => String(role ?? "").toLowerCase() === "driver",
    [role],
  );

  return (
    <AppLayout>
      <div className="space-y-4">
        <IntercityMyTripsList isDriver={isDriver} />
      </div>
    </AppLayout>
  );
}
