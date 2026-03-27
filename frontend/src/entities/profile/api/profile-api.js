import { taxiGet, taxiPost, taxiPut } from "@/shared/api/http";

export async function getProfile() {
  const payload = await taxiGet("/profile");
  return payload?.result ?? null;
}

export async function updateProfile(body) {
  const payload = await taxiPut("/profile", body);
  return payload?.result ?? null;
}

export async function uploadProfileAvatar(file) {
  const formData = new FormData();
  formData.append("file", file);

  const payload = await taxiPost("/profile/avatar", formData);
  return payload?.result ?? null;
}
