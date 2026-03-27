import { useLocation, useNavigate, useSearchParams } from "react-router-dom";
import { LoginForm } from "@/features/auth/ui/login-form";
import { RegisterForm } from "@/features/auth/ui/register-form";
import { useSettingsStore } from "@/features/settings/model/use-settings-store";
import { Alert } from "@/shared/ui/alert";
import { Tabs, TabButton } from "@/shared/ui/tabs";
import { AuthLayout } from "@/widgets/auth-layout/ui/auth-layout";

export function AuthPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const authTab = useSettingsStore((state) => state.authTab);
  const setAuthTab = useSettingsStore((state) => state.setAuthTab);

  const isRegisterRoute = location.pathname.includes("/register");
  const activeTab = isRegisterRoute ? "register" : authTab;
  const socialError = searchParams.get("socialError");
  const authApiUrl = import.meta.env.VITE_AUTH_API_URL ?? "";

  const yandexLoginUrl = `${authApiUrl}/auth/external/yandex/start`;
  const vkLoginUrl = `${authApiUrl}/auth/external/vk/start`;

  const switchTab = (tab) => {
    setAuthTab(tab);
    navigate(tab === "login" ? "/auth/login" : "/auth/register");
  };

  return (
    <AuthLayout
      title="MinimalTaxi"
      description="Войдите или создайте аккаунт, чтобы продолжить."
    >
      <Tabs className="mb-4">
        <TabButton
          isActive={activeTab === "login"}
          onClick={() => switchTab("login")}
          type="button"
        >
          Вход
        </TabButton>
        <TabButton
          isActive={activeTab === "register"}
          onClick={() => switchTab("register")}
          type="button"
        >
          Регистрация
        </TabButton>
      </Tabs>

      {activeTab === "register" ? <RegisterForm /> : <LoginForm />}

      {socialError && <Alert variant="destructive">{socialError}</Alert>}

      <div className="mt-4 space-y-2">
        <p className="text-center text-xs text-muted-foreground">
          Или продолжить через
        </p>
        <div className="grid grid-cols-2 gap-2">
          <a
            href={yandexLoginUrl}
            className="inline-flex h-10 items-center justify-center rounded-md border border-input bg-background px-3 text-sm hover:bg-accent"
          >
            Yandex
          </a>
          <a
            href={vkLoginUrl}
            className="inline-flex h-10 items-center justify-center rounded-md border border-input bg-background px-3 text-sm hover:bg-accent"
          >
            VK
          </a>
        </div>
      </div>

      {activeTab === "register" && (
        <p className="mt-4 text-center text-xs text-muted-foreground">
          После регистрации нужно подтвердить email.
        </p>
      )}
    </AuthLayout>
  );
}
