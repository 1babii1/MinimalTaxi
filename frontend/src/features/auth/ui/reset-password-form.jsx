import { useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { useResetPasswordMutation } from "@/features/auth/model/use-auth-mutations";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { Label } from "@/shared/ui/label";

export function ResetPasswordForm() {
  const mutation = useResetPasswordMutation();
  const [searchParams] = useSearchParams();

  const initialEmail = useMemo(
    () => searchParams.get("email") ?? "",
    [searchParams],
  );
  const token = useMemo(() => searchParams.get("token") ?? "", [searchParams]);

  const [email, setEmail] = useState(initialEmail);
  const [newPassword, setNewPassword] = useState("");

  const onSubmit = async (event) => {
    event.preventDefault();
    await mutation.mutateAsync({ email, token, newPassword });
  };

  const tokenMissing = !token;

  return (
    <form className="space-y-4" onSubmit={onSubmit}>
      {tokenMissing && (
        <Alert variant="destructive">Токен сброса не найден в URL.</Alert>
      )}
      {mutation.isSuccess && <Alert>Пароль успешно обновлён.</Alert>}
      {mutation.isError && (
        <Alert variant="destructive">{mutation.error.message}</Alert>
      )}

      <div className="space-y-2">
        <Label htmlFor="reset-email">Email</Label>
        <Input
          id="reset-email"
          type="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          required
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="reset-password">Новый пароль</Label>
        <Input
          id="reset-password"
          type="password"
          value={newPassword}
          onChange={(event) => setNewPassword(event.target.value)}
          required
        />
      </div>

      <Button
        className="w-full"
        disabled={mutation.isPending || tokenMissing}
        type="submit"
      >
        {mutation.isPending ? "Сохранение..." : "Сменить пароль"}
      </Button>
    </form>
  );
}
