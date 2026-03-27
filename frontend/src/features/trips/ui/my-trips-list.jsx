import { useState } from "react";
import {
  useCancelTripMutation,
  useCompleteTripMutation,
  useIntercityPassengersQuery,
  useMyTripsQuery,
} from "@/entities/trips/model/use-trips";
import { useAuthStore } from "@/entities/auth/model/use-auth-store";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/shared/ui/card";
import { Input } from "@/shared/ui/input";

export function MyTripsList({ tripType }) {
  const userId = useAuthStore((state) => state.userId);
  const [onlyActive, setOnlyActive] = useState(true);
  const [reason, setReason] = useState("User cancelled");
  const [dateSort, setDateSort] = useState("asc");

  const myTripsQuery = useMyTripsQuery(onlyActive);
  const cancelMutation = useCancelTripMutation();
  const completeMutation = useCompleteTripMutation();

  const filteredTrips = (myTripsQuery.data ?? []).filter(
    (trip) => !tripType || trip.tripType === tripType,
  );

  const sortedTrips =
    tripType === "Intercity"
      ? [...filteredTrips].sort((left, right) => {
          const leftTime = Date.parse(left.departureAt ?? left.createdAt ?? 0);
          const rightTime = Date.parse(
            right.departureAt ?? right.createdAt ?? 0,
          );
          return dateSort === "desc"
            ? rightTime - leftTime
            : leftTime - rightTime;
        })
      : filteredTrips;

  return (
    <Card>
      <CardHeader>
        <CardTitle>My Trips</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {(myTripsQuery.isError ||
          cancelMutation.isError ||
          completeMutation.isError) && (
          <Alert variant="destructive">
            {myTripsQuery.error?.message ||
              cancelMutation.error?.message ||
              completeMutation.error?.message}
          </Alert>
        )}

        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={onlyActive}
            onChange={(event) => setOnlyActive(event.target.checked)}
          />
          Only active
        </label>

        <div className="grid gap-3 md:grid-cols-2">
          <Input
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            placeholder="Cancel reason"
          />

          {tripType === "Intercity" && (
            <select
              className="h-9 w-full rounded-md border bg-transparent px-3 text-sm"
              value={dateSort}
              onChange={(event) => setDateSort(event.target.value)}
            >
              <option value="asc">Nearest departure first</option>
              <option value="desc">Latest departure first</option>
            </select>
          )}
        </div>

        <div className="space-y-3">
          {myTripsQuery.isLoading ? (
            <p className="text-sm text-muted-foreground">Loading my trips...</p>
          ) : (
            sortedTrips.map((trip) => (
              <div
                key={trip.tripId}
                className="rounded-lg border bg-muted/40 p-3"
              >
                <div className="mb-2 flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium">{trip.tripType}</p>
                    <p className="text-xs text-muted-foreground">
                      {trip.city || "No city"}
                    </p>
                  </div>
                  <p className="text-xs text-muted-foreground">{trip.status}</p>
                </div>

                {trip.tripType === "Intercity" && (
                  <p className="mb-2 text-xs text-muted-foreground">
                    Departure:{" "}
                    {trip.departureAt
                      ? new Date(trip.departureAt).toLocaleString()
                      : "—"}
                  </p>
                )}

                {trip.tripType === "Intercity" && trip.driverId === userId && (
                  <div className="mb-3">
                    <DriverPassengers tripId={trip.tripId} enabled />
                  </div>
                )}

                <div className="flex flex-wrap gap-2">
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() =>
                      cancelMutation.mutate({
                        id: trip.tripId,
                        reason,
                      })
                    }
                  >
                    Cancel
                  </Button>

                  {trip.status === "DriverAccepted" &&
                    trip.driverId === userId && (
                      <Button
                        size="sm"
                        disabled={completeMutation.isPending}
                        onClick={() =>
                          completeMutation.mutate({ id: trip.tripId })
                        }
                      >
                        Complete
                      </Button>
                    )}
                </div>
              </div>
            ))
          )}

          {!myTripsQuery.isLoading && sortedTrips.length === 0 && (
            <p className="text-sm text-muted-foreground">Trips not found.</p>
          )}
        </div>
      </CardContent>
    </Card>
  );
}

function DriverPassengers({ tripId, enabled }) {
  const query = useIntercityPassengersQuery(tripId, enabled);

  if (!enabled) {
    return null;
  }

  if (query.isLoading) {
    return (
      <p className="text-xs text-muted-foreground">Loading passengers...</p>
    );
  }

  if (query.isError) {
    return (
      <p className="text-xs text-destructive">
        {query.error?.message ?? "Failed to load passengers"}
      </p>
    );
  }

  if (!query.data?.length) {
    return <p className="text-xs text-muted-foreground">No passengers yet.</p>;
  }

  return (
    <div className="space-y-2">
      {query.data.map((passenger) => (
        <div
          key={passenger.userId}
          className="rounded-md border bg-background/60 p-2 text-xs"
        >
          <p className="font-medium">
            {passenger.name} ({passenger.seats} seats)
          </p>
          <p className="text-muted-foreground">
            From: {passenger.pickupAddress || "—"}
          </p>
          <p className="text-muted-foreground">
            To: {passenger.dropoffAddress || "—"}
          </p>
          <p className="text-muted-foreground">
            Distance to pickup:{" "}
            {typeof passenger.distanceMetersFromOrigin === "number"
              ? `${(passenger.distanceMetersFromOrigin / 1000).toFixed(2)} km`
              : "—"}
          </p>
        </div>
      ))}
    </div>
  );
}
