import { BrowserRouter } from "react-router-dom";
import { QueryProvider } from "@/app/providers/query-provider";
import { PermissionsReminderProvider } from "@/app/providers/permissions-reminder-provider";
import { ThemeProvider } from "@/app/providers/theme-provider";
import { TripsEventsProvider } from "@/app/providers/trips-events-provider";
import { AppRouter } from "@/app/router";

function App() {
  return (
    <QueryProvider>
      <TripsEventsProvider>
        <PermissionsReminderProvider>
          <ThemeProvider>
            <BrowserRouter>
              <AppRouter />
            </BrowserRouter>
          </ThemeProvider>
        </PermissionsReminderProvider>
      </TripsEventsProvider>
    </QueryProvider>
  );
}

export default App;
