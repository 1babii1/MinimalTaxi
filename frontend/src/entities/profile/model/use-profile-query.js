import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  getProfile,
  updateProfile,
  uploadProfileAvatar,
} from "@/entities/profile/api/profile-api";

const PROFILE_QUERY_KEY = ["profile"];

export function useProfileQuery() {
  return useQuery({
    queryKey: PROFILE_QUERY_KEY,
    queryFn: getProfile,
  });
}

export function useUpdateProfileMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: updateProfile,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: PROFILE_QUERY_KEY });
    },
  });
}

export function useUploadProfileAvatarMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: uploadProfileAvatar,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: PROFILE_QUERY_KEY });
    },
  });
}
