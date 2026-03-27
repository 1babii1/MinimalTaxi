import { useAuthStore } from "@/entities/auth/model/use-auth-store";

const AUTH_API_URL = import.meta.env.VITE_AUTH_API_URL ?? "";
const TAXI_API_URL = import.meta.env.VITE_TAXI_API_URL ?? "/taxi";

function normalizeError(payload, fallback) {
  const message = payload?.message ?? payload?.result?.message ?? fallback;
  const errors = payload?.errors ?? payload?.errorList ?? [];

  const collected = [];

  if (Array.isArray(errors) && errors.length > 0) {
    for (const item of errors) {
      if (typeof item === "string") {
        collected.push(item);
        continue;
      }

      if (typeof item?.message === "string") {
        collected.push(item.message);
      }

      if (Array.isArray(item?.messages)) {
        for (const nested of item.messages) {
          if (typeof nested === "string") {
            collected.push(nested);
          }
        }
      }
    }
  }

  if (collected.length > 0) {
    return [message, ...collected].join(" ");
  }

  return message;
}

async function request(
  baseUrl,
  path,
  { method = "GET", body, auth = false, retryOnUnauthorized = true } = {},
) {
  const isFormData =
    typeof FormData !== "undefined" && body instanceof FormData;
  const headers = isFormData
    ? undefined
    : {
        "Content-Type": "application/json",
      };

  const response = await fetch(`${baseUrl}${path}`, {
    method,
    headers,
    credentials: "include",
    body: body ? (isFormData ? body : JSON.stringify(body)) : undefined,
  });

  const payload = await response
    .json()
    .catch(() => ({ message: "Unexpected response" }));

  if (!response.ok) {
    if (
      response.status === 401 &&
      auth &&
      retryOnUnauthorized &&
      !path.includes("/auth/refresh")
    ) {
      try {
        await request(AUTH_API_URL, "/auth/refresh", {
          method: "POST",
          auth: false,
          retryOnUnauthorized: false,
        });

        return request(baseUrl, path, {
          method,
          body,
          auth,
          retryOnUnauthorized: false,
        });
      } catch (refreshError) {
        void refreshError;
      }
    }

    if (response.status === 401 && auth) {
      useAuthStore.getState().clearAuth();
      if (window.location.pathname !== "/auth/login") {
        window.location.assign("/auth/login");
      }
    }

    throw new Error(normalizeError(payload, "Request failed"));
  }

  return payload;
}

export function authPost(path, body) {
  return request(AUTH_API_URL, path, { method: "POST", body, auth: false });
}

export function authGet(path) {
  return request(AUTH_API_URL, path, { method: "GET", auth: true });
}

export function taxiGet(path) {
  return request(TAXI_API_URL, path, { method: "GET", auth: true });
}

export function taxiPost(path, body) {
  return request(TAXI_API_URL, path, { method: "POST", body, auth: true });
}

export function taxiPut(path, body) {
  return request(TAXI_API_URL, path, { method: "PUT", body, auth: true });
}

export function taxiDelete(path) {
  return request(TAXI_API_URL, path, { method: "DELETE", auth: true });
}
