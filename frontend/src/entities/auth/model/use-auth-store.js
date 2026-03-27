import { create } from "zustand";
import { persist } from "zustand/middleware";

export const useAuthStore = create(
  persist(
    (set) => ({
      userId: null,
      email: null,
      role: null,
      isAuthenticated: false,
      setSession: (session) => {
        set({
          userId: session?.userId ?? null,
          email: session?.email ?? null,
          role: session?.role ?? null,
          isAuthenticated: Boolean(session?.isAuthenticated),
        });
      },
      clearAuth: () =>
        set({
          userId: null,
          email: null,
          role: null,
          isAuthenticated: false,
        }),
    }),
    {
      name: "minimal-taxi-auth",
      partialize: (state) => ({
        userId: state.userId,
        email: state.email,
        role: state.role,
        isAuthenticated: state.isAuthenticated,
      }),
    },
  ),
);
