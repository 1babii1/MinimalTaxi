import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { useAuthStore } from "@/entities/auth/model/use-auth-store";

const TAXI_API_URL = import.meta.env.VITE_TAXI_API_URL ?? "/taxi";
const NOTIFICATIONS_PROMPT_KEY = "mt_notifications_prompted";

export function TripsEventsProvider({ children }) {
  const queryClient = useQueryClient();
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

  useEffect(() => {
    if (!isAuthenticated) {
      return;
    }

    if (typeof window === "undefined" || !("Notification" in window)) {
      return;
    }

    if (Notification.permission !== "default") {
      return;
    }

    if (window.localStorage.getItem(NOTIFICATIONS_PROMPT_KEY) === "1") {
      return;
    }

    window.localStorage.setItem(NOTIFICATIONS_PROMPT_KEY, "1");

    const allow = window.confirm(
      "Разрешить уведомления от сайта, чтобы получать сообщения об отмене заявок?",
    );

    if (allow) {
      void Notification.requestPermission();
    }
  }, [isAuthenticated]);

  useEffect(() => {
    if (!isAuthenticated) {
      return undefined;
    }

    const eventSource = new EventSource(`${TAXI_API_URL}/trips/events`, {
      withCredentials: true,
    });

    let timeoutId = null;

    const scheduleRefresh = () => {
      if (timeoutId) {
        return;
      }

      timeoutId = window.setTimeout(async () => {
        timeoutId = null;
        const previousTrips = collectMyTrips(queryClient);

        void queryClient.invalidateQueries({ queryKey: ["my-trips"] });
        void queryClient.invalidateQueries({ queryKey: ["nearby-trips"] });
        void queryClient.invalidateQueries({
          queryKey: ["nearby-trips-infinite"],
        });
        void queryClient.invalidateQueries({
          queryKey: ["intercity-passengers"],
        });
        void queryClient.invalidateQueries({
          queryKey: ["intercity-invitations"],
        });

        await queryClient.refetchQueries({ queryKey: ["my-trips"] });
        const nextTrips = collectMyTrips(queryClient);
        notifyAboutCancelledTrips(previousTrips, nextTrips);
      }, 250);
    };

    const handleTripsChanged = () => {
      scheduleRefresh();
    };

    const handleInvitationChanged = () => {
      void queryClient.invalidateQueries({
        queryKey: ["intercity-invitations"],
      });
    };

    eventSource.addEventListener("trips.changed", handleTripsChanged);
    eventSource.addEventListener(
      "intercity.invitations.changed",
      handleInvitationChanged,
    );

    return () => {
      if (timeoutId) {
        window.clearTimeout(timeoutId);
      }
      eventSource.removeEventListener("trips.changed", handleTripsChanged);
      eventSource.removeEventListener(
        "intercity.invitations.changed",
        handleInvitationChanged,
      );
      eventSource.close();
    };
  }, [isAuthenticated, queryClient]);

  return children;
}

function collectMyTrips(queryClient) {
  return queryClient
    .getQueriesData({ queryKey: ["my-trips"] })
    .flatMap(([, data]) => (Array.isArray(data) ? data : []));
}

function notifyAboutCancelledTrips(previousTrips, nextTrips) {
  if (typeof window === "undefined" || !("Notification" in window)) {
    return;
  }

  if (Notification.permission !== "granted") {
    return;
  }

  const previousById = new Map(
    previousTrips.map((trip) => [trip.tripId, trip]),
  );

  for (const trip of nextTrips) {
    const previous = previousById.get(trip.tripId);
    const wasCancelled = isCancelledStatus(previous?.status);
    const nowCancelled = isCancelledStatus(trip.status);

    if (!wasCancelled && nowCancelled) {
      const from = String(trip.fromAddress ?? "—");
      const to = String(trip.toAddress ?? "—");

      new Notification("Заявка отменена", {
        body: `${from} → ${to}`,
      });
    }
  }
}

function isCancelledStatus(status) {
  return (
    status === "Cancelled" ||
    status === "CancelledByPassenger" ||
    status === "CancelledByDriver"
  );
}
