import { useEffect, useMemo, useState } from "react";
import { Car, MapPin } from "lucide-react";
import { useLocation } from "react-router-dom";
import { useAuthStore } from "@/entities/auth/model/use-auth-store";
import {
  useAcceptIntercityInvitationMutation,
  useDeclineIntercityInvitationMutation,
  useMyIntercityInvitationsQuery,
  useMyTripsQuery,
  useNearbyTripsQuery,
} from "@/entities/trips/model/use-trips";
import { Button } from "@/shared/ui/button";

export function IntercityInvitationsFloatingButton() {
  const location = useLocation();
  const role = useAuthStore((state) => state.role);

  const [isOpen, setIsOpen] = useState(false);
  const [currentTimeMs, setCurrentTimeMs] = useState(() =>
    new Date().getTime(),
  );

  const isPassenger = String(role ?? "").toLowerCase() === "passenger";
  const isIntercityPage = location.pathname.startsWith("/app/trips/intercity");
  const isProfilePage =
    location.pathname.startsWith("/app/profile") ||
    location.pathname.startsWith("/app/settings");

  const invitationsQuery = useMyIntercityInvitationsQuery(isPassenger);
  const myTripsQuery = useMyTripsQuery(false, isPassenger);
  const nearbyTripsQuery = useNearbyTripsQuery({
    latitude: 0,
    longitude: 0,
    radiusMeters: 1,
    tripType: "Intercity",
    includeInactive: true,
  });

  const acceptMutation = useAcceptIntercityInvitationMutation();
  const declineMutation = useDeclineIntercityInvitationMutation();

  useEffect(() => {
    const intervalId = window.setInterval(() => {
      setCurrentTimeMs(new Date().getTime());
    }, 30_000);

    return () => window.clearInterval(intervalId);
  }, []);

  const pendingInvitations = useMemo(() => {
    return (invitationsQuery.data ?? [])
      .filter((invitation) => invitation.status === "Pending")
      .filter((invitation) => Date.parse(invitation.expiresAt) > currentTimeMs)
      .sort(
        (left, right) =>
          Date.parse(right.createdAt ?? 0) - Date.parse(left.createdAt ?? 0),
      );
  }, [currentTimeMs, invitationsQuery.data]);

  const myTripsMap = useMemo(() => {
    const map = new Map();
    for (const trip of myTripsQuery.data ?? []) {
      map.set(trip.tripId, trip);
    }
    return map;
  }, [myTripsQuery.data]);

  const nearbyTripsMap = useMemo(() => {
    const map = new Map();
    for (const trip of nearbyTripsQuery.data ?? []) {
      map.set(trip.tripId, trip);
    }
    return map;
  }, [nearbyTripsQuery.data]);

  if (!isPassenger || !isIntercityPage || isProfilePage) {
    return null;
  }

  return (
    <>
      <div className="pointer-events-none fixed inset-y-0 left-1/2 z-[70] w-full max-w-[480px] -translate-x-1/2">
        <Button
          type="button"
          variant="ghost"
          className={`pointer-events-auto absolute right-0 top-[42%] h-14 w-9 -translate-y-1/2 rounded-l-md rounded-r-none border-l border-y border-border px-0 text-lg font-semibold ${pendingInvitations.length > 0 ? "bg-blue-200/70 hover:bg-blue-200/80" : "bg-slate-200/70 hover:bg-slate-200/80"}`}
          onClick={() => setIsOpen(true)}
          aria-label="Приглашения водителей"
        >
          ⟨
        </Button>
      </div>

      {isOpen && (
        <div className="fixed inset-0 z-[80] flex items-end justify-center bg-black/40 p-3 sm:items-center">
          <div className="w-full max-w-[480px] rounded-xl border bg-background p-4 shadow-lg">
            <div className="mb-3 flex items-center justify-between">
              <p className="text-sm font-semibold">Приглашения водителей</p>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => setIsOpen(false)}
              >
                Закрыть
              </Button>
            </div>

            <div className="space-y-2">
              {pendingInvitations.length === 0 && (
                <p className="text-xs text-muted-foreground">
                  Новых приглашений пока нет.
                </p>
              )}

              {pendingInvitations.map((invitation) => {
                const driverTrip = nearbyTripsMap.get(invitation.driverTripId);
                const passengerTrip = myTripsMap.get(
                  invitation.passengerTripId,
                );
                const expiresInMinutes = Math.max(
                  0,
                  Math.ceil(
                    (Date.parse(invitation.expiresAt) - currentTimeMs) / 60000,
                  ),
                );

                const mapHref =
                  typeof driverTrip?.originLatitude === "number" &&
                  typeof driverTrip?.originLongitude === "number" &&
                  typeof driverTrip?.destinationLatitude === "number" &&
                  typeof driverTrip?.destinationLongitude === "number"
                    ? `https://yandex.ru/maps/?rtext=${driverTrip.originLatitude},${driverTrip.originLongitude}~${driverTrip.destinationLatitude},${driverTrip.destinationLongitude}&rtt=auto`
                    : null;

                return (
                  <div
                    key={invitation.invitationId}
                    className="rounded-md border border-blue-200 bg-blue-50 p-3"
                  >
                    <div className="flex items-start justify-between gap-2">
                      <p className="text-xs font-medium">
                        Поездка #{String(invitation.driverTripId).slice(0, 6)}
                      </p>
                      <span className="text-xs font-medium text-blue-700">
                        {expiresInMinutes} мин
                      </span>
                    </div>
                    <p className="text-xs text-muted-foreground">
                      Маршрут водителя: {driverTrip?.fromAddress || "—"} →{" "}
                      {driverTrip?.toAddress || "—"}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      Ваша заявка: {passengerTrip?.fromAddress || "—"} →{" "}
                      {passengerTrip?.toAddress || "—"}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      Выезд:{" "}
                      {formatDateTime(
                        driverTrip?.departureAt ?? driverTrip?.createdAt,
                      )}
                    </p>
                    <div className="mt-1 flex items-center gap-2 text-xs text-muted-foreground">
                      <Car
                        className="h-3.5 w-3.5"
                        style={{
                          color: toValidHexColorOrNull(
                            driverTrip?.driverCarColor,
                          ),
                          fill: toValidHexColorOrNull(
                            driverTrip?.driverCarColor,
                          ),
                        }}
                      />
                      <span>
                        Авто: {driverTrip?.driverCarBrand || "—"}{" "}
                        {driverTrip?.driverCarModel || ""}
                        {driverTrip?.driverCarPlateNumber
                          ? ` • ${driverTrip.driverCarPlateNumber}`
                          : ""}
                      </span>
                    </div>

                    <div className="mt-2 flex items-center justify-end gap-2">
                      {mapHref && (
                        <Button
                          asChild
                          type="button"
                          variant="outline"
                          size="sm"
                          className="h-9 w-9 px-0"
                        >
                          <a
                            href={mapHref}
                            target="_blank"
                            rel="noreferrer"
                            aria-label="Яндекс карты"
                            title="Яндекс карты"
                          >
                            <MapPin className="h-3.5 w-3.5" />
                          </a>
                        </Button>
                      )}
                      <Button
                        type="button"
                        size="sm"
                        className="h-9"
                        disabled={acceptMutation.isPending}
                        onClick={() =>
                          acceptMutation.mutate({
                            invitationId: invitation.invitationId,
                          })
                        }
                      >
                        Принять
                      </Button>
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="h-9"
                        disabled={declineMutation.isPending}
                        onClick={() =>
                          declineMutation.mutate({
                            invitationId: invitation.invitationId,
                          })
                        }
                      >
                        Отклонить
                      </Button>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      )}
    </>
  );
}

function toValidHexColorOrNull(value) {
  const normalized = String(value ?? "").trim();
  if (/^#[0-9A-Fa-f]{6}$/.test(normalized)) {
    return normalized;
  }

  return undefined;
}

function formatDateTime(value) {
  if (!value) {
    return "—";
  }

  return new Date(value).toLocaleString();
}
