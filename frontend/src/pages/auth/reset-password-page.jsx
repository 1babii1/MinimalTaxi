import { Link } from "react-router-dom";
import { ResetPasswordForm } from "@/features/auth/ui/reset-password-form";
import { AuthLayout } from "@/widgets/auth-layout/ui/auth-layout";

export function ResetPasswordPage() {
  return (
    <AuthLayout
      title="Новый пароль"
      description="Укажите новый пароль для аккаунта."
    >
      <ResetPasswordForm />
      <p className="mt-4 text-sm text-muted-foreground">
        <Link
          className="text-primary underline-offset-4 hover:underline"
          to="/auth/login"
        >
          Перейти ко входу
        </Link>
      </p>
    </AuthLayout>
  );
}
