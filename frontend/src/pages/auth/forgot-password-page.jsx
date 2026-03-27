import { Link } from "react-router-dom";
import { ForgotPasswordForm } from "@/features/auth/ui/forgot-password-form";
import { AuthLayout } from "@/widgets/auth-layout/ui/auth-layout";

export function ForgotPasswordPage() {
  return (
    <AuthLayout
      title="Восстановление пароля"
      description="Введите email и получите ссылку для сброса."
    >
      <ForgotPasswordForm />
      <p className="mt-4 text-sm text-muted-foreground">
        <Link
          className="text-primary underline-offset-4 hover:underline"
          to="/auth/login"
        >
          Вернуться ко входу
        </Link>
      </p>
    </AuthLayout>
  );
}
