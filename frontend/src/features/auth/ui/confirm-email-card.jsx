import { useEffect } from "react";
import { useSearchParams } from "react-router-dom";
import { useConfirmEmailMutation } from "@/features/auth/model/use-auth-mutations";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";

export function ConfirmEmailCard() {
  const mutation = useConfirmEmailMutation();
  const [searchParams] = useSearchParams();

  const userId = searchParams.get("userId") ?? "";
  const token = (searchParams.get("token") ?? "").replace(/ /g, "+");

  useEffect(() => {
    if (!userId || !token || mutation.isSuccess || mutation.isPending) return;

    mutation.mutate({ userId, token });
  }, [mutation, token, userId]);

  if (!userId || !token) {
    return (
      <Alert variant="destructive">Ссылка подтверждения некорректна.</Alert>
    );
  }

  return (
    <div className="space-y-3">
      {mutation.isPending && <Alert>Подтверждаем email...</Alert>}
      {mutation.isSuccess && (
        <Alert>Email успешно подтверждён. Теперь можно войти.</Alert>
      )}
      {mutation.isError && (
        <Alert variant="destructive">{mutation.error.message}</Alert>
      )}

      {mutation.isError && (
        <Button
          onClick={() => mutation.mutate({ userId, token })}
          type="button"
          variant="outline"
        >
          Попробовать снова
        </Button>
      )}
    </div>
  );
}
