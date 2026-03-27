import {
  useInfiniteQuery,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  acceptTrip,
  acceptIntercityInvitation,
  cancelTrip,
  completeTrip,
  createIntercityInvitation,
  createIntercityTrip,
  declineIntercityInvitation,
  createLocalTrip,
  getIntercityPassengers,
  getMyIntercityInvitations,
  getMyTrips,
  getNearbyTrips,
  joinIntercityTrip,
  leaveIntercityTrip,
  removeIntercityParticipant,
} from "@/entities/trips/api/trips-api";

export function useMyTripsQuery(onlyActive, enabled = true) {
  return useQuery({
    queryKey: ["my-trips", onlyActive],
    queryFn: () => getMyTrips(onlyActive),
    enabled,
  });
}

export function useNearbyTripsQuery(params) {
  return useQuery({
    queryKey: ["nearby-trips", params],
    queryFn: () => getNearbyTrips(params),
    enabled:
      typeof params.latitude === "number" &&
      typeof params.longitude === "number" &&
      Number.isFinite(params.latitude) &&
      Number.isFinite(params.longitude) &&
      params.radiusMeters > 0,
  });
}

export function useInfiniteNearbyTripsQuery(params) {
  return useInfiniteQuery({
    queryKey: ["nearby-trips-infinite", params],
    queryFn: ({ pageParam = 0 }) =>
      getNearbyTrips({
        ...params,
        offset: pageParam,
        limit: 20,
      }),
    initialPageParam: 0,
    getNextPageParam: (lastPage, allPages) => {
      if (!Array.isArray(lastPage) || lastPage.length < 20) {
        return undefined;
      }

      return allPages.length * 20;
    },
    enabled:
      typeof params.latitude === "number" &&
      typeof params.longitude === "number" &&
      Number.isFinite(params.latitude) &&
      Number.isFinite(params.longitude) &&
      params.radiusMeters > 0,
  });
}

export function useCreateLocalTripMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createLocalTrip,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["my-trips"] });
      await queryClient.invalidateQueries({ queryKey: ["nearby-trips"] });
      await queryClient.invalidateQueries({
        queryKey: ["nearby-trips-infinite"],
      });
    },
  });
}

export function useCreateIntercityTripMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createIntercityTrip,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["my-trips"] });
      await queryClient.invalidateQueries({ queryKey: ["nearby-trips"] });
      await queryClient.invalidateQueries({
        queryKey: ["nearby-trips-infinite"],
      });
    },
  });
}

export function useAcceptTripMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, totalSeats }) => acceptTrip(id, totalSeats),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["my-trips"] });
      await queryClient.invalidateQueries({ queryKey: ["nearby-trips"] });
      await queryClient.invalidateQueries({
        queryKey: ["nearby-trips-infinite"],
      });
    },
  });
}

export function useCancelTripMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, reason }) => cancelTrip(id, reason),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["my-trips"] });
      await queryClient.invalidateQueries({ queryKey: ["nearby-trips"] });
    },
  });
}

export function useJoinIntercityTripMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, payload }) => joinIntercityTrip(id, payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["my-trips"] });
      await queryClient.invalidateQueries({ queryKey: ["nearby-trips"] });
      await queryClient.invalidateQueries({
        queryKey: ["nearby-trips-infinite"],
      });
      await queryClient.invalidateQueries({
        queryKey: ["intercity-passengers"],
      });
    },
  });
}

export function useIntercityPassengersQuery(tripId, enabled) {
  return useQuery({
    queryKey: ["intercity-passengers", tripId],
    queryFn: () => getIntercityPassengers(tripId),
    enabled: Boolean(tripId) && Boolean(enabled),
  });
}

export function useRemoveIntercityParticipantMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, participantUserId }) =>
      removeIntercityParticipant(id, participantUserId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["my-trips"] });
      await queryClient.invalidateQueries({ queryKey: ["nearby-trips"] });
      await queryClient.invalidateQueries({
        queryKey: ["nearby-trips-infinite"],
      });
      await queryClient.invalidateQueries({
        queryKey: ["intercity-passengers"],
      });
    },
  });
}

export function useLeaveIntercityTripMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id }) => leaveIntercityTrip(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["my-trips"] });
      await queryClient.invalidateQueries({ queryKey: ["nearby-trips"] });
      await queryClient.invalidateQueries({
        queryKey: ["nearby-trips-infinite"],
      });
      await queryClient.invalidateQueries({
        queryKey: ["intercity-passengers"],
      });
    },
  });
}

export function useCreateIntercityInvitationMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: createIntercityInvitation,
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["intercity-invitations"],
      });
      await queryClient.invalidateQueries({ queryKey: ["nearby-trips"] });
      await queryClient.invalidateQueries({
        queryKey: ["nearby-trips-infinite"],
      });
    },
  });
}

export function useMyIntercityInvitationsQuery(enabled = true) {
  return useQuery({
    queryKey: ["intercity-invitations"],
    queryFn: () => getMyIntercityInvitations(),
    enabled,
  });
}

export function useAcceptIntercityInvitationMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ invitationId }) => acceptIntercityInvitation(invitationId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["intercity-invitations"],
      });
      await queryClient.invalidateQueries({ queryKey: ["my-trips"] });
      await queryClient.invalidateQueries({ queryKey: ["nearby-trips"] });
      await queryClient.invalidateQueries({
        queryKey: ["nearby-trips-infinite"],
      });
      await queryClient.invalidateQueries({
        queryKey: ["intercity-passengers"],
      });
    },
  });
}

export function useDeclineIntercityInvitationMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ invitationId }) => declineIntercityInvitation(invitationId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["intercity-invitations"],
      });
      await queryClient.invalidateQueries({ queryKey: ["nearby-trips"] });
      await queryClient.invalidateQueries({
        queryKey: ["nearby-trips-infinite"],
      });
    },
  });
}

export function useCompleteTripMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id }) => completeTrip(id),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["my-trips"] });
      await queryClient.invalidateQueries({ queryKey: ["nearby-trips"] });
      await queryClient.invalidateQueries({
        queryKey: ["nearby-trips-infinite"],
      });
    },
  });
}
