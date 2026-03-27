const RUS_TO_LATIN_PLATE_MAP = {
  А: "A",
  В: "B",
  Е: "E",
  К: "K",
  М: "M",
  Н: "H",
  О: "O",
  Р: "P",
  С: "C",
  Т: "T",
  У: "Y",
  Х: "X",
};

function toDigits(value) {
  return String(value ?? "").replace(/\D/g, "");
}

export function normalizePhoneStorage(value) {
  const digits = toDigits(value);
  if (!digits) {
    return "";
  }

  let normalized = digits;

  if (normalized.startsWith("8")) {
    normalized = `7${normalized.slice(1)}`;
  } else if (normalized.startsWith("9")) {
    normalized = `7${normalized}`;
  }

  if (normalized.startsWith("7")) {
    normalized = normalized.slice(0, 11);
  }

  return normalized ? `+${normalized}` : "";
}

export function formatPhoneInput(value) {
  const normalized = normalizePhoneStorage(value);
  if (!normalized) {
    return "";
  }

  const digits = normalized.replace(/\D/g, "");
  if (!digits.startsWith("7")) {
    return `+${digits}`;
  }

  const rest = digits.slice(1);
  const p1 = rest.slice(0, 3);
  const p2 = rest.slice(3, 6);
  const p3 = rest.slice(6, 8);
  const p4 = rest.slice(8, 10);

  let result = "+7";
  if (p1) {
    result += `-(${p1})`;
  }
  if (p2) {
    result += `-${p2}`;
  }
  if (p3) {
    result += `-${p3}`;
  }
  if (p4) {
    result += `-${p4}`;
  }

  return result;
}

export function handlePhoneMaskedKeyDown(event, currentValue, setNextValue) {
  if (event.key !== "Backspace" && event.key !== "Delete") {
    return;
  }

  const input = event.currentTarget;
  const selectionStart = input.selectionStart;
  const selectionEnd = input.selectionEnd;

  if (
    selectionStart == null ||
    selectionEnd == null ||
    selectionStart !== selectionEnd
  ) {
    return;
  }

  const cursor = selectionStart;
  const boundaryIndex = event.key === "Backspace" ? cursor - 1 : cursor;
  if (boundaryIndex < 0 || boundaryIndex >= currentValue.length) {
    return;
  }

  const boundaryChar = currentValue[boundaryIndex];
  if (/\d/.test(boundaryChar)) {
    return;
  }

  const digitPositions = [];
  for (let index = 0; index < currentValue.length; index += 1) {
    if (/\d/.test(currentValue[index])) {
      digitPositions.push(index);
    }
  }

  if (digitPositions.length === 0) {
    return;
  }

  let digitToRemove = -1;

  if (event.key === "Backspace") {
    for (let index = digitPositions.length - 1; index >= 0; index -= 1) {
      if (digitPositions[index] < cursor) {
        digitToRemove = index;
        break;
      }
    }
  } else {
    for (let index = 0; index < digitPositions.length; index += 1) {
      if (digitPositions[index] >= cursor) {
        digitToRemove = index;
        break;
      }
    }
  }

  if (digitToRemove < 0) {
    return;
  }

  event.preventDefault();

  const digits = toDigits(currentValue);
  const nextDigits =
    digits.slice(0, digitToRemove) + digits.slice(digitToRemove + 1);
  const nextFormatted = formatPhoneInput(nextDigits);
  setNextValue(nextFormatted);

  window.requestAnimationFrame(() => {
    const nextCursor = Math.max(
      0,
      Math.min(nextFormatted.length, boundaryIndex),
    );
    input.setSelectionRange(nextCursor, nextCursor);
  });
}

export function normalizeCarPlate(value) {
  const upper = String(value ?? "")
    .toUpperCase()
    .replace(/[АВЕКМНОРСТУХ]/g, (char) => RUS_TO_LATIN_PLATE_MAP[char] ?? char)
    .replace(/[^A-Z0-9]/g, "");

  return upper.slice(0, 9);
}

export function isValidCarPlate(value) {
  const normalized = normalizeCarPlate(value);
  return /^[ABEKMHOPCTYX]\d{3}[ABEKMHOPCTYX]{2}\d{2,3}$/.test(normalized);
}

export function parseSingleAddress(addressValue) {
  const normalized = String(addressValue ?? "").trim();
  if (!normalized) {
    return {
      city: "",
      street: "",
      house: "",
      apartment: null,
    };
  }

  const parts = normalized
    .split(",")
    .map((part) => part.trim())
    .filter(Boolean);

  if (parts.length === 1) {
    return {
      city: parts[0],
      street: parts[0],
      house: "1",
      apartment: null,
    };
  }

  if (parts.length === 2) {
    return {
      city: parts[0],
      street: parts[0],
      house: parts[1],
      apartment: null,
    };
  }

  return {
    city: parts[0],
    street: parts.slice(1, -1).join(", ") || parts[1],
    house: parts.at(-1) ?? "1",
    apartment: null,
  };
}

export function formatProfileAddress(address) {
  if (!address) {
    return "";
  }

  return [address.city, address.street, address.house, address.apartment]
    .map((item) => String(item ?? "").trim())
    .filter(Boolean)
    .join(", ");
}
