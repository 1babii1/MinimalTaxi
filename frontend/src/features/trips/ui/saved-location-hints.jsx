export function SavedLocationHints({ title, locations, onSelect }) {
  if (!locations.length) {
    return null;
  }

  return (
    <div className="space-y-2">
      <p className="text-xs text-muted-foreground">{title}</p>
      <div className="max-h-40 overflow-auto rounded-md border bg-background">
        {locations.map((location) => (
          <button
            key={location.id}
            type="button"
            className="block w-full border-b px-3 py-2 text-left text-sm last:border-0 hover:bg-muted/50"
            onClick={() => onSelect(location)}
          >
            <p className="font-medium">{location.name}</p>
            <p className="text-xs text-muted-foreground">{location.address}</p>
          </button>
        ))}
      </div>
    </div>
  );
}
