import { authPost } from "@/shared/api/http";

export function register(payload) {
  return authPost("/auth/register", payload);
}

export function login(payload) {
  return authPost("/auth/login", payload);
}

export function confirmEmail(payload) {
  return authPost("/auth/confirm-email", payload);
}

export function forgotPassword(payload) {
  return authPost("/auth/forgot-password", payload);
}

export function resetPassword(payload) {
  return authPost("/auth/reset-password", payload);
}

export function changePassword(payload) {
  return authPost("/auth/change-password", payload);
}

export function changeEmail(payload) {
  return authPost("/auth/change-email", payload);
}
