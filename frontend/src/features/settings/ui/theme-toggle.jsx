import { Moon, Sun } from "lucide-react";
import { useSettingsStore } from "@/features/settings/model/use-settings-store";
import { Button } from "@/shared/ui/button";

export function ThemeToggle() {
  const theme = useSettingsStore((state) => state.theme);
  const setTheme = useSettingsStore((state) => state.setTheme);

  const prefersDark =
    typeof window !== "undefined" &&
    window.matchMedia("(prefers-color-scheme: dark)").matches;

  const activeTheme =
    theme === "system" ? (prefersDark ? "dark" : "light") : theme;
  const nextTheme = activeTheme === "dark" ? "light" : "dark";

  return (
    <Button
      type="button"
      variant="outline"
      size="icon"
      onClick={() => setTheme(nextTheme)}
      title={`Current: ${activeTheme}. Switch to ${nextTheme}`}
      aria-label="Toggle theme"
    >
      {activeTheme === "dark" ? (
        <Moon className="size-4" />
      ) : (
        <Sun className="size-4" />
      )}
    </Button>
  );
}
