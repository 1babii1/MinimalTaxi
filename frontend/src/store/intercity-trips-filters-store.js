import { create } from "zustand";
import { persist } from "zustand/middleware";

export const useIntercityTripsFiltersStore = create(
  persist(
    (set) => ({
      onlyActive: true,
      onlyMine: false,
      fromAddress: "",
      toAddress: "",
      dateFilter: "",
      radiusMeters: 50000,
      sortKeys: ["newest"],
      setOnlyActive: (onlyActive) => set({ onlyActive }),
      setOnlyMine: (onlyMine) => set({ onlyMine }),
      setFromAddress: (fromAddress) => set({ fromAddress }),
      setToAddress: (toAddress) => set({ toAddress }),
      setDateFilter: (dateFilter) => set({ dateFilter }),
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
          dateFilter: "",
          radiusMeters: 50000,
        }),
    }),
    {
      name: "minimal-taxi-intercity-filters",
      partialize: (state) => ({
        onlyActive: state.onlyActive,
        onlyMine: state.onlyMine,
        fromAddress: state.fromAddress,
        toAddress: state.toAddress,
        dateFilter: state.dateFilter,
        radiusMeters: state.radiusMeters,
        sortKeys: state.sortKeys,
      }),
    },
  ),
);
