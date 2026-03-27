import { ChevronDown } from "lucide-react";
import { useMemo, useState } from "react";
import { useSettingsStore } from "@/features/settings/model/use-settings-store";
import { cn } from "@/shared/lib/cn";

const MODE_LABELS = {
  local: "Местные",
  intercity: "Межгород",
};

export function TripModeSwitcher() {
  const tripMode = useSettingsStore((state) => state.tripMode);
  const setTripMode = useSettingsStore((state) => state.setTripMode);
  const [isOpen, setIsOpen] = useState(false);

  const secondary = useMemo(
    () => (tripMode === "local" ? "intercity" : "local"),
    [tripMode],
  );

  const switchMode = (mode) => {
    setTripMode(mode);
    setIsOpen(false);
  };

  return (
    <div className="relative w-full max-w-xs">
      <button
        type="button"
        className="flex w-full items-center justify-between rounded-xl border bg-card px-4 py-3 text-left"
        onClick={() => setIsOpen((prev) => !prev)}
      >
        <span className="flex items-center gap-2">
          <span className="rounded-md bg-primary px-2 py-1 text-xs font-medium text-primary-foreground">
            {MODE_LABELS[tripMode]}
          </span>
          <span className="text-xs text-muted-foreground">
            {MODE_LABELS[secondary]}
          </span>
        </span>
        <ChevronDown className="size-4 text-muted-foreground" />
      </button>

      <div
        className={cn(
          "absolute bottom-full left-0 mb-2 hidden w-full rounded-xl border bg-popover p-2 shadow-sm",
          isOpen && "block",
        )}
      >
        <button
          type="button"
          className={cn(
            "mb-1 w-full rounded-md px-3 py-2 text-left text-sm hover:bg-accent",
            tripMode === "local" && "bg-accent",
          )}
          onClick={() => switchMode("local")}
        >
          Местные
        </button>
        <button
          type="button"
          className={cn(
            "w-full rounded-md px-3 py-2 text-left text-sm hover:bg-accent",
            tripMode === "intercity" && "bg-accent",
          )}
          onClick={() => switchMode("intercity")}
        >
          Межгород
        </button>
      </div>
    </div>
  );
}
