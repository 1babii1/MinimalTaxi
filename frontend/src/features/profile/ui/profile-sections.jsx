import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Car, MapPin } from "lucide-react";
import {
  useCreateLocalTripMutation,
  useMyTripsQuery,
} from "@/entities/trips/model/use-trips";
import { useAuthStore } from "@/entities/auth/model/use-auth-store";
import {
  useChangeEmailMutation,
  useChangePasswordMutation,
} from "@/features/auth/model/use-auth-mutations";
import { useSavedLocations } from "@/entities/locations/model/use-saved-locations";
import { AddressAutocompleteInput } from "@/features/trips/ui/address-autocomplete-input";
import { ProfileForm } from "@/features/profile/ui/profile-form";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/shared/ui/card";
import { Input } from "@/shared/ui/input";
import { Label } from "@/shared/ui/label";

const SECTIONS = [
  { id: "profile", title: "Профиль", href: "/app/settings/profile" },
  { id: "security", title: "Безопасность", href: "/app/settings/security" },
  { id: "history", title: "История заказов", href: "/app/settings/history" },
  { id: "locations", title: "Мои локации", href: "/app/settings/locations" },
  {
    id: "support",
    title: "Поддержать разработчика",
    href: "/app/settings/support",
  },
];

export function ProfileSections() {
  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <CardTitle>Настройки</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3 pt-0">
          {SECTIONS.map((section) => (
            <Link
              key={section.id}
              to={section.href}
              className="block text-sm text-foreground underline-offset-4 hover:underline"
            >
              {section.title}
            </Link>
          ))}
        </CardContent>
      </Card>
    </div>
  );
}

export function SettingsProfileSection() {
  return <ProfileForm />;
}

