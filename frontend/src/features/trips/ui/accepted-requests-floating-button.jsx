import { useMemo, useState } from "react";
import { Car, MapPin, MoreVertical } from "lucide-react";
import { useLocation } from "react-router-dom";
import { useAuthStore } from "@/entities/auth/model/use-auth-store";
import {
  useCancelTripMutation,
  useCompleteTripMutation,
  useMyTripsQuery,
} from "@/entities/trips/model/use-trips";
import { Button } from "@/shared/ui/button";

export function AcceptedRequestsFloatingButton() {
  const location = useLocation();
  const role = useAuthStore((state) => state.role);
  const userId = useAuthStore((state) => state.userId);
  const myTripsQuery = useMyTripsQuery(false, true);
  const cancelMutation = useCancelTripMutation();
  const completeMutation = useCompleteTripMutation();

  const [isOpen, setIsOpen] = useState(false);
  const [openActionMenuTripId, setOpenActionMenuTripId] = useState(null);

  const isDriver = String(role ?? "").toLowerCase() === "driver";
  const isProfilePage =
    location.pathname.startsWith("/app/profile") ||
    location.pathname.startsWith("/app/settings");

  const acceptedTrips = useMemo(() => {
    return (myTripsQuery.data ?? [])
      .filter((trip) => trip.driverId === userId)
      .filter((trip) => trip.status === "DriverAccepted")
      .sort(
        (left, right) =>
          Date.parse(right.departureAt ?? right.createdAt ?? 0) -
          Date.parse(left.departureAt ?? left.createdAt ?? 0),
      );
  }, [myTripsQuery.data, userId]);

  if (!isDriver || isProfilePage) {
    return null;
  }

  return (
    <>
      <div className="pointer-events-none fixed inset-y-0 left-1/2 z-[70] w-full max-w-[480px] -translate-x-1/2">
        <Button
          type="button"
          variant="ghost"
          className={`pointer-events-auto absolute right-0 top-1/2 h-14 w-9 -translate-y-1/2 rounded-l-md rounded-r-none border-l border-y border-border px-0 text-lg font-semibold ${acceptedTrips.length > 0 ? "bg-green-200/70 hover:bg-green-200/80" : "bg-slate-200/70 hover:bg-slate-200/80"}`}
          onClick={() => setIsOpen(true)}
          aria-label="Принятые заявки"
        >
          ⟨
        </Button>
      </div>

      {isOpen && (
        <div className="fixed inset-0 z-[80] flex items-end justify-center bg-black/40 p-3 sm:items-center">
          <div className="w-full max-w-[480px] rounded-xl border bg-background p-4 shadow-lg">
            <div className="mb-3 flex items-center justify-between">
              <p className="text-sm font-semibold">Принятые заявки</p>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => setIsOpen(false)}
              >
                Закрыть
              </Button>
            </div>

            <div className="rounded-lg border border-yellow-300 bg-yellow-100 p-3">
              {acceptedTrips.length === 0 && (
                <p className="text-sm text-muted-foreground">
                  Пока у вас нет принятых заявок
                </p>
              )}

              <div className="space-y-2">
                {acceptedTrips.map((trip) => (
                  <div
                    key={`floating-accepted-${trip.tripId}`}
                    className="rounded-md border border-green-200 bg-green-100 p-3"
                  >
                    <div className="flex items-start justify-between gap-2">
                      <p className="text-xs font-medium">
                        {formatTripCardTitle(trip)}
                      </p>
                      <div className="relative flex items-center gap-1">
                        <span className="text-xs font-medium text-green-600">
                          Принят
                        </span>
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          className="h-6 w-6"
                          onClick={() =>
                            setOpenActionMenuTripId((current) =>
                              current === trip.tripId ? null : trip.tripId,
                            )
                          }
                          aria-label="Действия"
                        >
                          <MoreVertical className="h-3.5 w-3.5" />
                        </Button>

                        {openActionMenuTripId === trip.tripId && (
                          <div className="absolute right-0 top-7 z-20 min-w-[132px] rounded-md border bg-background p-1 shadow-lg">
                            <Button
                              type="button"
                              variant="ghost"
                              size="sm"
                              className="w-full justify-start"
                              disabled={cancelMutation.isPending}
                              onClick={() => {
                                setOpenActionMenuTripId(null);
                                cancelMutation.mutate({
                                  id: trip.tripId,
                                  reason: "User cancelled",
                                });
                              }}
                            >
                              Отменить
                            </Button>
                          </div>
                        )}
                      </div>
                    </div>
                    <p className="text-xs text-muted-foreground">
                      Откуда: {trip.fromAddress || "—"}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      Куда: {trip.toAddress || "—"}
                    </p>
                    <p className="text-xs text-muted-foreground">
                      Дата: {formatDateTime(trip.departureAt ?? trip.createdAt)}
                    </p>
                    <div className="mt-1 flex items-center gap-2 text-xs text-muted-foreground">
                      <Car
                        className="h-3.5 w-3.5"
                        style={{
                          color: toValidHexColorOrNull(trip.driverCarColor),
                          fill: toValidHexColorOrNull(trip.driverCarColor),
                        }}
                      />
                      <span>
                        Авто: {trip.driverCarBrand || "—"}{" "}
                        {trip.driverCarModel || ""}
                        {trip.driverCarPlateNumber
                          ? ` • ${trip.driverCarPlateNumber}`
                          : ""}
                      </span>
                    </div>

                    <div className="mt-2 flex items-center justify-end gap-2">
                      <span className="mr-auto text-xs text-muted-foreground">
                        {getCounterpartyName(role, trip)}
                      </span>

                      {buildRouteMapHref(trip) && (
                        <Button
                          asChild
                          type="button"
                          variant="outline"
                          size="sm"
                          className="h-9 w-9 px-0"
                        >
                          <a
                            href={buildRouteMapHref(trip)}
                            target="_blank"
                            rel="noreferrer"
                            aria-label="Яндекс карты"
                            title="Яндекс карты"
                          >
                            <MapPin className="h-3.5 w-3.5" />
                          </a>
                        </Button>
                      )}

                      {Boolean(getCounterpartyPhone(role, trip)) && (
                        <Button
                          asChild
                          type="button"
                          variant="outline"
                          size="sm"
                          className="h-9 px-2"
                        >
                          <a
                            href={`tel:${getCounterpartyPhone(role, trip)}`}
                            className="inline-flex h-full items-center justify-center"
                          >
                            <span className="block text-[10px] leading-none">
                              Позвонить
                            </span>
                          </a>
                        </Button>
                      )}

                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="h-9"
                        disabled={completeMutation.isPending}
                        onClick={() =>
                          completeMutation.mutate({ id: trip.tripId })
                        }
                      >
                        Завершить
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

function formatTripCardTitle(trip) {
  const shortId = String(trip.tripId).slice(0, 6);
  if (trip.tripType === "Intercity") {
    return `Межгород №${shortId}`;
  }

  return `Местная заявка №${shortId}`;
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

function buildRouteMapHref(trip) {
  const originLatitude = trip.originLatitude ?? trip.latitude;
  const originLongitude = trip.originLongitude ?? trip.longitude;

  const hasRouteCoordinates =
    typeof originLatitude === "number" &&
    typeof originLongitude === "number" &&
    typeof trip.destinationLatitude === "number" &&
    typeof trip.destinationLongitude === "number";

  if (!hasRouteCoordinates) {
    return null;
  }

  return `https://yandex.ru/maps/?rtext=${originLatitude},${originLongitude}~${trip.destinationLatitude},${trip.destinationLongitude}&rtt=auto`;
}

function getCounterpartyName(role, trip) {
  const isDriver = String(role ?? "").toLowerCase() === "driver";
  const source = isDriver ? trip.passengerName : trip.driverName;
  const normalized = String(source ?? "").trim();

  if (!normalized) {
    return "—";
  }

  return normalized.length > 10 ? `${normalized.slice(0, 10)}...` : normalized;
}

function getCounterpartyPhone(role, trip) {
  const isDriver = String(role ?? "").toLowerCase() === "driver";
  const source = isDriver ? trip.passengerPhone : trip.driverPhone;
  return String(source ?? "").trim();
}
