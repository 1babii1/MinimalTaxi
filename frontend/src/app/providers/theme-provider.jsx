import { useEffect } from "react";
import { useSettingsStore } from "@/features/settings/model/use-settings-store";

function getSystemTheme() {
  if (window.matchMedia("(prefers-color-scheme: dark)").matches) {
    return "dark";
  }

  return "light";
}

export function ThemeProvider({ children }) {
  const theme = useSettingsStore((state) => state.theme);

  useEffect(() => {
    const root = document.documentElement;
    const activeTheme = theme === "system" ? getSystemTheme() : theme;

    root.classList.toggle("dark", activeTheme === "dark");
  }, [theme]);

  return children;
}
