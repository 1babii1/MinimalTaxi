import { create } from "zustand";
import { persist } from "zustand/middleware";

export const useLocalTripsFiltersStore = create(
  persist(
    (set) => ({
      onlyActive: true,
      onlyMine: false,
      fromAddress: "",
      toAddress: "",
      radiusMeters: 50000,
      sortKeys: ["newest"],
      setOnlyActive: (onlyActive) => set({ onlyActive }),
      setOnlyMine: (onlyMine) => set({ onlyMine }),
      setFromAddress: (fromAddress) => set({ fromAddress }),
      setToAddress: (toAddress) => set({ toAddress }),
      setRadiusMeters: (radiusMeters) => set({ radiusMeters }),
      addSortKey: (sortKey) =>
        set((state) => {
          if (state.sortKeys.includes(sortKey)) {
            return state;
          }

          return { sortKeys: [...state.sortKeys, sortKey] };
        }),
      setSortKeys: (sortKeys) => set({ sortKeys }),
      removeSortKey: (sortKey) =>
        set((state) => ({
          sortKeys: state.sortKeys.filter((item) => item !== sortKey),
        })),
      resetSortKeys: () => set({ sortKeys: ["newest"] }),
      resetAdvancedFilters: () =>
        set({
          fromAddress: "",
          toAddress: "",
          radiusMeters: 50000,
        }),
    }),
    {
      name: "minimal-taxi-local-filters",
      partialize: (state) => ({
        onlyActive: state.onlyActive,
        onlyMine: state.onlyMine,
        fromAddress: state.fromAddress,
        toAddress: state.toAddress,
        radiusMeters: state.radiusMeters,
        sortKeys: state.sortKeys,
      }),
    },
  ),
);
