import { cn } from "@/shared/lib/cn";

export function Tabs({ className, ...props }) {
  return (
    <div
      className={cn("grid grid-cols-2 rounded-lg bg-muted p-1", className)}
      {...props}
    />
  );
}

export function TabButton({ isActive, className, ...props }) {
  return (
    <button
      className={cn(
        "rounded-md px-3 py-2 text-sm font-medium transition-colors",
        isActive
          ? "bg-background text-foreground shadow-sm"
          : "text-muted-foreground hover:text-foreground",
        className,
      )}
      {...props}
    />
  );
}
