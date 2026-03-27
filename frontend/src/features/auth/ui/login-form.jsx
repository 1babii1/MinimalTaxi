import { useState } from "react";
import { Link } from "react-router-dom";
import { useLoginMutation } from "@/features/auth/model/use-auth-mutations";
import { useAuthStore } from "@/entities/auth/model/use-auth-store";
import { authGet } from "@/shared/api/http";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { Label } from "@/shared/ui/label";

export function LoginForm() {
  const mutation = useLoginMutation();
  const setSession = useAuthStore((state) => state.setSession);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [formError, setFormError] = useState(null);

  const onSubmit = async (event) => {
    event.preventDefault();
    setFormError(null);

    await mutation.mutateAsync({ email, password });
    const session = await authGet("/auth/session");

    setSession(session);
  };

  return (
    <form className="space-y-4" onSubmit={onSubmit}>
      {mutation.isSuccess && <Alert>Вы успешно вошли в систему.</Alert>}
      {mutation.isError && (
        <Alert variant="destructive">{mutation.error.message}</Alert>
      )}
      {formError && <Alert variant="destructive">{formError}</Alert>}

      <div className="space-y-2">
        <Label htmlFor="login-email">Email</Label>
        <Input
          id="login-email"
          type="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          required
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="login-password">Пароль</Label>
        <Input
          id="login-password"
          type="password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          required
        />
      </div>

      <Button className="w-full" disabled={mutation.isPending} type="submit">
        {mutation.isPending ? "Вход..." : "Войти"}
      </Button>

      <p className="text-center text-sm text-muted-foreground">
        Забыли пароль?{" "}
        <Link
          className="text-primary underline-offset-4 hover:underline"
          to="/auth/forgot-password"
        >
          Восстановить
        </Link>
      </p>
    </form>
  );
}
