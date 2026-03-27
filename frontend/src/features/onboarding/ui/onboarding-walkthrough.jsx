import { useMemo, useState } from "react";
import { Compass, MapPinned, Rocket, X } from "lucide-react";
import { Button } from "@/shared/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/shared/ui/card";
import { cn } from "@/shared/lib/cn";

const SLIDES = [
  {
    id: "intro",
    title: "Добро пожаловать в MinimalTaxi",
    text: "Приложение работает благодаря бесплатным API Яндекса и Dadata. Спасибо, что пользуетесь сервисом и помогаете ему развиваться.",
    Icon: Rocket,
    accent: "from-sky-500/20 via-cyan-400/10 to-emerald-500/20",
  },
  {
    id: "routes",
    title: "Как пользоваться",
    text: "1) Создайте местную или межгород заявку. 2) Отслеживайте статусы в карточках. 3) Используйте карту и звонок прямо из заявки.",
    Icon: MapPinned,
    accent: "from-emerald-500/20 via-lime-400/10 to-yellow-500/20",
  },
  {
    id: "features",
    title: "Подсказки и удобство",
    text: "Адресные подсказки, сохранённые локации и realtime-обновления помогают быстрее найти поездку и не пропустить изменения.",
    Icon: Compass,
    accent: "from-indigo-500/20 via-blue-400/10 to-sky-500/20",
  },
];

export function OnboardingWalkthrough({ onFinish }) {
  const [index, setIndex] = useState(0);

  const slide = useMemo(() => SLIDES[index], [index]);
  const isLast = index === SLIDES.length - 1;

  return (
    <div className="fixed inset-0 z-[120] flex items-center justify-center bg-black/60 p-3">
      <Card className="w-full max-w-[460px] overflow-hidden border-0 shadow-2xl">
        <div className={cn("relative bg-gradient-to-br p-5", slide.accent)}>
          <button
            type="button"
            className="absolute right-3 top-3 inline-flex h-8 w-8 items-center justify-center rounded-full border bg-background/80"
            onClick={onFinish}
            aria-label="Пропустить"
          >
            <X className="h-4 w-4" />
          </button>

          <div className="mb-3 inline-flex h-12 w-12 items-center justify-center rounded-xl border bg-background/90">
            <slide.Icon className="h-6 w-6 animate-pulse text-primary" />
          </div>

          <CardHeader className="p-0">
            <CardTitle className="text-xl">{slide.title}</CardTitle>
          </CardHeader>
          <CardContent className="p-0 pt-2 text-sm text-muted-foreground">
            {slide.text}
          </CardContent>
        </div>

        <CardContent className="space-y-4 p-5">
          <div className="flex items-center justify-center gap-2">
            {SLIDES.map((item, itemIndex) => (
              <span
                key={item.id}
                className={cn(
                  "h-1.5 w-6 rounded-full bg-muted transition-all",
                  itemIndex === index && "w-10 bg-primary",
                )}
              />
            ))}
          </div>

          <div className="flex items-center justify-between gap-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => setIndex((prev) => Math.max(0, prev - 1))}
              disabled={index === 0}
            >
              Назад
            </Button>

            <Button
              type="button"
              onClick={() => {
                if (isLast) {
                  onFinish();
                  return;
                }

                setIndex((prev) => Math.min(SLIDES.length - 1, prev + 1));
              }}
            >
              {isLast ? "Начать" : "Далее"}
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
