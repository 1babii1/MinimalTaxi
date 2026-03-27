import { cn } from "@/shared/lib/cn";

export function Label({ className, ...props }) {
  return (
    <label
      className={cn("text-sm font-medium leading-none", className)}
      {...props}
    />
  );
}
