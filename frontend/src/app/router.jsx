import { useEffect, useState } from "react";
import { Navigate, Route, Routes, useLocation } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { useAuthStore } from "@/entities/auth/model/use-auth-store";
import { getProfile } from "@/entities/profile/api/profile-api";
import { authGet } from "@/shared/api/http";
import { AuthPage } from "@/pages/auth/auth-page";
import { ConfirmEmailPage } from "@/pages/auth/confirm-email-page";
import { ForgotPasswordPage } from "@/pages/auth/forgot-password-page";
import { ResetPasswordPage } from "@/pages/auth/reset-password-page";
import { IntercityTripsPage } from "@/pages/trips/intercity-trips-page";
import { LocalTripsPage } from "@/pages/trips/local-trips-page";
import { ProfilePage } from "@/pages/profile/profile-page";
import { SettingsProfilePage } from "@/pages/profile/settings-profile-page";
import { SettingsSecurityPage } from "@/pages/profile/settings-security-page";
import { SettingsHistoryPage } from "@/pages/profile/settings-history-page";
import { SettingsLocationsPage } from "@/pages/profile/settings-locations-page";
import { SettingsSupportPage } from "@/pages/profile/settings-support-page";

function ProtectedRoute({
  children,
  requireCompletedProfile = false,
  needsProfileCompletion = false,
}) {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate replace to="/auth/login" />;
  }

  if (
    requireCompletedProfile &&
    needsProfileCompletion &&
    location.pathname !== "/app/settings/profile"
  ) {
    return <Navigate replace to="/app/settings/profile" />;
  }

  return children;
}

function PublicOnlyRoute({ children }) {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);

  if (isAuthenticated) {
    return <Navigate replace to="/app/trips/local" />;
  }

  return children;
}

export function AppRouter() {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const setSession = useAuthStore((state) => state.setSession);
  const clearAuth = useAuthStore((state) => state.clearAuth);
  const [isCheckingSession, setIsCheckingSession] = useState(true);

  useEffect(() => {
    let isMounted = true;

    authGet("/auth/session")
      .then((session) => {
        if (!isMounted) return;
        setSession(session);
      })
      .catch(() => {
        if (!isMounted) return;
        clearAuth();
      })
      .finally(() => {
        if (!isMounted) return;
        setIsCheckingSession(false);
      });

    return () => {
      isMounted = false;
    };
  }, [clearAuth, setSession]);

  const profileCompletionQuery = useQuery({
    queryKey: ["profile-completion", isAuthenticated],
    queryFn: () => getProfile(),
    enabled: !isCheckingSession && isAuthenticated,
    retry: false,
  });

  const needsProfileCompletion =
    isAuthenticated && profileCompletionQuery.data
      ? isProfileIncomplete(profileCompletionQuery.data)
      : false;

  if (
    isCheckingSession ||
    (isAuthenticated && profileCompletionQuery.isLoading)
  ) {
    return null;
  }

  return (
    <Routes>
      <Route
        path="/"
        element={
          isAuthenticated ? (
            <Navigate replace to="/app/trips/local" />
          ) : (
            <Navigate replace to="/auth/login" />
          )
        }
      />
      <Route
        path="/auth/login"
        element={
          <PublicOnlyRoute>
            <AuthPage />
          </PublicOnlyRoute>
        }
      />
      <Route
        path="/auth/register"
        element={
          <PublicOnlyRoute>
            <AuthPage />
          </PublicOnlyRoute>
        }
      />
      <Route
        path="/auth/forgot-password"
        element={
          <PublicOnlyRoute>
            <ForgotPasswordPage />
          </PublicOnlyRoute>
        }
      />
      <Route
        path="/auth/reset-password"
        element={
          <PublicOnlyRoute>
            <ResetPasswordPage />
          </PublicOnlyRoute>
        }
      />
      <Route path="/auth/confirm-email" element={<ConfirmEmailPage />} />
      <Route
        path="/app/trips"
        element={<Navigate replace to="/app/trips/local" />}
      />
      <Route
        path="/app/trips/local"
        element={
          <ProtectedRoute
            requireCompletedProfile
            needsProfileCompletion={needsProfileCompletion}
          >
            <LocalTripsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/app/trips/intercity"
        element={
          <ProtectedRoute
            requireCompletedProfile
            needsProfileCompletion={needsProfileCompletion}
          >
            <IntercityTripsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/app/profile"
        element={<Navigate replace to="/app/settings" />}
      />
      <Route
        path="/app/settings"
        element={
          <ProtectedRoute
            requireCompletedProfile
            needsProfileCompletion={needsProfileCompletion}
          >
            <ProfilePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/app/settings/profile"
        element={
          <ProtectedRoute>
            <SettingsProfilePage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/app/settings/security"
        element={
          <ProtectedRoute
            requireCompletedProfile
            needsProfileCompletion={needsProfileCompletion}
          >
            <SettingsSecurityPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/app/settings/history"
        element={
          <ProtectedRoute
            requireCompletedProfile
            needsProfileCompletion={needsProfileCompletion}
          >
            <SettingsHistoryPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/app/settings/locations"
        element={
          <ProtectedRoute
            requireCompletedProfile
            needsProfileCompletion={needsProfileCompletion}
          >
            <SettingsLocationsPage />
          </ProtectedRoute>
        }
      />
      <Route
        path="/app/settings/support"
        element={
          <ProtectedRoute
            requireCompletedProfile
            needsProfileCompletion={needsProfileCompletion}
          >
            <SettingsSupportPage />
          </ProtectedRoute>
        }
      />
      <Route path="*" element={<Navigate replace to="/auth/login" />} />
    </Routes>
  );
}

function isProfileIncomplete(profile) {
  const role = String(profile?.role ?? "").toLowerCase();
  const hasName = Boolean(String(profile?.name ?? "").trim());

  if (!hasName) {
    return true;
  }

  if (role === "driver") {
    return !(
      String(profile?.carInfo?.brand ?? "").trim() &&
      String(profile?.carInfo?.model ?? "").trim() &&
      String(profile?.carInfo?.color ?? "").trim() &&
      String(profile?.carInfo?.plateNumber ?? "").trim()
    );
  }

  return !(
    String(profile?.address?.city ?? "").trim() &&
    String(profile?.address?.street ?? "").trim() &&
    String(profile?.address?.house ?? "").trim()
  );
}
