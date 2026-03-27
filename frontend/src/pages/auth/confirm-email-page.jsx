import { Link } from "react-router-dom";
import { ConfirmEmailCard } from "@/features/auth/ui/confirm-email-card";
import { AuthLayout } from "@/widgets/auth-layout/ui/auth-layout";

export function ConfirmEmailPage() {
  return (
    <AuthLayout
      title="Подтверждение email"
      description="Подтверждаем аккаунт по ссылке из письма."
    >
      <ConfirmEmailCard />
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
