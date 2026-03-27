import { ThemeToggle } from "@/features/settings/ui/theme-toggle";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/shared/ui/card";

export function AuthLayout({ title, description, children }) {
  return (
    <main className="relative flex min-h-screen items-center justify-center bg-gradient-to-b from-background to-muted/50 px-4 py-8">
      <Card className="w-full max-w-md">
        <CardHeader className="space-y-2">
          <div className="flex items-start justify-between gap-3">
            <CardTitle>{title}</CardTitle>
            <ThemeToggle />
          </div>
          <CardDescription>{description}</CardDescription>
        </CardHeader>
        <CardContent>{children}</CardContent>
      </Card>
    </main>
  );
}
