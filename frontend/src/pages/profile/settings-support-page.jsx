import { useState } from "react";
import { AppLayout } from "@/widgets/app-layout/ui/app-layout";
import { Button } from "@/shared/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/shared/ui/card";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/shared/ui/tooltip";

export function SettingsSupportPage() {
  const [errorMessage, setErrorMessage] = useState("");
  const [copiedKey, setCopiedKey] = useState("");

  const supportItems = [
    {
      id: "sber",
      label: "Карта Сбербанк",
      value: "2202 2068 7982 6381",
      copyValue: "2202206879826381",
    },
    {
      id: "usdt",
      label: "USDT (TRC20)",
      value: "TTgHQUpuCYU8ZU8MF3tGYr8kTMxzDdy2V7",
      copyValue: "TTgHQUpuCYU8ZU8MF3tGYr8kTMxzDdy2V7",
    },
    {
      id: "ton",
      label: "TON",
      value: "UQCH7KhRIhYc669GkXWg4kkdslIZO0x6AypleSWCRZFwQrOL",
      copyValue: "UQCH7KhRIhYc669GkXWg4kkdslIZO0x6AypleSWCRZFwQrOL",
    },
  ];

  const email = "sabangulov.ilnaz@mail.ru";

  const onCopy = async (value, key) => {
    try {
      await navigator.clipboard.writeText(value);
      setErrorMessage("");
      setCopiedKey(key);
      window.setTimeout(() => {
        setCopiedKey((current) => (current === key ? "" : current));
      }, 1500);
    } catch {
      setErrorMessage("Не удалось скопировать. Попробуйте ещё раз.");
    }
  };

  return (
    <AppLayout>
      <TooltipProvider delayDuration={0}>
        <Card>
          <CardHeader>
            <CardTitle>Поддержать разработчика</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4 text-sm">
            {errorMessage && (
              <p className="text-sm text-destructive">{errorMessage}</p>
            )}

            <p className="text-muted-foreground">
              Спасибо за поддержку проекта. Можно выбрать любой удобный способ:
            </p>

            <div className="space-y-3">
              {supportItems.map((item) => (
                <div
                  key={item.id}
                  className="rounded-lg border bg-muted/30 p-3"
                >
                  <p className="text-xs text-muted-foreground">{item.label}</p>
                  <p className="break-all font-medium">{item.value}</p>
                  <div className="mt-2 flex justify-end">
                    <Tooltip open={copiedKey === `${item.id}-copy`}>
                      <TooltipTrigger asChild>
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          onClick={() =>
                            onCopy(item.copyValue, `${item.id}-copy`)
                          }
                        >
                          Копировать
                        </Button>
                      </TooltipTrigger>
                      <TooltipContent side="top" align="end">
                        Скопировано
                      </TooltipContent>
                    </Tooltip>
                  </div>
                </div>
              ))}
            </div>

            <div className="rounded-lg border bg-muted/30 p-3">
              <p className="text-xs text-muted-foreground">Email</p>
              <p className="break-all font-medium">{email}</p>
              <div className="mt-2 flex flex-wrap justify-end gap-2">
                <Tooltip open={copiedKey === "email-copy"}>
                  <TooltipTrigger asChild>
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      onClick={() => onCopy(email, "email-copy")}
                    >
                      Копировать
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent side="top" align="end">
                    Скопировано
                  </TooltipContent>
                </Tooltip>
                <Button
                  type="button"
                  size="sm"
                  onClick={() => {
                    window.location.href = `mailto:${email}`;
                  }}
                >
                  Написать письмо
                </Button>
              </div>
            </div>
          </CardContent>
        </Card>
      </TooltipProvider>
    </AppLayout>
  );
}
