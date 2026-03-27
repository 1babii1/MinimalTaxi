import { taxiGet } from "@/shared/api/http";

export async function getAddressSuggestions(query, limit = 5) {
  const normalized = String(query ?? "").trim();
  if (normalized.length < 3) {
    return [];
  }

  const params = new URLSearchParams();
  params.set("query", normalized);
  params.set("limit", String(limit));

  const payload = await taxiGet(`/geocoding/suggest?${params.toString()}`);
  return payload?.result ?? [];
}

export async function getAddressByCoordinates(latitude, longitude) {
  const params = new URLSearchParams();
  params.set("latitude", String(latitude));
  params.set("longitude", String(longitude));

  const payload = await taxiGet(`/geocoding/reverse?${params.toString()}`);
  return payload?.result?.address ?? null;
}

export async function getCarBrandSuggestions(query, limit = 8) {
  const normalized = String(query ?? "").trim();
  if (normalized.length < 1) {
    return [];
  }

  const params = new URLSearchParams();
  params.set("query", normalized);
  params.set("limit", String(limit));

  const payload = await taxiGet(`/geocoding/cars/brands?${params.toString()}`);
  return payload?.result ?? [];
}

export async function getCarModelSuggestions(brand, query, limit = 8) {
  const normalizedBrand = String(brand ?? "").trim();
  const normalizedQuery = String(query ?? "").trim();

  if (!normalizedBrand || normalizedQuery.length < 1) {
    return [];
  }

  const params = new URLSearchParams();
  params.set("brand", normalizedBrand);
  params.set("query", normalizedQuery);
  params.set("limit", String(limit));

  const payload = await taxiGet(`/geocoding/cars/models?${params.toString()}`);
  return payload?.result ?? [];
}
