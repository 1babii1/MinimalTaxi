import { taxiDelete, taxiGet, taxiPost } from "@/shared/api/http";

export async function getSavedLocations() {
  const payload = await taxiGet("/profile/locations");
  return payload?.result ?? [];
}

export async function createSavedLocation(body) {
  const payload = await taxiPost("/profile/locations", body);
  return payload?.result ?? null;
}

export async function deleteSavedLocation(id) {
  await taxiDelete(`/profile/locations/${id}`);
}
