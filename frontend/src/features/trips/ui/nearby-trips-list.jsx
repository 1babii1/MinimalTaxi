import { useMemo, useState } from "react";
import {
  useAcceptTripMutation,
  useCompleteTripMutation,
  useJoinIntercityTripMutation,
  useNearbyTripsQuery,
} from "@/entities/trips/model/use-trips";
import { useAuthStore } from "@/entities/auth/model/use-auth-store";
import { AddressAutocompleteInput } from "@/features/trips/ui/address-autocomplete-input";
import { GeolocationButton } from "@/features/trips/ui/geolocation-button";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/shared/ui/card";
import { Input } from "@/shared/ui/input";
import { Label } from "@/shared/ui/label";

export function NearbyTripsList({ tripType }) {
  const role = useAuthStore((state) => state.role);
  const userId = useAuthStore((state) => state.userId);
  const [latitude, setLatitude] = useState("0");
  const [longitude, setLongitude] = useState("0");
  const [radiusMeters, setRadiusMeters] = useState("50000");
  const [city, setCity] = useState("");
  const [fromAddress, setFromAddress] = useState("");
  const [toAddress, setToAddress] = useState("");
  const [seats, setSeats] = useState("1");
  const [pickupAddress, setPickupAddress] = useState("");
  const [dropoffAddress, setDropoffAddress] = useState("");
  const [pickupLatitude, setPickupLatitude] = useState("");
  const [pickupLongitude, setPickupLongitude] = useState("");
  const [dropoffLatitude, setDropoffLatitude] = useState("");
  const [dropoffLongitude, setDropoffLongitude] = useState("");
  const [dateSort, setDateSort] = useState("asc");

  const params = useMemo(
    () => ({
      latitude: Number(latitude),
      longitude: Number(longitude),
      radiusMeters: Number(radiusMeters || 0),
      city: city || null,
      fromAddress: fromAddress || null,
      toAddress: toAddress || null,
      tripType,
    }),
    [latitude, longitude, radiusMeters, city, fromAddress, toAddress, tripType],
  );

  const tripsQuery = useNearbyTripsQuery(params);
  const acceptMutation = useAcceptTripMutation();
  const completeMutation = useCompleteTripMutation();
  const joinMutation = useJoinIntercityTripMutation();

  const onAccept = async (tripId) => {
    await acceptMutation.mutateAsync({ id: tripId });
  };

  const onJoin = async (tripId) => {
    await joinMutation.mutateAsync({
      id: tripId,
      payload: {
        seats: Number(seats),
        pickup: {
          latitude: Number(pickupLatitude),
          longitude: Number(pickupLongitude),
        },
        dropoff: {
          latitude: Number(dropoffLatitude),
          longitude: Number(dropoffLongitude),
        },
        pickupAddress,
        dropoffAddress,
      },
    });
  };

  const onComplete = async (tripId) => {
    await completeMutation.mutateAsync({ id: tripId });
  };

  const sortedTrips = useMemo(() => {
    const source = tripsQuery.data ?? [];
    if (tripType !== "Intercity") {
      return source;
    }

    const direction = dateSort === "desc" ? -1 : 1;
    return [...source].sort((left, right) => {
      const leftTime = Date.parse(left.departureAt ?? left.createdAt ?? 0);
      const rightTime = Date.parse(right.departureAt ?? right.createdAt ?? 0);
      return (leftTime - rightTime) * direction;
    });
  }, [tripsQuery.data, tripType, dateSort]);

  return (
    <Card>
      <CardHeader>
        <CardTitle>Nearby Trips</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {(tripsQuery.isError ||
          acceptMutation.isError ||
          completeMutation.isError ||
          joinMutation.isError) && (
          <Alert variant="destructive">
            {tripsQuery.error?.message ||
              acceptMutation.error?.message ||
              completeMutation.error?.message ||
              joinMutation.error?.message ||
              "Не удалось загрузить nearby trips"}
          </Alert>
        )}

        <div className="grid gap-3 md:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="nearby-latitude">Latitude</Label>
            <Input
              id="nearby-latitude"
              type="number"
              step="any"
              value={latitude}
              onChange={(event) => setLatitude(event.target.value)}
              placeholder="0 = без гео-фильтра"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="nearby-longitude">Longitude</Label>
            <Input
              id="nearby-longitude"
              type="number"
              step="any"
              value={longitude}
              onChange={(event) => setLongitude(event.target.value)}
              placeholder="0 = без гео-фильтра"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="nearby-radius">Radius (meters)</Label>
            <Input
              id="nearby-radius"
              type="number"
              value={radiusMeters}
              onChange={(event) => setRadiusMeters(event.target.value)}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="nearby-city">City</Label>
            <Input
              id="nearby-city"
              value={city}
              onChange={(event) => setCity(event.target.value)}
              placeholder="Можно оставить пустым"
            />
          </div>

          {tripType === "Intercity" && (
            <>
              <div className="space-y-2">
                <Label htmlFor="nearby-from-address">From filter</Label>
                <Input
                  id="nearby-from-address"
                  value={fromAddress}
                  onChange={(event) => setFromAddress(event.target.value)}
                  placeholder="Например: Казань"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="nearby-to-address">To filter</Label>
                <Input
                  id="nearby-to-address"
                  value={toAddress}
                  onChange={(event) => setToAddress(event.target.value)}
                  placeholder="Например: Москва"
                />
              </div>
            </>
          )}

          {tripType === "Intercity" && (
            <div className="space-y-2">
              <Label htmlFor="nearby-date-sort">Date sort</Label>
              <select
                id="nearby-date-sort"
                className="h-9 w-full rounded-md border bg-transparent px-3 text-sm"
                value={dateSort}
                onChange={(event) => setDateSort(event.target.value)}
              >
                <option value="asc">Nearest departure first</option>
                <option value="desc">Latest departure first</option>
              </select>
            </div>
          )}

          {role === "Passenger" && (
            <>
              <div className="space-y-2">
                <Label htmlFor="nearby-join-seats">Seats for join</Label>
                <Input
                  id="nearby-join-seats"
                  type="number"
                  min="1"
                  value={seats}
                  onChange={(event) => setSeats(event.target.value)}
                />
              </div>

              <AddressAutocompleteInput
                id="nearby-pickup-address"
                label="Pickup address"
                value={pickupAddress}
                onChange={setPickupAddress}
                onSelect={(location) => {
                  setPickupLatitude(String(location.latitude));
                  setPickupLongitude(String(location.longitude));
                }}
                placeholder="Адрес, откуда забрать"
                required
              />

              <GeolocationButton
                label="Определить мою точку посадки"
                onLocationSelected={(location) => {
                  setPickupLatitude(String(location.latitude));
                  setPickupLongitude(String(location.longitude));
                }}
              />

              <AddressAutocompleteInput
                id="nearby-dropoff-address"
                label="Dropoff address"
                value={dropoffAddress}
                onChange={setDropoffAddress}
                onSelect={(location) => {
                  setDropoffLatitude(String(location.latitude));
                  setDropoffLongitude(String(location.longitude));
                }}
                placeholder="Адрес, куда отвезти"
                required
              />

              <div className="space-y-2">
                <Label htmlFor="nearby-pickup-lat">Pickup latitude</Label>
                <Input
                  id="nearby-pickup-lat"
                  type="number"
                  step="any"
                  value={pickupLatitude}
                  onChange={(event) => setPickupLatitude(event.target.value)}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="nearby-pickup-lon">Pickup longitude</Label>
                <Input
                  id="nearby-pickup-lon"
                  type="number"
                  step="any"
                  value={pickupLongitude}
                  onChange={(event) => setPickupLongitude(event.target.value)}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="nearby-dropoff-lat">Dropoff latitude</Label>
                <Input
                  id="nearby-dropoff-lat"
                  type="number"
                  step="any"
                  value={dropoffLatitude}
                  onChange={(event) => setDropoffLatitude(event.target.value)}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="nearby-dropoff-lon">Dropoff longitude</Label>
                <Input
                  id="nearby-dropoff-lon"
                  type="number"
                  step="any"
                  value={dropoffLongitude}
                  onChange={(event) => setDropoffLongitude(event.target.value)}
                />
              </div>
            </>
          )}
        </div>

        <div className="space-y-3">
          {tripsQuery.isLoading ? (
            <p className="text-sm text-muted-foreground">Loading trips...</p>
          ) : (
            sortedTrips.map((trip) =>
              (() => {
                const isIntercity = trip.tripType === "Intercity";
                const isPassengerRequest = Boolean(trip.isPassengerRequest);
                const cardClassName =
                  isIntercity && isPassengerRequest
                    ? "rounded-lg border border-primary/30 bg-primary/5 p-3"
                    : "rounded-lg border bg-muted/40 p-3";

                return (
                  <div key={trip.tripId} className={cardClassName}>
                    <div className="mb-2 flex items-center justify-between gap-3">
                      <div>
                        <p className="text-sm font-medium">{trip.tripType}</p>
                        <p className="text-xs text-muted-foreground">
                          {trip.city || "No city"}
                        </p>
                      </div>
                      <p className="text-xs text-muted-foreground">
                        {(trip.distanceMeters / 1000).toFixed(2)} km
                      </p>
                    </div>

                    <div className="mb-3 text-xs text-muted-foreground">
                      Status: {trip.status}
                    </div>

                    {trip.tripType === "Intercity" && (
                      <>
                        <div className="mb-1 text-xs text-muted-foreground">
                          Departure:{" "}
                          {trip.departureAt
                            ? new Date(trip.departureAt).toLocaleString()
                            : "—"}
                        </div>
                        <div className="mb-3 text-xs text-muted-foreground">
                          Type:{" "}
                          {trip.isPassengerRequest
                            ? "Passenger request (looking for driver)"
                            : "Driver offer (looking for passengers)"}
                        </div>
                      </>
                    )}

                    {role === "Driver" &&
                      trip.status === "Created" &&
                      (trip.tripType !== "Intercity" ||
                        trip.isPassengerRequest) && (
                        <Button
                          size="sm"
                          disabled={acceptMutation.isPending}
                          onClick={() => onAccept(trip.tripId)}
                        >
                          Accept
                        </Button>
                      )}

                    {role === "Driver" &&
                      trip.status === "DriverAccepted" &&
                      trip.driverId === userId && (
                        <Button
                          size="sm"
                          disabled={completeMutation.isPending}
                          onClick={() => onComplete(trip.tripId)}
                        >
                          Complete
                        </Button>
                      )}

                    {role === "Passenger" &&
                      trip.tripType === "Intercity" &&
                      !trip.isPassengerRequest && (
                        <Button
                          size="sm"
                          disabled={joinMutation.isPending}
                          onClick={() => onJoin(trip.tripId)}
                        >
                          Join
                        </Button>
                      )}

                    {role === "Passenger" &&
                      trip.tripType === "Intercity" &&
                      trip.isPassengerRequest && (
                        <p className="text-xs text-muted-foreground">
                          Нельзя присоединиться: это заявка пассажира на поиск
                          водителя.
                        </p>
                      )}
                  </div>
                );
              })(),
            )
          )}

          {!tripsQuery.isLoading && sortedTrips.length === 0 && (
            <p className="text-sm text-muted-foreground">
              Trips not found in selected radius.
            </p>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
