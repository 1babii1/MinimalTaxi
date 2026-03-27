import { useMemo, useRef, useState } from "react";
import { Camera } from "lucide-react";
import { useAuthStore } from "@/entities/auth/model/use-auth-store";
import { CAR_BRANDS, CAR_MODELS_BY_BRAND } from "@/constants/car-catalog";
import {
  useProfileQuery,
  useUploadProfileAvatarMutation,
  useUpdateProfileMutation,
} from "@/entities/profile/model/use-profile-query";
import { AddressAutocompleteInput } from "@/features/trips/ui/address-autocomplete-input";
import {
  formatPhoneInput,
  formatProfileAddress,
  handlePhoneMaskedKeyDown,
  isValidCarPlate,
  normalizeCarPlate,
  normalizePhoneStorage,
  parseSingleAddress,
} from "@/lib/profile-input-formatters";
import { Alert } from "@/shared/ui/alert";
import { Button } from "@/shared/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/shared/ui/card";
import { Input } from "@/shared/ui/input";
import { Label } from "@/shared/ui/label";

export function ProfileForm() {
  const authRole = useAuthStore((state) => state.role);
  const authEmail = useAuthStore((state) => state.email);

  const profileQuery = useProfileQuery();
  const updateMutation = useUpdateProfileMutation();
  const uploadAvatarMutation = useUploadProfileAvatarMutation();

  const [isEditing, setIsEditing] = useState(false);
  const [form, setForm] = useState({
    name: "",
    phone: "",
    address: "",
    carBrand: "",
    carModel: "",
    carColor: "",
    carPlateNumber: "",
  });
  const [addressLocation, setAddressLocation] = useState(null);
  const [formError, setFormError] = useState("");
  const [avatarPreviewUrl, setAvatarPreviewUrl] = useState(null);
  const [avatarFile, setAvatarFile] = useState(null);
  const avatarInputRef = useRef(null);

  const isDriver = useMemo(
    () => String(authRole ?? "").toLowerCase() === "driver",
    [authRole],
  );

  const profile = profileQuery.data;

  const currentAvatarUrl = avatarPreviewUrl ?? profile?.avatarUrl ?? null;
  const avatarFallback = (form.name || profile?.name || "?")
    .slice(0, 1)
    .toUpperCase();

  const onSubmit = async (event) => {
    event.preventDefault();
    setFormError("");

    const normalizedPhone = normalizePhoneStorage(form.phone);
    const normalizedPlate = normalizeCarPlate(form.carPlateNumber);

    if (!isDriver && !addressLocation) {
      setFormError("Выберите адрес из подсказки, чтобы сохранить координаты.");
      return;
    }

    if (isDriver && !isValidCarPlate(normalizedPlate)) {
      setFormError("Номер авто должен быть в формате M365MH102.");
      return;
    }

    const parsedAddress = parseSingleAddress(form.address);

    if (avatarFile) {
      await uploadAvatarMutation.mutateAsync(avatarFile);
    }

    const body = {
      name: form.name,
      phone: normalizedPhone,
      address: isDriver
        ? null
        : {
            city: parsedAddress.city,
            street: parsedAddress.street,
            house: parsedAddress.house,
            apartment: parsedAddress.apartment,
            latitude: addressLocation?.latitude ?? null,
            longitude: addressLocation?.longitude ?? null,
          },
      carInfo: isDriver
        ? {
            brand: form.carBrand,
            model: form.carModel,
            color: form.carColor,
            plateNumber: normalizedPlate,
          }
        : null,
    };

    await updateMutation.mutateAsync(body);
    setIsEditing(false);
  };

  const onCancelEditing = () => {
    setAvatarPreviewUrl(null);
    setAvatarFile(null);
    setIsEditing(false);
  };

  const onStartEditing = () => {
    setForm({
      name: profile?.name ?? "",
      phone: formatPhoneInput(profile?.phone ?? ""),
      address: formatProfileAddress(profile?.address),
      carBrand: profile?.carInfo?.brand ?? "",
      carModel: profile?.carInfo?.model ?? "",
      carColor: profile?.carInfo?.color ?? "",
      carPlateNumber: normalizeCarPlate(profile?.carInfo?.plateNumber ?? ""),
    });
    setAddressLocation(null);
    setFormError("");
    setAvatarPreviewUrl(null);
    setAvatarFile(null);
    setIsEditing(true);
  };

  const onAvatarChange = (event) => {
    const file = event.target.files?.[0] ?? null;
    if (!file) {
      return;
    }

    setAvatarFile(file);
    setAvatarPreviewUrl(URL.createObjectURL(file));
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Профиль</CardTitle>
      </CardHeader>
      <CardContent>
        <form className="space-y-4" onSubmit={onSubmit}>
          {profileQuery.isError && (
            <Alert variant="destructive">
              {profileQuery.error?.message ?? "Не удалось загрузить профиль"}
            </Alert>
          )}

          {uploadAvatarMutation.isError && (
            <Alert variant="destructive">
              {uploadAvatarMutation.error.message}
            </Alert>
          )}

          {updateMutation.isError && (
            <Alert variant="destructive">{updateMutation.error.message}</Alert>
          )}
          {formError && <Alert variant="destructive">{formError}</Alert>}

          {(updateMutation.isSuccess || uploadAvatarMutation.isSuccess) && (
            <Alert>Профиль обновлён</Alert>
          )}

          <div className="flex justify-center">
            <div className="relative">
              <div className="flex h-24 w-24 items-center justify-center overflow-hidden rounded-full border bg-muted text-2xl font-semibold">
                {currentAvatarUrl ? (
                  <img
                    src={currentAvatarUrl}
                    alt="Аватар"
                    className="h-full w-full object-cover"
                  />
                ) : (
                  avatarFallback
                )}
              </div>

              {isEditing && (
                <button
                  type="button"
                  onClick={() => avatarInputRef.current?.click()}
                  className="absolute bottom-0 right-0 rounded-full border bg-background p-1"
                  aria-label="Изменить аватар"
                >
                  <Camera size={16} />
                </button>
              )}

              <input
                ref={avatarInputRef}
                type="file"
                accept="image/png,image/jpeg,image/webp,image/gif"
                className="hidden"
                onChange={onAvatarChange}
              />
            </div>
          </div>

          {!isEditing ? (
            <div className="space-y-3">
              <ProfileItem label="Name" value={profile?.name || "—"} />
              <ProfileItem label="Эл. почта" value={authEmail || "—"} />
              <ProfileItem label="Телефон" value={profile?.phone || "—"} />

              {!isDriver ? (
                <>
                  <ProfileItem
                    label="Адрес"
                    value={formatProfileAddress(profile?.address) || "—"}
                  />
                </>
              ) : (
                <>
                  <ProfileItem
                    label="Авто: Марка"
                    value={profile?.carInfo?.brand || "—"}
                  />
                  <ProfileItem
                    label="Авто: Модель"
                    value={profile?.carInfo?.model || "—"}
                  />
                  <ProfileItem
                    label="Авто: Цвет"
                    value={profile?.carInfo?.color || "—"}
                  />
                  <ProfileItem
                    label="Авто: Номер"
                    value={profile?.carInfo?.plateNumber || "—"}
                  />
                </>
              )}

              <Button
                type="button"
                onClick={onStartEditing}
                disabled={profileQuery.isLoading}
              >
                Изменить профиль
              </Button>
            </div>
          ) : (
            <>
              <div className="space-y-2">
                <Label htmlFor="profile-name">Имя</Label>
                <Input
                  id="profile-name"
                  value={form.name}
                  onChange={(event) =>
                    setForm((prev) => ({ ...prev, name: event.target.value }))
                  }
                  required
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="profile-email">Эл. почта</Label>
                <Input id="profile-email" value={authEmail ?? ""} readOnly />
              </div>

              <div className="space-y-2">
                <Label htmlFor="profile-phone">Телефон</Label>
                <Input
                  id="profile-phone"
                  value={form.phone}
                  onChange={(event) =>
                    setForm((prev) => ({
                      ...prev,
                      phone: formatPhoneInput(event.target.value),
                    }))
                  }
                  onKeyDown={(event) =>
                    handlePhoneMaskedKeyDown(event, form.phone, (nextPhone) =>
                      setForm((prev) => ({
                        ...prev,
                        phone: nextPhone,
                      })),
                    )
                  }
                  placeholder="+7-(961)-368-16-35"
                />
              </div>

              {!isDriver ? (
                <div className="space-y-4">
                  <AddressAutocompleteInput
                    id="profile-passenger-address"
                    label="Адрес"
                    value={form.address}
                    onChange={(nextValue) => {
                      setForm((prev) => ({ ...prev, address: nextValue }));
                      setAddressLocation(null);
                    }}
                    onSelect={setAddressLocation}
                    placeholder="Введите адрес"
                    required
                  />
                  <div className="grid gap-4 md:grid-cols-2">
                    <div className="space-y-2">
                      <Label htmlFor="profile-address-lat">Широта</Label>
                      <Input
                        id="profile-address-lat"
                        value={addressLocation?.latitude ?? ""}
                        readOnly
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="profile-address-lon">Долгота</Label>
                      <Input
                        id="profile-address-lon"
                        value={addressLocation?.longitude ?? ""}
                        readOnly
                      />
                    </div>
                  </div>
                </div>
              ) : (
                <div className="grid gap-4 md:grid-cols-2">
                  <div className="space-y-2">
                    <Label htmlFor="car-brand">Марка</Label>
                    <select
                      id="car-brand"
                      className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
                      value={form.carBrand}
                      onChange={(event) =>
                        setForm((prev) => ({
                          ...prev,
                          carBrand: event.target.value,
                          carModel: "",
                        }))
                      }
                      required
                    >
                      <option value="">Выберите марку</option>
                      {CAR_BRANDS.map((item) => (
                        <option key={item} value={item}>
                          {item}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="car-model">Модель</Label>
                    <select
                      id="car-model"
                      className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm disabled:cursor-not-allowed disabled:opacity-50"
                      value={form.carModel}
                      onChange={(event) =>
                        setForm((prev) => ({
                          ...prev,
                          carModel: event.target.value,
                        }))
                      }
                      disabled={!form.carBrand}
                      required
                    >
                      <option value="">
                        {form.carBrand
                          ? "Выберите модель"
                          : "Сначала выберите марку"}
                      </option>
                      {(CAR_MODELS_BY_BRAND[form.carBrand] ?? []).map(
                        (item) => (
                          <option key={item} value={item}>
                            {item}
                          </option>
                        ),
                      )}
                    </select>
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="car-color">Цвет (HEX)</Label>
                    <div className="flex items-center gap-2">
                      <Input
                        id="car-color"
                        type="color"
                        className="h-10 w-12 p-1"
                        value={toValidHexColor(form.carColor)}
                        onChange={(event) =>
                          setForm((prev) => ({
                            ...prev,
                            carColor: event.target.value,
                          }))
                        }
                        required
                      />
                      <Input
                        value={form.carColor}
                        onChange={(event) =>
                          setForm((prev) => ({
                            ...prev,
                            carColor: event.target.value,
                          }))
                        }
                        placeholder="#9ca3af"
                        required
                      />
                    </div>
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="car-plate">Номер авто</Label>
                    <Input
                      id="car-plate"
                      value={form.carPlateNumber}
                      onChange={(event) =>
                        setForm((prev) => ({
                          ...prev,
                          carPlateNumber: normalizeCarPlate(event.target.value),
                        }))
                      }
                      placeholder="M365MH102"
                      maxLength={9}
                      required
                    />
                  </div>
                </div>
              )}

              <div className="flex gap-2">
                <Button
                  disabled={
                    updateMutation.isPending ||
                    uploadAvatarMutation.isPending ||
                    profileQuery.isLoading
                  }
                  type="submit"
                >
                  {updateMutation.isPending || uploadAvatarMutation.isPending
                    ? "Сохранение..."
                    : "Сохранить"}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={onCancelEditing}
                >
                  Отмена
                </Button>
              </div>
            </>
          )}
        </form>
      </CardContent>
    </Card>
  );
}

function ProfileItem({ label, value }) {
  return (
    <div>
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="text-sm">{value}</p>
    </div>
  );
}

function toValidHexColor(value) {
  const normalized = String(value ?? "").trim();
  if (/^#[0-9A-Fa-f]{6}$/.test(normalized)) {
    return normalized;
  }

  return "#9ca3af";
}
