import { cn } from "@/shared/lib/cn";

export function Alert({ className, variant = "default", ...props }) {
  const styles =
    variant === "destructive"
      ? "border-destructive/60 text-destructive"
      : "border-border text-foreground";

  return (
    <div
      className={cn("rounded-lg border px-4 py-3 text-sm", styles, className)}
      {...props}
    />
  );
}
