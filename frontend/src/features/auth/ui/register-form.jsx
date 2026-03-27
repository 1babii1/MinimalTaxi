import { useState } from "react";
import { CAR_BRANDS, CAR_MODELS_BY_BRAND } from "@/constants/car-catalog";
import { useRegisterMutation } from "@/features/auth/model/use-auth-mutations";
import { AddressAutocompleteInput } from "@/features/trips/ui/address-autocomplete-input";
import {
  formatPhoneInput,
  handlePhoneMaskedKeyDown,
  isValidCarPlate,
  normalizeCarPlate,
  normalizePhoneStorage,
  parseSingleAddress,
} from "@/lib/profile-input-formatters";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";
import { Input } from "@/shared/ui/input";
import { Label } from "@/shared/ui/label";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/shared/ui/tooltip";

export function RegisterForm() {
  const mutation = useRegisterMutation();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [name, setName] = useState("");
  const [phone, setPhone] = useState("");
  const [role, setRole] = useState("Passenger");

  const [address, setAddress] = useState("");
  const [addressLocation, setAddressLocation] = useState(null);

  const [brand, setBrand] = useState("");
  const [model, setModel] = useState("");
  const [color, setColor] = useState("#9ca3af");
  const [plateNumber, setPlateNumber] = useState("");
  const [formError, setFormError] = useState("");
  const [showSuccessTooltip, setShowSuccessTooltip] = useState(false);

  const onSubmit = async (event) => {
    event.preventDefault();
    setFormError("");

    const normalizedPhone = normalizePhoneStorage(phone);
    const normalizedPlate = normalizeCarPlate(plateNumber);

    if (role === "Passenger" && !addressLocation) {
      setFormError("Выберите адрес из подсказки, чтобы сохранить координаты.");
      return;
    }

    if (role === "Driver" && !isValidCarPlate(normalizedPlate)) {
      setFormError("Номер авто должен быть в формате M365MH102.");
      return;
    }

    const parsedAddress = parseSingleAddress(address);

    const payload = {
      email,
      password,
      role,
      name,
      phone: normalizedPhone || null,
      address:
        role === "Passenger"
          ? {
              city: parsedAddress.city,
              street: parsedAddress.street,
              house: parsedAddress.house,
              apartment: parsedAddress.apartment,
              latitude: addressLocation?.latitude ?? null,
              longitude: addressLocation?.longitude ?? null,
            }
          : null,
      carInfo:
        role === "Driver"
          ? {
              brand,
              model,
              color,
              plateNumber: normalizedPlate,
            }
          : null,
    };

    await mutation.mutateAsync(payload);

    setShowSuccessTooltip(true);
    window.setTimeout(() => {
      setShowSuccessTooltip(false);
    }, 1500);
  };

  return (
    <form className="space-y-4" onSubmit={onSubmit}>
      {mutation.isError && (
        <Alert variant="destructive">{mutation.error.message}</Alert>
      )}
      {formError && <Alert variant="destructive">{formError}</Alert>}

      <div className="space-y-2">
        <Label htmlFor="register-email">Эл. почта</Label>
        <Input
          id="register-email"
          type="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          autoComplete="email"
          required
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="register-password">Пароль</Label>
        <Input
          id="register-password"
          type="password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          autoComplete="new-password"
          required
        />
        <p className="text-xs text-muted-foreground">
          Минимум 8 символов, 1 заглавная буква и 1 цифра.
        </p>
      </div>

      <div className="space-y-2">
        <Label htmlFor="register-name">Имя</Label>
        <Input
          id="register-name"
          value={name}
          onChange={(event) => setName(event.target.value)}
          required
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="register-phone">Телефон</Label>
        <Input
          id="register-phone"
          value={phone}
          onChange={(event) => setPhone(formatPhoneInput(event.target.value))}
          onKeyDown={(event) =>
            handlePhoneMaskedKeyDown(event, phone, setPhone)
          }
          placeholder="+7-(961)-368-16-35"
        />
      </div>

      <div className="space-y-2">
        <Label id="register-role-label">Роль</Label>
        <div
          className="grid h-10 w-full grid-cols-2 rounded-md border border-input p-1"
          role="radiogroup"
          aria-labelledby="register-role-label"
        >
          <button
            type="button"
            className={`rounded-sm text-sm transition ${
              role === "Passenger"
                ? "bg-primary text-primary-foreground"
                : "text-muted-foreground"
            }`}
            onClick={() => setRole("Passenger")}
          >
            Пассажир
          </button>
          <button
            type="button"
            className={`rounded-sm text-sm transition ${
              role === "Driver"
                ? "bg-primary text-primary-foreground"
                : "text-muted-foreground"
            }`}
            onClick={() => setRole("Driver")}
          >
            Водитель
          </button>
        </div>
      </div>

      {role === "Passenger" && (
        <div className="grid gap-3 rounded-md border p-3">
          <AddressAutocompleteInput
            id="register-passenger-address"
            label="Адрес"
            value={address}
            onChange={(nextValue) => {
              setAddress(nextValue);
              setAddressLocation(null);
            }}
            onSelect={setAddressLocation}
            placeholder="Введите адрес"
            required
          />
          <div className="grid gap-2 md:grid-cols-2">
            <Input
              value={addressLocation?.latitude ?? ""}
              readOnly
              placeholder="Широта"
            />
            <Input
              value={addressLocation?.longitude ?? ""}
              readOnly
              placeholder="Долгота"
            />
          </div>
        </div>
      )}

      {role === "Driver" && (
        <div className="grid gap-3 rounded-md border p-3">
          <p className="text-sm font-medium">Информация об авто</p>
          <Label htmlFor="register-car-brand">Марка</Label>
          <select
            id="register-car-brand"
            className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
            value={brand}
            onChange={(event) => {
              setBrand(event.target.value);
              setModel("");
            }}
            required
          >
            <option value="">Выберите марку</option>
            {CAR_BRANDS.map((item) => (
              <option key={item} value={item}>
                {item}
              </option>
            ))}
          </select>

          <Label htmlFor="register-car-model">Модель</Label>
          <select
            id="register-car-model"
            className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm disabled:cursor-not-allowed disabled:opacity-50"
            value={model}
            onChange={(event) => setModel(event.target.value)}
            disabled={!brand}
            required
          >
            <option value="">
              {brand ? "Выберите модель" : "Сначала выберите марку"}
            </option>
            {(CAR_MODELS_BY_BRAND[brand] ?? []).map((item) => (
              <option key={item} value={item}>
                {item}
              </option>
            ))}
          </select>

          <Label htmlFor="register-car-color">Цвет (HEX)</Label>
          <div className="flex items-center gap-2">
            <Input
              id="register-car-color"
              type="color"
              className="h-10 w-12 p-1"
              value={color}
              onChange={(event) => setColor(event.target.value)}
              required
            />
            <Input
              placeholder="#9ca3af"
              value={color}
              onChange={(event) => setColor(event.target.value)}
              required
            />
          </div>
          <Input
            value={plateNumber}
            onChange={(event) =>
              setPlateNumber(normalizeCarPlate(event.target.value))
            }
            maxLength={9}
            placeholder="M365MH102"
            required
          />
        </div>
      )}

      <TooltipProvider delayDuration={0}>
        <Tooltip open={showSuccessTooltip}>
          <TooltipTrigger asChild>
            <Button
              className="w-full"
              disabled={mutation.isPending}
              type="submit"
            >
              {mutation.isPending ? "Создание..." : "Создать аккаунт"}
            </Button>
          </TooltipTrigger>
          <TooltipContent side="top" align="end">
            Письмо с подтверждением отправлено
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>
    </form>
  );
}
