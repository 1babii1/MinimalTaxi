import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  createSavedLocation,
  deleteSavedLocation,
  getSavedLocations,
} from "@/entities/locations/api/locations-api";

const SAVED_LOCATIONS_QUERY_KEY = ["saved-locations"];

export function useSavedLocations() {
  const queryClient = useQueryClient();

  const locationsQuery = useQuery({
    queryKey: SAVED_LOCATIONS_QUERY_KEY,
    queryFn: getSavedLocations,
  });

  const createMutation = useMutation({
    mutationFn: createSavedLocation,
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: SAVED_LOCATIONS_QUERY_KEY,
      });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: deleteSavedLocation,
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: SAVED_LOCATIONS_QUERY_KEY,
      });
    },
  });

  return {
    locations: locationsQuery.data ?? [],
    isLoading: locationsQuery.isLoading,
    isError: locationsQuery.isError,
    error: locationsQuery.error,
    addLocation: async (location) => {
      const latitude = Number(location.latitude);
      const longitude = Number(location.longitude);

      if (!Number.isFinite(latitude) || !Number.isFinite(longitude)) {
        throw new Error("Некорректные координаты");
      }

      await createMutation.mutateAsync({
        name: String(location.name ?? "").trim(),
        address: String(location.address ?? "").trim(),
        latitude,
        longitude,
      });
    },
    removeLocation: async (id) => {
      await deleteMutation.mutateAsync(id);
    },
    isAdding: createMutation.isPending,
    addError: createMutation.error,
    isRemoving: deleteMutation.isPending,
    removeError: deleteMutation.error,
  };
}
