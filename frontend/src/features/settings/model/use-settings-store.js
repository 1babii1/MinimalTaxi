import { create } from "zustand";
import { persist } from "zustand/middleware";

export const useSettingsStore = create(
  persist(
    (set) => ({
      theme: "system",
      authTab: "login",
      tripMode: "local",
      setTheme: (theme) => set({ theme }),
      setAuthTab: (authTab) => set({ authTab }),
      setTripMode: (tripMode) => set({ tripMode }),
    }),
    {
      name: "minimal-taxi-settings",
      partialize: (state) => ({
        theme: state.theme,
        authTab: state.authTab,
        tripMode: state.tripMode,
      }),
    },
  ),
);
