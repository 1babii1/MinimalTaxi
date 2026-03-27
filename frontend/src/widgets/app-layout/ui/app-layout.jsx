import { useMemo, useState } from "react";
import { Link, useLocation } from "react-router-dom";
import { CarFront, Route, Settings } from "lucide-react";
import { AcceptedRequestsFloatingButton } from "@/features/trips/ui/accepted-requests-floating-button";
import { IntercityInvitationsFloatingButton } from "@/features/trips/ui/intercity-invitations-floating-button";
import { OnboardingWalkthrough } from "@/features/onboarding/ui/onboarding-walkthrough";
import { ThemeToggle } from "@/features/settings/ui/theme-toggle";
import { useAuthStore } from "@/entities/auth/model/use-auth-store";
import { authPost } from "@/shared/api/http";
import { Button } from "@/shared/ui/button";
import { cn } from "@/shared/lib/cn";

export function AppLayout({ children }) {
  const location = useLocation();
  const clearAuth = useAuthStore((state) => state.clearAuth);
  const userId = useAuthStore((state) => state.userId);
  const email = useAuthStore((state) => state.email);
  const [dismissedOnboardingByKey, setDismissedOnboardingByKey] = useState({});
  const avatarFallback = (email || "U").slice(0, 1).toUpperCase();

  const onboardingStorageKey = useMemo(() => {
    if (!userId) {
      return null;
    }

    return `mt_onboarding_seen_${userId}`;
  }, [userId]);

  const isOnboardingOpen = useMemo(() => {
    if (!onboardingStorageKey) {
      return false;
    }

    if (dismissedOnboardingByKey[onboardingStorageKey]) {
      return false;
    }

    return window.localStorage.getItem(onboardingStorageKey) !== "1";
  }, [dismissedOnboardingByKey, onboardingStorageKey]);

  const finishOnboarding = () => {
    if (onboardingStorageKey) {
      window.localStorage.setItem(onboardingStorageKey, "1");
      setDismissedOnboardingByKey((prev) => ({
        ...prev,
        [onboardingStorageKey]: true,
      }));
    }
  };

  return (
    <main className="mx-auto flex min-h-screen w-full max-w-[480px] flex-col bg-muted/30">
      <header className="sticky top-0 z-10 mb-4 border-b bg-background px-4 py-3">
        <div className="grid grid-cols-3 items-center gap-2">
          <Link
            to="/app/settings"
            className="inline-flex h-9 w-9 items-center justify-center rounded-full border text-sm font-medium"
            aria-label="Профиль"
          >
            {avatarFallback}
          </Link>

          <h1 className="text-center text-base font-semibold">MinimalTaxi</h1>

          <div className="justify-self-end">
            <div className="flex items-center gap-2">
              <ThemeToggle />
              <Button
                variant="outline"
                size="sm"
                onClick={async () => {
                  try {
                    await authPost("/auth/logout");
                  } catch (logoutError) {
                    void logoutError;
                  }

                  clearAuth();
                  window.location.assign("/auth/login");
                }}
              >
                Logout
              </Button>
            </div>
          </div>
        </div>
      </header>

      <section className="mb-24 flex-1 px-4">{children}</section>
      <AcceptedRequestsFloatingButton />
      <IntercityInvitationsFloatingButton />
      {isOnboardingOpen && (
        <OnboardingWalkthrough onFinish={finishOnboarding} />
      )}

      <nav className="fixed bottom-0 left-1/2 z-10 flex w-full max-w-[480px] -translate-x-1/2 items-center justify-between gap-2 border-t bg-background px-3 py-2">
        <div className="grid w-full grid-cols-3 gap-2">
          <Link
            className={cn(
              "inline-flex flex-col items-center justify-center rounded-md px-3 py-2 text-xs",
              location.pathname.startsWith("/app/trips/local")
                ? "bg-primary text-primary-foreground"
                : "text-muted-foreground",
            )}
            to="/app/trips/local"
          >
            <CarFront className="mb-1 h-4 w-4" />
            Местные
          </Link>

          <Link
            className={cn(
              "inline-flex flex-col items-center justify-center rounded-md px-3 py-2 text-xs",
              location.pathname.startsWith("/app/trips/intercity")
                ? "bg-primary text-primary-foreground"
                : "text-muted-foreground",
            )}
            to="/app/trips/intercity"
          >
            <Route className="mb-1 h-4 w-4" />
            Межгород
          </Link>

          <Link
            className={cn(
              "inline-flex flex-col items-center justify-center rounded-md px-3 py-2 text-xs",
              location.pathname.startsWith("/app/settings")
                ? "bg-primary text-primary-foreground"
                : "text-muted-foreground",
            )}
            to="/app/settings"
          >
            <Settings className="mb-1 h-4 w-4" />
            Настройки
          </Link>
        </div>
      </nav>
    </main>
  );
}
