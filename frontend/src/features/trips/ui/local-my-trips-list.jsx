import { useMemo, useState } from "react";
import {
  ArrowDown,
  ArrowUp,
  Car,
  MapPin,
  MoreVertical,
  Plus,
  X,
} from "lucide-react";
import { useAuthStore } from "@/entities/auth/model/use-auth-store";
import {
  useAcceptTripMutation,
  useCancelTripMutation,
  useInfiniteNearbyTripsQuery,
  useMyTripsQuery,
} from "@/entities/trips/model/use-trips";
import { LocalTripCreateForm } from "@/features/trips/ui/local-trip-create-form";
import { useLocalTripsFiltersStore } from "@/store/local-trips-filters-store";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/shared/ui/card";
import { Input } from "@/shared/ui/input";
import { Label } from "@/shared/ui/label";

export function LocalMyTripsList() {
  const role = useAuthStore((state) => state.role);
  const isDriver = String(role ?? "").toLowerCase() === "driver";
  const fromAddress = useLocalTripsFiltersStore((state) => state.fromAddress);
  const toAddress = useLocalTripsFiltersStore((state) => state.toAddress);
  const radiusMeters = useLocalTripsFiltersStore((state) => state.radiusMeters);
  const sortKeys = useLocalTripsFiltersStore(
    (state) => state.sortKeys ?? ["newest"],
  );

  const setFromAddress = useLocalTripsFiltersStore(
    (state) => state.setFromAddress,
  );
  const setToAddress = useLocalTripsFiltersStore((state) => state.setToAddress);
  const setRadiusMeters = useLocalTripsFiltersStore(
    (state) => state.setRadiusMeters,
  );
  const addSortKey = useLocalTripsFiltersStore((state) => state.addSortKey);
  const setSortKeys = useLocalTripsFiltersStore((state) => state.setSortKeys);
  const removeSortKey = useLocalTripsFiltersStore(
    (state) => state.removeSortKey,
  );
  const resetSortKeys = useLocalTripsFiltersStore(
    (state) => state.resetSortKeys,
  );
  const resetAdvancedFilters = useLocalTripsFiltersStore(
    (state) => state.resetAdvancedFilters,
  );

  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [isFiltersOpen, setIsFiltersOpen] = useState(false);
  const [isSortOptionsOpen, setIsSortOptionsOpen] = useState(false);
  const [openActionMenuTripId, setOpenActionMenuTripId] = useState(null);
  const [draftFromAddress, setDraftFromAddress] = useState(fromAddress);
  const [draftToAddress, setDraftToAddress] = useState(toAddress);
  const [draftRadiusMeters, setDraftRadiusMeters] = useState(
    String(radiusMeters),
  );

  const myAllTripsQuery = useMyTripsQuery(false, true);
  const nearbyTripsQuery = useInfiniteNearbyTripsQuery({
    latitude: 0,
    longitude: 0,
    radiusMeters: Number(radiusMeters),
    tripType: "Local",
    fromAddress: fromAddress || null,
    toAddress: toAddress || null,
    includeInactive: false,
  });

  const acceptMutation = useAcceptTripMutation();
  const cancelMutation = useCancelTripMutation();

  const localTrips = useMemo(() => {
    const source = (nearbyTripsQuery.data?.pages ?? []).flatMap(
      (page) => page ?? [],
    );

    return source
      .filter((trip) => trip.tripType === "Local")
      .filter((trip) => trip.status === "Created")
      .sort((left, right) => compareBySortKeys(left, right, sortKeys));
  }, [nearbyTripsQuery.data?.pages, sortKeys]);

  const createdTrips = useMemo(() => {
    const source = myAllTripsQuery.data ?? [];

    return source
      .filter((trip) => trip.tripType === "Local")
      .filter((trip) => trip.createdByUser)
      .filter((trip) => shouldShowInCreatedBlock(trip))
      .sort((left, right) => compareBySortKeys(left, right, ["newest"]));
  }, [myAllTripsQuery.data]);

  const availableSortOptions = getAvailableSortOptions(sortKeys);

  const isLoading = nearbyTripsQuery.isLoading;
  const isError =
    nearbyTripsQuery.isError ||
    cancelMutation.isError ||
    acceptMutation.isError;

  const errorMessage =
    nearbyTripsQuery.error?.message ||
    acceptMutation.error?.message ||
    cancelMutation.error?.message;

  const applyFilters = () => {
    setFromAddress(String(draftFromAddress).trim());
    setToAddress(String(draftToAddress).trim());
    setRadiusMeters(Math.max(1, Number(draftRadiusMeters || 50000)));
    setIsFiltersOpen(false);
  };

  const toggleSortDirection = (index) => {
    const current = sortKeys[index];
    const toggled = getToggledSortKey(current);

    if (!toggled) {
      return;
    }

    const next = [...sortKeys];
    next[index] = toggled;
    setSortKeys(next);
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between gap-2">
          <CardTitle>Местные</CardTitle>
          {!isDriver && (
            <Button
              type="button"
              size="sm"
              onClick={() => setIsCreateOpen(true)}
            >
              Создать заявку
            </Button>
          )}
        </div>
      </CardHeader>

      <CardContent className="space-y-4">
        {isError && <Alert variant="destructive">{errorMessage}</Alert>}

        {createdTrips.length > 0 && (
          <div className="rounded-lg border border-yellow-300 bg-yellow-100 p-3">
            <p className="mb-2 text-sm font-semibold">Созданные мной заявки</p>
            <div className="space-y-2">
              {createdTrips.map((trip) => (
                <div
                  key={`created-${trip.tripId}`}
                  className="rounded-md border border-green-200 bg-green-100 p-2"
                >
                  <div className="flex items-start justify-between gap-2">
                    <p className="text-xs font-medium">
                      Местная заявка №{String(trip.tripId).slice(0, 6)}
                    </p>
                    <div className="relative flex items-center gap-1">
                      <span
                        className={`text-xs font-medium ${getCreatedStatusClassName(trip.status)}`}
                      >
                        {getCreatedStatusLabel(trip.status)}
                      </span>
                      {!isCancelledStatus(trip.status) && (
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
                      )}

                      {openActionMenuTripId === trip.tripId &&
                        !isCancelledStatus(trip.status) && (
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
                    Время: {formatDateTime(trip.createdAt)}
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

                    {trip.status === "DriverAccepted" &&
                      Boolean(getCounterpartyPhone(role, trip)) && (
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
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        <div className="flex flex-wrap items-center gap-2 text-sm">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => {
              setDraftFromAddress(fromAddress);
              setDraftToAddress(toAddress);
              setDraftRadiusMeters(String(radiusMeters));
              setIsSortOptionsOpen(false);
              setIsFiltersOpen(true);
            }}
          >
            Все фильтры
          </Button>
        </div>

        {isFiltersOpen && (
          <div className="fixed inset-0 z-[140] flex items-end justify-center bg-black/40 p-3 sm:items-center">
            <div className="w-full max-w-[480px] rounded-xl border bg-background p-4 shadow-lg">
              <div className="mb-3 flex items-center justify-between">
                <p className="text-sm font-semibold">Фильтры</p>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => setIsFiltersOpen(false)}
                >
                  Закрыть
                </Button>
              </div>

              <div className="space-y-3">
                <div className="space-y-1">
                  <Label htmlFor="local-filter-from">Откуда</Label>
                  <Input
                    id="local-filter-from"
                    value={draftFromAddress}
                    onChange={(event) =>
                      setDraftFromAddress(event.target.value)
                    }
                    placeholder="Фильтр по адресу отправления"
                  />
                </div>

                <div className="space-y-1">
                  <Label htmlFor="local-filter-to">Куда</Label>
                  <Input
                    id="local-filter-to"
                    value={draftToAddress}
                    onChange={(event) => setDraftToAddress(event.target.value)}
                    placeholder="Фильтр по адресу назначения"
                  />
                </div>

                <div className="space-y-1">
                  <Label htmlFor="local-filter-radius">Радиус (м)</Label>
                  <Input
                    id="local-filter-radius"
                    type="number"
                    min="1"
                    value={draftRadiusMeters}
                    onChange={(event) =>
                      setDraftRadiusMeters(event.target.value)
                    }
                  />
                </div>

                <div className="space-y-2">
                  <Label>Сортировка</Label>
                  <div className="relative">
                    <div className="flex items-center gap-2 overflow-x-auto pb-1">
                      {sortKeys.map((item, index) => (
                        <div
                          key={item}
                          className="flex w-[32%] min-w-[170px] items-center gap-2 rounded-md border px-2 py-2"
                        >
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            className="h-6 w-6"
                            onClick={() => toggleSortDirection(index)}
                          >
                            {item === "oldest" || item === "distance_desc" ? (
                              <ArrowDown className="h-3.5 w-3.5" />
                            ) : (
                              <ArrowUp className="h-3.5 w-3.5" />
                            )}
                          </Button>
                          <span className="truncate text-xs">
                            {index + 1}. {getSortLabel(item)}
                          </span>
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            className="ml-auto h-6 w-6"
                            onClick={() => removeSortKey(item)}
                          >
                            <X className="h-3.5 w-3.5" />
                          </Button>
                        </div>
                      ))}

                      <Button
                        type="button"
                        variant="outline"
                        size="icon"
                        className="h-9 w-9 shrink-0"
                        onClick={() => setIsSortOptionsOpen((prev) => !prev)}
                      >
                        <Plus className="h-4 w-4" />
                      </Button>
                    </div>

                    {isSortOptionsOpen && (
                      <div className="absolute bottom-full right-0 z-20 mb-2 min-w-[260px] rounded-md border bg-background p-2 shadow-lg">
                        {availableSortOptions.length === 0 && (
                          <p className="text-xs text-muted-foreground">
                            Все варианты сортировки уже добавлены.
                          </p>
                        )}

                        {availableSortOptions.map((option) => (
                          <Button
                            key={option.key}
                            type="button"
                            variant="outline"
                            className="mb-1 w-full justify-start last:mb-0"
                            onClick={() => {
                              addSortKey(option.key);
                              setIsSortOptionsOpen(false);
                            }}
                          >
                            {option.label}
                          </Button>
                        ))}
                      </div>
                    )}
                  </div>
                </div>

                <div className="flex justify-between gap-2">
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => {
                      resetAdvancedFilters();
                      resetSortKeys();
                      setDraftFromAddress("");
                      setDraftToAddress("");
                      setDraftRadiusMeters("50000");
                      setIsSortOptionsOpen(false);
                    }}
                  >
                    Сбросить
                  </Button>

                  <Button type="button" onClick={applyFilters}>
                    Применить
                  </Button>
                </div>
              </div>
            </div>
          </div>
        )}

        {!isDriver && isCreateOpen && (
          <div className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-3 sm:items-center">
            <div className="w-full max-w-[480px]">
              <LocalTripCreateForm
                onCreated={() => setIsCreateOpen(false)}
                onCancel={() => setIsCreateOpen(false)}
              />
            </div>
          </div>
        )}

        {isLoading && (
          <p className="text-sm text-muted-foreground">Загрузка поездок...</p>
        )}

        {!isLoading && localTrips.length === 0 && (
          <div className="rounded-lg border border-dashed bg-muted/30 p-4 text-center">
            <p className="text-sm text-muted-foreground">
              По выбранным фильтрам поездки не найдены.
            </p>
          </div>
        )}

        <div className="space-y-3">
          {localTrips.map((trip) => {
            const originLatitude = trip.originLatitude ?? trip.latitude;
            const originLongitude = trip.originLongitude ?? trip.longitude;

            const hasRouteCoordinates =
              typeof originLatitude === "number" &&
              typeof originLongitude === "number" &&
              typeof trip.destinationLatitude === "number" &&
              typeof trip.destinationLongitude === "number";

            const mapHref = hasRouteCoordinates
              ? `https://yandex.ru/maps/?rtext=${originLatitude},${originLongitude}~${trip.destinationLatitude},${trip.destinationLongitude}&rtt=auto`
              : null;

            return (
              <div
                key={trip.tripId}
                className="rounded-lg border bg-background p-3 shadow-sm"
              >
                <div className="flex items-start justify-between gap-2">
                  <p className="text-sm font-medium">
                    Местная заявка №{String(trip.tripId).slice(0, 6)}
                  </p>
                  <span className="text-xs text-muted-foreground">
                    {formatTripDistance(
                      originLatitude,
                      originLongitude,
                      trip.destinationLatitude,
                      trip.destinationLongitude,
                    )}
                  </span>
                </div>
                <p className="mt-1 text-xs text-muted-foreground">
                  Статус: {formatStatus(trip.status)}
                </p>
                <p className="text-xs text-muted-foreground">
                  Откуда: {trip.fromAddress || "—"}
                </p>
                <p className="text-xs text-muted-foreground">
                  Куда: {trip.toAddress || "—"}
                </p>
                <p className="text-xs text-muted-foreground">
                  Когда: {formatDateTime(trip.createdAt)}
                </p>

                {trip.status === "DriverAccepted" && trip.driverId && (
                  <div className="mt-1 flex items-center gap-2 text-xs text-muted-foreground">
                    <Car
                      className="h-3.5 w-3.5"
                      style={{
                        color: toValidHexColorOrNull(trip.driverCarColor),
                        fill: toValidHexColorOrNull(trip.driverCarColor),
                      }}
                    />
                    <span>
                      {trip.driverCarBrand || "—"} {trip.driverCarModel || ""}
                      {trip.driverCarPlateNumber
                        ? ` • ${trip.driverCarPlateNumber}`
                        : ""}
                    </span>
                  </div>
                )}

                <div className="mt-3 flex items-center gap-2">
                  {mapHref ? (
                    <a
                      href={mapHref}
                      target="_blank"
                      rel="noreferrer"
                      className="inline-flex items-center text-xs text-primary underline-offset-4 hover:underline"
                    >
                      <MapPin className="mr-1 h-3 w-3" />
                      Открыть на карте
                    </a>
                  ) : (
                    <span className="text-xs text-muted-foreground">
                      Открыть на карте
                    </span>
                  )}

                  {role === "Driver" && trip.status === "Created" && (
                    <Button
                      type="button"
                      size="sm"
                      className="ml-auto"
                      disabled={acceptMutation.isPending}
                      onClick={() =>
                        acceptMutation.mutate({
                          id: trip.tripId,
                        })
                      }
                    >
                      Принять
                    </Button>
                  )}
                </div>
              </div>
            );
          })}
        </div>

        {nearbyTripsQuery.hasNextPage && (
          <Button
            type="button"
            variant="outline"
            className="w-full"
            onClick={() => nearbyTripsQuery.fetchNextPage()}
            disabled={nearbyTripsQuery.isFetchingNextPage}
          >
            {nearbyTripsQuery.isFetchingNextPage
              ? "Загрузка..."
              : "Загрузить еще"}
          </Button>
        )}
      </CardContent>
    </Card>
  );
}

function toValidHexColorOrNull(value) {
  const normalized = String(value ?? "").trim();
  if (/^#[0-9A-Fa-f]{6}$/.test(normalized)) {
    return normalized;
  }

  return undefined;
}

function isCreatedBlockStatus(status) {
  return status === "Created" || status === "DriverAccepted";
}

function shouldShowInCreatedBlock(trip) {
  return isCreatedBlockStatus(trip.status);
}

function isCancelledStatus(status) {
  return (
    status === "Cancelled" ||
    status === "CancelledByPassenger" ||
    status === "CancelledByDriver"
  );
}

function getCreatedStatusLabel(status) {
  if (status === "Created") {
    return "Создан";
  }

  if (status === "DriverAccepted") {
    return "Принят";
  }

  return "Отменен";
}

function getCreatedStatusClassName(status) {
  if (status === "Created") {
    return "text-yellow-600";
  }

  if (status === "DriverAccepted") {
    return "text-green-600";
  }

  return "text-red-600";
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

function compareBySortKeys(left, right, sortKeys) {
  for (const sortKey of sortKeys) {
    if (sortKey === "newest") {
      const result =
        Date.parse(right.createdAt ?? 0) - Date.parse(left.createdAt ?? 0);
      if (result !== 0) {
        return result;
      }
    }

    if (sortKey === "oldest") {
      const result =
        Date.parse(left.createdAt ?? 0) - Date.parse(right.createdAt ?? 0);
      if (result !== 0) {
        return result;
      }
    }

    if (sortKey === "distance_asc") {
      const leftDistance = getTripDistanceKm(left);
      const rightDistance = getTripDistanceKm(right);
      const result = compareNullableDistance(
        leftDistance,
        rightDistance,
        "asc",
      );
      if (result !== 0) {
        return result;
      }
    }

    if (sortKey === "distance_desc") {
      const leftDistance = getTripDistanceKm(left);
      const rightDistance = getTripDistanceKm(right);
      const result = compareNullableDistance(
        leftDistance,
        rightDistance,
        "desc",
      );
      if (result !== 0) {
        return result;
      }
    }
  }

  return 0;
}

function formatDateTime(value) {
  if (!value) {
    return "—";
  }

  return new Date(value).toLocaleString();
}

function formatStatus(status) {
  if (status === "Pending" || status === "Created") {
    return "Ожидает водителя";
  }

  if (status === "DriverAccepted") {
    return "Водитель принят";
  }

  if (status === "Completed") {
    return "Завершена";
  }

  if (
    status === "Cancelled" ||
    status === "CancelledByPassenger" ||
    status === "CancelledByDriver"
  ) {
    return "Отменена";
  }

  return status;
}

function formatTripDistance(
  fromLatitude,
  fromLongitude,
  toLatitude,
  toLongitude,
) {
  const distanceKm = calculateDistanceKm(
    fromLatitude,
    fromLongitude,
    toLatitude,
    toLongitude,
  );

  if (distanceKm == null) {
    return "—";
  }

  return `${distanceKm.toFixed(1)} км`;
}

function getTripDistanceKm(trip) {
  const fromLatitude = trip.originLatitude ?? trip.latitude;
  const fromLongitude = trip.originLongitude ?? trip.longitude;

  return calculateDistanceKm(
    fromLatitude,
    fromLongitude,
    trip.destinationLatitude,
    trip.destinationLongitude,
  );
}

function compareNullableDistance(leftValue, rightValue, direction) {
  if (leftValue == null && rightValue == null) {
    return 0;
  }

  if (leftValue == null) {
    return 1;
  }

  if (rightValue == null) {
    return -1;
  }

  if (direction === "asc") {
    return leftValue - rightValue;
  }

  return rightValue - leftValue;
}

function calculateDistanceKm(
  fromLatitude,
  fromLongitude,
  toLatitude,
  toLongitude,
) {
  if (
    typeof fromLatitude !== "number" ||
    typeof fromLongitude !== "number" ||
    typeof toLatitude !== "number" ||
    typeof toLongitude !== "number"
  ) {
    return null;
  }

  if (
    !Number.isFinite(fromLatitude) ||
    !Number.isFinite(fromLongitude) ||
    !Number.isFinite(toLatitude) ||
    !Number.isFinite(toLongitude)
  ) {
    return null;
  }

  const earthRadiusKm = 6371;
  const deltaLat = degreesToRadians(toLatitude - fromLatitude);
  const deltaLon = degreesToRadians(toLongitude - fromLongitude);
  const fromLatRad = degreesToRadians(fromLatitude);
  const toLatRad = degreesToRadians(toLatitude);

  const a =
    Math.sin(deltaLat / 2) * Math.sin(deltaLat / 2) +
    Math.cos(fromLatRad) *
      Math.cos(toLatRad) *
      Math.sin(deltaLon / 2) *
      Math.sin(deltaLon / 2);

  const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  return earthRadiusKm * c;
}

function degreesToRadians(value) {
  return (value * Math.PI) / 180;
}

const SORT_OPTIONS = [
  { key: "newest", label: "Сначала новые" },
  { key: "oldest", label: "Сначала старые" },
  { key: "distance_asc", label: "Сначала с меньшим расстоянием" },
  { key: "distance_desc", label: "Сначала с большим расстоянием" },
];

const TOGGLED_SORT_KEY = {
  newest: "oldest",
  oldest: "newest",
  distance_asc: "distance_desc",
  distance_desc: "distance_asc",
};

function getSortLabel(sortKey) {
  const option = SORT_OPTIONS.find((item) => item.key === sortKey);
  return option?.label ?? "Без сортировки";
}

function getToggledSortKey(sortKey) {
  return TOGGLED_SORT_KEY[sortKey] ?? null;
}

function getAvailableSortOptions(sortKeys) {
  const hasDateSort =
    sortKeys.includes("newest") || sortKeys.includes("oldest");
  const hasDistanceSort =
    sortKeys.includes("distance_asc") || sortKeys.includes("distance_desc");

  return SORT_OPTIONS.filter((item) => {
    if (sortKeys.includes(item.key)) {
      return false;
    }

    if (hasDateSort && (item.key === "newest" || item.key === "oldest")) {
      return false;
    }

    if (
      hasDistanceSort &&
      (item.key === "distance_asc" || item.key === "distance_desc")
    ) {
      return false;
    }

    return true;
  });
}
