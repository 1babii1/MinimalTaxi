import { LocalMyTripsList } from "@/features/trips/ui/local-my-trips-list";
import { AppLayout } from "@/widgets/app-layout/ui/app-layout";

export function LocalTripsPage() {
  return (
    <AppLayout>
      <div className="space-y-4">
        <LocalMyTripsList />
      </div>
    </AppLayout>
  );
}
