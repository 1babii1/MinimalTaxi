import { useMutation } from "@tanstack/react-query";
import {
  changeEmail,
  changePassword,
  confirmEmail,
  forgotPassword,
  login,
  register,
  resetPassword,
} from "@/entities/auth/api/auth-api";

export function useRegisterMutation() {
  return useMutation({ mutationFn: register });
}

export function useLoginMutation() {
  return useMutation({ mutationFn: login });
}

export function useConfirmEmailMutation() {
  return useMutation({ mutationFn: confirmEmail });
}

export function useForgotPasswordMutation() {
  return useMutation({ mutationFn: forgotPassword });
}

export function useResetPasswordMutation() {
  return useMutation({ mutationFn: resetPassword });
}

export function useChangePasswordMutation() {
  return useMutation({ mutationFn: changePassword });
}

export function useChangeEmailMutation() {
  return useMutation({ mutationFn: changeEmail });
}
