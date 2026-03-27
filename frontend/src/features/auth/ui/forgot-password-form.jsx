import { useState } from "react";
import { useForgotPasswordMutation } from "@/features/auth/model/use-auth-mutations";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { Label } from "@/shared/ui/label";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/shared/ui/tooltip";

export function ForgotPasswordForm() {
  const mutation = useForgotPasswordMutation();
  const [email, setEmail] = useState("");
  const [showSuccessTooltip, setShowSuccessTooltip] = useState(false);

  const onSubmit = async (event) => {
    event.preventDefault();
    await mutation.mutateAsync({ email });
    setShowSuccessTooltip(true);
    window.setTimeout(() => {
      setShowSuccessTooltip(false);
    }, 1500);
  };

  return (
    <form className="space-y-4" onSubmit={onSubmit}>
      {mutation.isError && (
        <Alert variant="destructive">{mutation.error.message}</Alert>
      )}

      <div className="space-y-2">
        <Label htmlFor="forgot-email">Email</Label>
        <Input
          id="forgot-email"
          type="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          required
        />
      </div>

      <TooltipProvider delayDuration={0}>
        <Tooltip open={showSuccessTooltip}>
          <TooltipTrigger asChild>
            <Button
              className="w-full"
              disabled={mutation.isPending}
              type="submit"
            >
              {mutation.isPending ? "Отправка..." : "Отправить ссылку"}
            </Button>
          </TooltipTrigger>
          <TooltipContent side="top" align="end">
            Если аккаунт найден, письмо отправлено
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>
    </form>
  );
}
