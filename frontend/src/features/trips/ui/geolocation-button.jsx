import { useGeolocation } from "@/hooks/use-geolocation";
import { Button } from "@/shared/ui/button";

export function GeolocationButton({ onLocationSelected, label }) {
  const { location, loading, error, getCurrentPosition } = useGeolocation();

  const handleClick = async () => {
    try {
      const loc = await getCurrentPosition();
      onLocationSelected(loc);
    } catch {
      return;
    }
  };

  return (
    <div className="space-y-1">
      <Button
        type="button"
        variant="outline"
        onClick={handleClick}
        disabled={loading}
      >
        {loading ? "Определяем..." : (label ?? "Определить местоположение")}
      </Button>

      {error && <p className="text-xs text-destructive">{error}</p>}
      {location && (
        <p className="text-xs text-muted-foreground">
          Точка: {location.latitude.toFixed(5)}, {location.longitude.toFixed(5)}
        </p>
      )}
    </div>
  );
}