export function SecuritySection() {
  const authEmail = useAuthStore((state) => state.email);
  const clearAuth = useAuthStore((state) => state.clearAuth);

  const changePasswordMutation = useChangePasswordMutation();
  const changeEmailMutation = useChangeEmailMutation();

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const [newEmail, setNewEmail] = useState("");
  const [emailPassword, setEmailPassword] = useState("");

  const onChangePassword = async (event) => {
    event.preventDefault();

    if (newPassword !== confirmPassword) {
      return;
    }

    await changePasswordMutation.mutateAsync({
      currentPassword,
      newPassword,
    });

    setCurrentPassword("");
    setNewPassword("");
    setConfirmPassword("");
  };

  const onChangeEmail = async (event) => {
    event.preventDefault();

    await changeEmailMutation.mutateAsync({
      newEmail,
      password: emailPassword,
    });

    clearAuth();
    window.location.assign("/auth/login");
  };

  const passwordMismatch =
    confirmPassword.length > 0 &&
    newPassword.length > 0 &&
    newPassword !== confirmPassword;

  return (
    <div className="grid gap-4 md:grid-cols-2">
      <Card>
        <CardHeader>
          <CardTitle>Смена пароля</CardTitle>
        </CardHeader>
        <CardContent>
          <form className="space-y-4" onSubmit={onChangePassword}>
            {changePasswordMutation.isSuccess && (
              <Alert>Пароль успешно изменён.</Alert>
            )}
            {changePasswordMutation.isError && (
              <Alert variant="destructive">
                {changePasswordMutation.error.message}
              </Alert>
            )}
            {passwordMismatch && (
              <Alert variant="destructive">Пароли не совпадают.</Alert>
            )}

            <div className="space-y-2">
              <Label htmlFor="current-password">Текущий пароль</Label>
              <Input
                id="current-password"
                type="password"
                value={currentPassword}
                onChange={(event) => setCurrentPassword(event.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="new-password">Новый пароль</Label>
              <Input
                id="new-password"
                type="password"
                value={newPassword}
                onChange={(event) => setNewPassword(event.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="confirm-password">Повторите пароль</Label>
              <Input
                id="confirm-password"
                type="password"
                value={confirmPassword}
                onChange={(event) => setConfirmPassword(event.target.value)}
                required
              />
            </div>

            <Button
              disabled={changePasswordMutation.isPending || passwordMismatch}
              type="submit"
            >
              {changePasswordMutation.isPending
                ? "Сохранение..."
                : "Изменить пароль"}
            </Button>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Смена почты</CardTitle>
        </CardHeader>
        <CardContent>
          <form className="space-y-4" onSubmit={onChangeEmail}>
            {changeEmailMutation.isError && (
              <Alert variant="destructive">
                {changeEmailMutation.error.message}
              </Alert>
            )}

            <div className="space-y-2">
              <Label htmlFor="current-email">Текущая почта</Label>
              <Input id="current-email" value={authEmail ?? ""} readOnly />
            </div>

            <div className="space-y-2">
              <Label htmlFor="new-email">Новая почта</Label>
              <Input
                id="new-email"
                type="email"
                value={newEmail}
                onChange={(event) => setNewEmail(event.target.value)}
                required
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="email-password">Подтвердите паролем</Label>
              <Input
                id="email-password"
                type="password"
                value={emailPassword}
                onChange={(event) => setEmailPassword(event.target.value)}
                required
              />
            </div>

            <Button disabled={changeEmailMutation.isPending} type="submit">
              {changeEmailMutation.isPending
                ? "Сохранение..."
                : "Изменить почту"}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

export function OrderHistorySection() {
  const role = useAuthStore((state) => state.role);
  const isDriver = String(role ?? "").toLowerCase() === "driver";
  const query = useMyTripsQuery(false);
  const createLocalTripMutation = useCreateLocalTripMutation();

  const groupedTrips = useMemo(() => {
    const items = query.data ?? [];
    const groups = new Map();

    for (const trip of items) {
      const source = trip.departureAt ?? trip.createdAt;
      const date = source ? new Date(source) : null;
      const key = date ? date.toLocaleDateString() : "Без даты";

      if (!groups.has(key)) {
        groups.set(key, []);
      }

      groups.get(key).push(trip);
    }

    return Array.from(groups.entries()).sort((left, right) => {
      const leftDate = Date.parse(
        left[1][0]?.departureAt ?? left[1][0]?.createdAt ?? 0,
      );
      const rightDate = Date.parse(
        right[1][0]?.departureAt ?? right[1][0]?.createdAt ?? 0,
      );
      return rightDate - leftDate;
    });
  }, [query.data]);

  return (
    <Card>
      <CardHeader>
        <CardTitle>История заказов</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {query.isError && (
          <Alert variant="destructive">
            {query.error?.message ?? "Не удалось загрузить историю."}
          </Alert>
        )}

        {createLocalTripMutation.isError && (
          <Alert variant="destructive">
            {createLocalTripMutation.error?.message ??
              "Не удалось повторить поездку."}
          </Alert>
        )}

        {createLocalTripMutation.isSuccess && (
          <Alert>Заявка успешно создана.</Alert>
        )}

        {query.isLoading && (
          <p className="text-sm text-muted-foreground">Загрузка истории...</p>
        )}

        {!query.isLoading && groupedTrips.length === 0 && (
          <p className="text-sm text-muted-foreground">История пока пустая.</p>
        )}

        {groupedTrips.map(([date, trips]) => (
          <div key={date} className="space-y-2 rounded-lg border p-3">
            <p className="text-sm font-medium">{date}</p>
            <div className="space-y-2">
              {trips.map((trip) => (
                <div
                  key={trip.tripId}
                  className="rounded-md border bg-muted/40 p-2"
                >
                  <div className="flex items-start justify-between gap-2">
                    <p className="text-xs font-medium">
                      {formatHistoryTripTitle(trip)}
                    </p>
                    <span
                      className={`text-xs font-medium ${getHistoryStatusClassName(trip.status)}`}
                    >
                      {getHistoryStatusLabel(trip.status)}
                    </span>
                  </div>

                  <p className="text-xs text-muted-foreground">
                    Откуда: {trip.fromAddress || "—"}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    Куда: {trip.toAddress || "—"}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    Время:{" "}
                    {formatHistoryDateTime(trip.departureAt ?? trip.createdAt)}
                  </p>

                  {trip.status === "DriverAccepted" &&
                    hasDriverCarInfo(trip) && (
                      <div className="mt-1 flex items-center gap-2 text-xs text-muted-foreground">
                        <Car
                          className="h-3.5 w-3.5"
                          style={{
                            color: toValidHexColorOrNull(trip.driverCarColor),
                            fill: toValidHexColorOrNull(trip.driverCarColor),
                          }}
                        />
                        <span>
                          Авто: {trip.driverCarBrand || ""}{" "}
                          {trip.driverCarModel || ""}
                          {trip.driverCarPlateNumber
                            ? ` • ${trip.driverCarPlateNumber}`
                            : ""}
                        </span>
                      </div>
                    )}

                  <div className="mt-2 flex items-center justify-end gap-2">
                    {buildHistoryRouteMapHref(trip) && (
                      <a
                        href={buildHistoryRouteMapHref(trip)}
                        target="_blank"
                        rel="noreferrer"
                        className="inline-flex items-center text-xs text-primary underline-offset-4 hover:underline"
                      >
                        <MapPin className="mr-1 h-3.5 w-3.5" />
                        Маршрут
                      </a>
                    )}

                    {Boolean(getHistoryCounterpartyPhone(role, trip)) && (
                      <Button
                        asChild
                        type="button"
                        variant="outline"
                        size="sm"
                        className="h-8 px-2"
                      >
                        <a
                          href={`tel:${getHistoryCounterpartyPhone(role, trip)}`}
                        >
                          Позвонить
                        </a>
                      </Button>
                    )}

                    {!isDriver &&
                      trip.tripType === "Local" &&
                      canRepeatLocalTrip(trip) && (
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          className="h-8 px-2"
                          disabled={createLocalTripMutation.isPending}
                          onClick={() =>
                            createLocalTripMutation.mutate({
                              from: {
                                latitude: trip.originLatitude,
                                longitude: trip.originLongitude,
                              },
                              to: {
                                latitude: trip.destinationLatitude,
                                longitude: trip.destinationLongitude,
                              },
                              fromAddress: trip.fromAddress,
                              toAddress: trip.toAddress,
                              description: null,
                            })
                          }
                        >
                          Повторить
                        </Button>
                      )}
                  </div>
                </div>
              ))}
            </div>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}

export function SavedLocationsSection() {
  const {
    locations,
    addLocation,
    removeLocation,
    isLoading,
    isError,
    error,
    isAdding,
    addError,
    isRemoving,
    removeError,
  } = useSavedLocations();

  const [name, setName] = useState("");
  const [address, setAddress] = useState("");
  const [latitude, setLatitude] = useState("");
  const [longitude, setLongitude] = useState("");
  const [message, setMessage] = useState("");

  const onSave = async (event) => {
    event.preventDefault();
    setMessage("");

    await addLocation({
      name,
      address,
      latitude,
      longitude,
    });

    setName("");
    setAddress("");
    setLatitude("");
    setLongitude("");
    setMessage("Локация сохранена.");
  };

  return (
    <div className="grid gap-4 md:grid-cols-2">
      <Card>
        <CardHeader>
          <CardTitle>Новая локация</CardTitle>
        </CardHeader>
        <CardContent>
          <form className="space-y-4" onSubmit={onSave}>
            {message && <Alert>{message}</Alert>}
            {addError && (
              <Alert variant="destructive">{addError.message}</Alert>
            )}

            <div className="space-y-2">
              <Label htmlFor="location-name">Название</Label>
              <Input
                id="location-name"
                value={name}
                onChange={(event) => setName(event.target.value)}
                placeholder="Дом, Работа, Университет"
                required
              />
            </div>

            <AddressAutocompleteInput
              id="saved-location-address"
              label="Адрес"
              value={address}
              onChange={setAddress}
              onSelect={(location) => {
                setLatitude(String(location.latitude));
                setLongitude(String(location.longitude));
              }}
              placeholder="Введите адрес"
              required
            />

            <div className="grid gap-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="saved-location-lat">Широта</Label>
                <Input
                  id="saved-location-lat"
                  type="number"
                  step="any"
                  value={latitude}
                  onChange={(event) => setLatitude(event.target.value)}
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="saved-location-lon">Долгота</Label>
                <Input
                  id="saved-location-lon"
                  type="number"
                  step="any"
                  value={longitude}
                  onChange={(event) => setLongitude(event.target.value)}
                  required
                />
              </div>
            </div>

            <Button disabled={isAdding} type="submit">
              {isAdding ? "Сохранение..." : "Сохранить локацию"}
            </Button>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Сохранённые локации</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          {isError && (
            <Alert variant="destructive">
              {error?.message ?? "Не удалось загрузить локации."}
            </Alert>
          )}
          {removeError && (
            <Alert variant="destructive">{removeError.message}</Alert>
          )}
          {isLoading && (
            <p className="text-sm text-muted-foreground">Загрузка локаций...</p>
          )}

          {!locations.length && (
            <p className="text-sm text-muted-foreground">
              Пока нет сохранённых локаций.
            </p>
          )}

          {locations.map((location) => (
            <div key={location.id} className="rounded-md border p-3">
              <p className="text-sm font-medium">{location.name}</p>
              <p className="text-xs text-muted-foreground">
                {location.address}
              </p>
              <p className="text-xs text-muted-foreground">
                {location.latitude}, {location.longitude}
              </p>
              <div className="mt-2">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  disabled={isRemoving}
                  onClick={() => removeLocation(location.id)}
                >
                  Удалить
                </Button>
              </div>
            </div>
          ))}
        </CardContent>
      </Card>
    </div>
  );
}

function getHistoryStatusLabel(status) {
  if (status === "Created") return "Создан";
  if (status === "DriverAccepted") return "Принят";
  if (status === "Completed") return "Завершен";
  if (status === "CancelledByDriver") return "Отменен водителем";
  if (status === "CancelledByPassenger") return "Отменен пассажиром";
  return "Отменен";
}

function getHistoryStatusClassName(status) {
  if (status === "Created") return "text-yellow-600";
  if (status === "DriverAccepted") return "text-green-600";
  if (status === "Completed") return "text-blue-600";
  return "text-red-600";
}

function formatHistoryDateTime(value) {
  if (!value) {
    return "—";
  }

  return new Date(value).toLocaleString();
}

function toValidHexColorOrNull(value) {
  const normalized = String(value ?? "").trim();
  if (/^#[0-9A-Fa-f]{6}$/.test(normalized)) {
    return normalized;
  }

  return undefined;
}

function hasDriverCarInfo(trip) {
  return Boolean(
    String(trip.driverCarBrand ?? "").trim() ||
    String(trip.driverCarModel ?? "").trim() ||
    String(trip.driverCarPlateNumber ?? "").trim(),
  );
}

function buildHistoryRouteMapHref(trip) {
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

function getHistoryCounterpartyPhone(role, trip) {
  const isDriver = String(role ?? "").toLowerCase() === "driver";
  const source = isDriver ? trip.passengerPhone : trip.driverPhone;
  return String(source ?? "").trim();
}

function canRepeatLocalTrip(trip) {
  return (
    typeof trip.originLatitude === "number" &&
    typeof trip.originLongitude === "number" &&
    typeof trip.destinationLatitude === "number" &&
    typeof trip.destinationLongitude === "number" &&
    Boolean(String(trip.fromAddress ?? "").trim()) &&
    Boolean(String(trip.toAddress ?? "").trim())
  );
}

function formatHistoryTripTitle(trip) {
  const shortId = String(trip.tripId).slice(0, 6);
  if (trip.tripType === "Intercity") {
    return `Межгород №${shortId}`;
  }

  return `Местная заявка №${shortId}`;
}
