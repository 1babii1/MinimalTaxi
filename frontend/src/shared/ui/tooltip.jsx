import * as TooltipPrimitive from "@radix-ui/react-tooltip";
import { cn } from "@/shared/lib/cn";

export function TooltipProvider({ children, ...props }) {
  return (
    <TooltipPrimitive.Provider {...props}>{children}</TooltipPrimitive.Provider>
  );
}

export function Tooltip({ children, ...props }) {
  return <TooltipPrimitive.Root {...props}>{children}</TooltipPrimitive.Root>;
}

export function TooltipTrigger({ children, ...props }) {
  return (
    <TooltipPrimitive.Trigger {...props}>{children}</TooltipPrimitive.Trigger>
  );
}

export function TooltipContent({ className, sideOffset = 6, ...props }) {
  return (
    <TooltipPrimitive.Portal>
      <TooltipPrimitive.Content
        sideOffset={sideOffset}
        className={cn(
          "z-50 overflow-hidden rounded-md border bg-popover px-3 py-1.5 text-xs text-popover-foreground shadow-md",
          className,
        )}
        {...props}
      />
    </TooltipPrimitive.Portal>
  );
}
