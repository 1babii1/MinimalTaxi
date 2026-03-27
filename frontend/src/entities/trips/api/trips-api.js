import { taxiGet, taxiPost } from "@/shared/api/http";

export async function createLocalTrip(body) {
  const payload = await taxiPost("/trips/local", {
    from: body.from,
    to: body.to,
    fromAddress: body.fromAddress,
    toAddress: body.toAddress,
    description: body.description,
  });
  return payload?.result;
}

export async function createIntercityTrip(body) {
  const payload = await taxiPost("/trips/intercity", body);
  return payload?.result;
}

export async function getNearbyTrips(params) {
  const query = new URLSearchParams();
  query.set("latitude", String(params.latitude));
  query.set("longitude", String(params.longitude));
  query.set("radiusMeters", String(params.radiusMeters));
  query.set("limit", String(params.limit ?? 20));
  query.set("offset", String(params.offset ?? 0));
  query.set("includeInactive", params.includeInactive ? "true" : "false");

  if (params.city) {
    query.set("city", params.city);
  }

  if (params.tripType) {
    query.set("tripType", params.tripType);
  }

  if (params.fromAddress) {
    query.set("fromAddress", params.fromAddress);
  }

  if (params.toAddress) {
    query.set("toAddress", params.toAddress);
  }

  if (typeof params.fromLatitude === "number") {
    query.set("fromLatitude", String(params.fromLatitude));
  }

  if (typeof params.fromLongitude === "number") {
    query.set("fromLongitude", String(params.fromLongitude));
  }

  if (typeof params.fromRadiusMeters === "number") {
    query.set("fromRadiusMeters", String(params.fromRadiusMeters));
  }

  if (typeof params.toLatitude === "number") {
    query.set("toLatitude", String(params.toLatitude));
  }

  if (typeof params.toLongitude === "number") {
    query.set("toLongitude", String(params.toLongitude));
  }

  if (typeof params.toRadiusMeters === "number") {
    query.set("toRadiusMeters", String(params.toRadiusMeters));
  }

  const payload = await taxiGet(`/trips/nearby?${query.toString()}`);
  return payload?.result ?? [];
}

export async function getMyTrips(onlyActive) {
  const payload = await taxiGet(
    `/trips/my?onlyActive=${onlyActive ? "true" : "false"}`,
  );
  return payload?.result ?? [];
}

export async function acceptTrip(id, totalSeats) {
  const payload = await taxiPost(`/trips/${id}/accept`, { totalSeats });
  return payload?.result;
}

export async function cancelTrip(id, reason) {
  const payload = await taxiPost(`/trips/${id}/cancel`, { reason });
  return payload?.result;
}

export async function completeTrip(id) {
  const payload = await taxiPost(`/trips/${id}/complete`, {});
  return payload?.result;
}

export async function joinIntercityTrip(id, seats) {
  const payload = await taxiPost(`/intercity/${id}/join`, seats);
  return payload?.result;
}

export async function getIntercityPassengers(id) {
  const payload = await taxiGet(`/intercity/${id}/passengers`);
  return payload?.result;
}

export async function removeIntercityParticipant(id, participantUserId) {
  const payload = await taxiPost(
    `/intercity/${id}/participants/${participantUserId}/remove`,
    {},
  );
  return payload?.result;
}

export async function leaveIntercityTrip(id) {
  const payload = await taxiPost(`/intercity/${id}/leave`, {});
  return payload?.result;
}

export async function createIntercityInvitation(body) {
  const payload = await taxiPost(`/intercity/invitations`, body);
  return payload?.result;
}

export async function getMyIntercityInvitations() {
  const payload = await taxiGet(`/intercity/invitations/my`);
  return payload?.result ?? [];
}

export async function acceptIntercityInvitation(invitationId) {
  const payload = await taxiPost(
    `/intercity/invitations/${invitationId}/accept`,
    {},
  );
  return payload?.result;
}

export async function declineIntercityInvitation(invitationId) {
  const payload = await taxiPost(
    `/intercity/invitations/${invitationId}/decline`,
    {},
  );
  return payload?.result;
}
