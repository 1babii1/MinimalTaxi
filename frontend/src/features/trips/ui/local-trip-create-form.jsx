import { useEffect, useMemo, useState } from "react";
import { ArrowUpDown, Bookmark, Crosshair, MapPin } from "lucide-react";
import {
  getAddressByCoordinates,
  getAddressSuggestions,
} from "@/entities/geocoding/api/geocoding-api";
import { useSavedLocations } from "@/entities/locations/model/use-saved-locations";
import { useCreateLocalTripMutation } from "@/entities/trips/model/use-trips";
import { useGeolocation } from "@/hooks/use-geolocation";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/shared/ui/card";
import { Input } from "@/shared/ui/input";
import { Label } from "@/shared/ui/label";

export function LocalTripCreateForm({ onCreated, onCancel }) {
  const mutation = useCreateLocalTripMutation();
  const { locations } = useSavedLocations();

  const [fromLatitude, setFromLatitude] = useState("");
  const [fromLongitude, setFromLongitude] = useState("");
  const [toLatitude, setToLatitude] = useState("");
  const [toLongitude, setToLongitude] = useState("");
  const [fromAddress, setFromAddress] = useState("");
  const [toAddress, setToAddress] = useState("");
  const [description, setDescription] = useState("");
  const [activeAddressField, setActiveAddressField] = useState("from");
  const [showSavedLocations, setShowSavedLocations] = useState(false);
  const [fromAddressSuggestions, setFromAddressSuggestions] = useState([]);
  const [toAddressSuggestions, setToAddressSuggestions] = useState([]);
  const [isFromSuggestionsLoading, setIsFromSuggestionsLoading] =
    useState(false);
  const [isToSuggestionsLoading, setIsToSuggestionsLoading] = useState(false);
  const [fromSuggestionsLocked, setFromSuggestionsLocked] = useState(false);
  const [toSuggestionsLocked, setToSuggestionsLocked] = useState(false);
  const [formError, setFormError] = useState("");
  const {
    loading: geolocationLoading,
    error: geolocationError,
    getCurrentPosition,
  } = useGeolocation();

  const quickLocations = useMemo(() => locations.slice(0, 3), [locations]);

  useEffect(() => {
    let isCancelled = false;

    if (fromSuggestionsLocked || String(fromAddress).trim().length < 3) {
      setFromAddressSuggestions([]);
      setIsFromSuggestionsLoading(false);
      return () => {
        isCancelled = true;
      };
    }

    setIsFromSuggestionsLoading(true);
    const timeoutId = window.setTimeout(async () => {
      try {
        const suggestions = await getAddressSuggestions(fromAddress, 5);
        if (!isCancelled) {
          setFromAddressSuggestions(suggestions);
        }
      } catch {
        if (!isCancelled) {
          setFromAddressSuggestions([]);
        }
      } finally {
        if (!isCancelled) {
          setIsFromSuggestionsLoading(false);
        }
      }
    }, 300);

    return () => {
      isCancelled = true;
      window.clearTimeout(timeoutId);
    };
  }, [fromAddress, fromSuggestionsLocked]);

  useEffect(() => {
    let isCancelled = false;

    if (toSuggestionsLocked || String(toAddress).trim().length < 3) {
      setToAddressSuggestions([]);
      setIsToSuggestionsLoading(false);
      return () => {
        isCancelled = true;
      };
    }

    setIsToSuggestionsLoading(true);
    const timeoutId = window.setTimeout(async () => {
      try {
        const suggestions = await getAddressSuggestions(toAddress, 5);
        if (!isCancelled) {
          setToAddressSuggestions(suggestions);
        }
      } catch {
        if (!isCancelled) {
          setToAddressSuggestions([]);
        }
      } finally {
        if (!isCancelled) {
          setIsToSuggestionsLoading(false);
        }
      }
    }, 300);

    return () => {
      isCancelled = true;
      window.clearTimeout(timeoutId);
    };
  }, [toAddress, toSuggestionsLocked]);

  const swapDirections = () => {
    setFromAddress(toAddress);
    setToAddress(fromAddress);
    setFromLatitude(toLatitude);
    setFromLongitude(toLongitude);
    setToLatitude(fromLatitude);
    setToLongitude(fromLongitude);
  };

  const onSubmit = async (event) => {
    event.preventDefault();

    setFormError("");

    const parsedFromLatitude = Number(fromLatitude);
    const parsedFromLongitude = Number(fromLongitude);
    const parsedToLatitude = Number(toLatitude);
    const parsedToLongitude = Number(toLongitude);

    if (
      !Number.isFinite(parsedFromLatitude) ||
      !Number.isFinite(parsedFromLongitude) ||
      !Number.isFinite(parsedToLatitude) ||
      !Number.isFinite(parsedToLongitude)
    ) {
      setFormError("Выберите точки Откуда и Куда.");
      return;
    }

    await mutation.mutateAsync({
      from: {
        latitude: parsedFromLatitude,
        longitude: parsedFromLongitude,
      },
      to: {
        latitude: parsedToLatitude,
        longitude: parsedToLongitude,
      },
      fromAddress,
      toAddress,
      description: String(description).trim() || null,
    });

    onCreated?.();
  };

  const detectFromLocation = async () => {
    try {
      const location = await getCurrentPosition();

      setFromLatitude(String(location.latitude));
      setFromLongitude(String(location.longitude));

      const detectedAddress = await getAddressByCoordinates(
        location.latitude,
        location.longitude,
      );

      if (detectedAddress) {
        setFromSuggestionsLocked(true);
        setFromAddress(detectedAddress);
        setFromAddressSuggestions([]);
      }
    } catch {
      return;
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Создать заявку</CardTitle>
      </CardHeader>
      <CardContent>
        <form className="space-y-4" onSubmit={onSubmit}>
          {mutation.isSuccess && <Alert>Поездка создана</Alert>}
          {formError && <Alert variant="destructive">{formError}</Alert>}
          {mutation.isError && (
            <Alert variant="destructive">{mutation.error.message}</Alert>
          )}

          <div className="space-y-2">
            <div className="flex items-center justify-between gap-2">
              <Label htmlFor="local-from-address">Откуда</Label>
              <div className="ml-auto flex items-center gap-2">
                <Button
                  type="button"
                  variant="outline"
                  size="icon"
                  onClick={swapDirections}
                >
                  <ArrowUpDown className="h-4 w-4" />
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={detectFromLocation}
                  disabled={geolocationLoading}
                >
                  <Crosshair className="mr-1 h-4 w-4" />
                  {geolocationLoading ? "Определяем..." : "Определить"}
                </Button>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Input
                id="local-from-address"
                value={fromAddress}
                onChange={(event) => {
                  setFromSuggestionsLocked(false);
                  setFromAddress(event.target.value);
                }}
                placeholder="Введите адрес отправления"
                required
              />
              <Button
                type="button"
                variant="outline"
                size="icon"
                onClick={() => {
                  setActiveAddressField("from");
                  setShowSavedLocations((prev) => !prev);
                }}
                aria-label="Мои локации"
              >
                <Bookmark className="h-4 w-4" />
              </Button>
            </div>

            {showSavedLocations && quickLocations.length > 0 && (
              <div className="flex flex-wrap gap-2">
                {quickLocations.map((location) => (
                  <Button
                    key={location.id}
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => {
                      if (activeAddressField === "from") {
                        setFromSuggestionsLocked(true);
                        setFromAddress(location.address);
                        setFromLatitude(String(location.latitude));
                        setFromLongitude(String(location.longitude));
                      } else {
                        setToSuggestionsLocked(true);
                        setToAddress(location.address);
                        setToLatitude(String(location.latitude));
                        setToLongitude(String(location.longitude));
                      }
                      setShowSavedLocations(false);
                    }}
                  >
                    {location.name}
                  </Button>
                ))}
              </div>
            )}

            {isFromSuggestionsLoading && (
              <p className="text-xs text-muted-foreground">Поиск адреса...</p>
            )}

            {!isFromSuggestionsLoading && fromAddressSuggestions.length > 0 && (
              <div className="max-h-40 overflow-auto rounded-md border bg-background">
                {fromAddressSuggestions.map((item, index) => (
                  <button
                    key={`${item.address}-${index}`}
                    type="button"
                    className="block w-full border-b px-3 py-2 text-left text-sm last:border-0 hover:bg-muted/50"
                    onClick={() => {
                      setFromSuggestionsLocked(true);
                      setFromAddress(item.address);
                      setFromLatitude(String(item.latitude));
                      setFromLongitude(String(item.longitude));
                      setFromAddressSuggestions([]);
                    }}
                  >
                    {item.address}
                  </button>
                ))}
              </div>
            )}

            {geolocationError && (
              <p className="text-xs text-destructive">{geolocationError}</p>
            )}
          </div>

          <div className="space-y-2">
            <Label htmlFor="local-to-address">Куда</Label>
            <div className="flex items-center gap-2">
              <Input
                id="local-to-address"
                value={toAddress}
                onChange={(event) => {
                  setToSuggestionsLocked(false);
                  setToAddress(event.target.value);
                }}
                placeholder="Введите адрес назначения"
                required
              />
              <Button
                type="button"
                variant="outline"
                size="icon"
                onClick={() => {
                  setActiveAddressField("to");
                  setShowSavedLocations((prev) => !prev);
                }}
                aria-label="Мои локации"
              >
                <Bookmark className="h-4 w-4" />
              </Button>
            </div>

            {isToSuggestionsLoading && (
              <p className="text-xs text-muted-foreground">Поиск адреса...</p>
            )}

            {!isToSuggestionsLoading && toAddressSuggestions.length > 0 && (
              <div className="max-h-40 overflow-auto rounded-md border bg-background">
                {toAddressSuggestions.map((item, index) => (
                  <button
                    key={`${item.address}-${index}`}
                    type="button"
                    className="block w-full border-b px-3 py-2 text-left text-sm last:border-0 hover:bg-muted/50"
                    onClick={() => {
                      setToSuggestionsLocked(true);
                      setToAddress(item.address);
                      setToLatitude(String(item.latitude));
                      setToLongitude(String(item.longitude));
                      setToAddressSuggestions([]);
                    }}
                  >
                    {item.address}
                  </button>
                ))}
              </div>
            )}
          </div>

          {fromLatitude && fromLongitude && (
            <p className="text-xs text-muted-foreground">
              <MapPin className="mr-1 inline-block h-3 w-3" />
              Откуда: {Number(fromLatitude).toFixed(5)},{" "}
              {Number(fromLongitude).toFixed(5)}
            </p>
          )}

          {toLatitude && toLongitude && (
            <p className="text-xs text-muted-foreground">
              <MapPin className="mr-1 inline-block h-3 w-3" />
              Куда: {Number(toLatitude).toFixed(5)},{" "}
              {Number(toLongitude).toFixed(5)}
            </p>
          )}

          <div className="space-y-2">
            <Label htmlFor="local-description">Описание</Label>
            <textarea
              id="local-description"
              className="min-h-24 w-full rounded-md border border-input bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              value={description}
              onChange={(event) => setDescription(event.target.value)}
            />
          </div>

          <div className="flex justify-end gap-2">
            {onCancel && (
              <Button type="button" variant="outline" onClick={onCancel}>
                Отмена
              </Button>
            )}
            <Button disabled={mutation.isPending} type="submit">
              {mutation.isPending ? "Создание..." : "Создать заявку"}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
