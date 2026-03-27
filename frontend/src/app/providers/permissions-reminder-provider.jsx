import { useCallback, useEffect, useRef } from "react";
import { create } from "zustand";
import { useAuthStore } from "@/entities/auth/model/use-auth-store";

const CHECK_INTERVAL_MS = 30_000;
const REMINDER_VISIBLE_MS = 5_000;

const usePermissionsReminderStore = create((set) => ({
  message: "",
  visible: false,
  show: (message) =>
    set({
      message,
      visible: true,
    }),
  hide: () =>
    set({
      visible: false,
    }),
}));

export function PermissionsReminderProvider({ children }) {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const visible = usePermissionsReminderStore((state) => state.visible);
  const message = usePermissionsReminderStore((state) => state.message);
  const checkAndRemindRef = useRef(async () => undefined);

  const requestPermissionsAgain = useCallback(async () => {
    if (typeof window === "undefined") {
      return;
    }

    if ("Notification" in window && Notification.permission === "default") {
      try {
        await Notification.requestPermission();
      } catch {
        // Ignore request errors from unsupported browsers.
      }
    }

    if ("geolocation" in navigator) {
      try {
        await new Promise((resolve) => {
          navigator.geolocation.getCurrentPosition(
            () => resolve(),
            () => resolve(),
            {
              enableHighAccuracy: false,
              timeout: 8000,
              maximumAge: 0,
            },
          );
        });
      } catch {
        // Geolocation flow resolves in callbacks; keep this as a safety net.
      }
    }

    await checkAndRemindRef.current();
  }, []);

  useEffect(() => {
    if (!isAuthenticated || typeof window === "undefined") {
      usePermissionsReminderStore.getState().hide();
      return undefined;
    }

    let hideTimeoutId = null;
    let intervalId = null;
    let geolocationPermissionStatus = null;
    let notificationsPermissionStatus = null;

    const hideReminderLater = () => {
      if (hideTimeoutId) {
        window.clearTimeout(hideTimeoutId);
      }

      hideTimeoutId = window.setTimeout(() => {
        usePermissionsReminderStore.getState().hide();
      }, REMINDER_VISIBLE_MS);
    };

    const showReminder = (nextMessage) => {
      usePermissionsReminderStore.getState().show(nextMessage);
      hideReminderLater();
    };

    const collectMissingPermissions = async () => {
      const missing = [];

      if ("Notification" in window && Notification.permission !== "granted") {
        missing.push("уведомления");
      }

      if (!("geolocation" in navigator)) {
        missing.push("геолокацию");
      } else if (
        "permissions" in navigator &&
        navigator.permissions &&
        typeof navigator.permissions.query === "function"
      ) {
        try {
          const geolocationStatus = await navigator.permissions.query({
            name: "geolocation",
          });

          if (geolocationStatus.state !== "granted") {
            missing.push("геолокацию");
          }
        } catch {
          // Ignore and fallback to geolocation usage flow only.
        }
      }

      return missing;
    };

    const checkAndRemind = async () => {
      if (document.visibilityState === "hidden") {
        return;
      }

      const missing = await collectMissingPermissions();
      if (missing.length === 0) {
        usePermissionsReminderStore.getState().hide();
        return;
      }

      const merged = Array.from(new Set(missing));
      const messageValue =
        merged.length === 1
          ? `Включите ${merged[0]} в браузере для корректной работы приложения.`
          : `Включите ${merged.join(" и ")} в браузере для корректной работы приложения.`;

      showReminder(messageValue);
    };

    checkAndRemindRef.current = checkAndRemind;

    const subscribePermissionChange = async () => {
      if (
        !("permissions" in navigator) ||
        !navigator.permissions ||
        typeof navigator.permissions.query !== "function"
      ) {
        return;
      }

      try {
        geolocationPermissionStatus = await navigator.permissions.query({
          name: "geolocation",
        });

        geolocationPermissionStatus.onchange = () => {
          void checkAndRemind();
        };
      } catch {
        geolocationPermissionStatus = null;
      }

      try {
        notificationsPermissionStatus = await navigator.permissions.query({
          name: "notifications",
        });

        notificationsPermissionStatus.onchange = () => {
          void checkAndRemind();
        };
      } catch {
        notificationsPermissionStatus = null;
      }
    };

    const handleVisibilityOrFocus = () => {
      void checkAndRemind();
    };

    void checkAndRemind();
    void subscribePermissionChange();

    intervalId = window.setInterval(() => {
      void checkAndRemind();
    }, CHECK_INTERVAL_MS);

    document.addEventListener("visibilitychange", handleVisibilityOrFocus);
    window.addEventListener("focus", handleVisibilityOrFocus);

    return () => {
      if (hideTimeoutId) {
        window.clearTimeout(hideTimeoutId);
      }

      if (intervalId) {
        window.clearInterval(intervalId);
      }

      if (geolocationPermissionStatus) {
        geolocationPermissionStatus.onchange = null;
      }

      if (notificationsPermissionStatus) {
        notificationsPermissionStatus.onchange = null;
      }

      document.removeEventListener("visibilitychange", handleVisibilityOrFocus);
      window.removeEventListener("focus", handleVisibilityOrFocus);

      checkAndRemindRef.current = async () => undefined;
    };
  }, [isAuthenticated]);

  return (
    <>
      {children}
      {visible && (
        <div className="fixed right-4 top-4 z-[120] max-w-[360px] rounded-md border border-amber-300 bg-amber-200/70 px-4 py-2 text-xs font-medium text-amber-950 shadow-lg backdrop-blur-sm">
          <p>{message}</p>
          <button
            type="button"
            className="mt-2 rounded-sm border border-amber-600/60 bg-amber-100/80 px-2 py-1 text-[11px] font-semibold text-amber-900 transition hover:bg-amber-50"
            onClick={() => {
              void requestPermissionsAgain();
            }}
          >
            Запросить разрешения снова
          </button>
        </div>
      )}
    </>
  );
}
