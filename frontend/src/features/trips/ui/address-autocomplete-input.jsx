import { useEffect, useMemo, useRef, useState } from "react";
import { getAddressSuggestions } from "@/entities/geocoding/api/geocoding-api";
import { Input } from "@/shared/ui/input";
import { Label } from "@/shared/ui/label";

export function AddressAutocompleteInput({
  id,
  label,
  value,
  placeholder,
  onChange,
  onSelect,
  onSelectSuggestion,
  required = false,
}) {
  const [suggestions, setSuggestions] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const selectedAddressRef = useRef("");

  const shouldSearch = useMemo(
    () => String(value ?? "").trim().length >= 3,
    [value],
  );

  useEffect(() => {
    let isCancelled = false;

    const normalizedValue = String(value ?? "").trim();
    if (normalizedValue && normalizedValue === selectedAddressRef.current) {
      selectedAddressRef.current = "";
      setSuggestions([]);
      setIsLoading(false);
      return () => {
        isCancelled = true;
      };
    }

    if (!shouldSearch) {
      setSuggestions([]);
      setIsLoading(false);
      return () => {
        isCancelled = true;
      };
    }

    setIsLoading(true);
    const timeoutId = window.setTimeout(async () => {
      try {
        const items = await getAddressSuggestions(value, 5);
        if (!isCancelled) {
          setSuggestions(items);
        }
      } catch {
        if (!isCancelled) {
          setSuggestions([]);
        }
      } finally {
        if (!isCancelled) {
          setIsLoading(false);
        }
      }
    }, 300);

    return () => {
      isCancelled = true;
      window.clearTimeout(timeoutId);
    };
  }, [shouldSearch, value]);

  const handleSelect = (item) => {
    selectedAddressRef.current = item.address;
    onChange(item.address);
    onSelect({ latitude: item.latitude, longitude: item.longitude });
    onSelectSuggestion?.(item);
    setSuggestions([]);
  };

  return (
    <div className="space-y-2">
      <Label htmlFor={id}>{label}</Label>
      <Input
        id={id}
        value={value}
        onChange={(event) => {
          selectedAddressRef.current = "";
          onChange(event.target.value);
        }}
        placeholder={placeholder}
        required={required}
        autoComplete="off"
      />
      {isLoading && (
        <p className="text-xs text-muted-foreground">Поиск адреса...</p>
      )}
      {!isLoading && suggestions.length > 0 && (
        <div className="max-h-40 overflow-auto rounded-md border bg-background">
          {suggestions.map((item, index) => (
            <button
              key={`${item.address}-${index}`}
              type="button"
              className="block w-full border-b px-3 py-2 text-left text-sm last:border-0 hover:bg-muted/50"
              onClick={() => handleSelect(item)}
            >
              {item.address}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
