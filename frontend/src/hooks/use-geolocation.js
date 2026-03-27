import { useCallback, useState } from "react";

export function useGeolocation() {
  const [location, setLocation] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const getCurrentPosition = useCallback(() => {
    return new Promise((resolve, reject) => {
      if (!navigator.geolocation) {
        const message = "Geolocation не поддерживается в этом браузере";
        setError(message);
        reject(new Error(message));
        return;
      }

      setLoading(true);
      setError(null);

      navigator.geolocation.getCurrentPosition(
        (position) => {
          const loc = {
            latitude: position.coords.latitude,
            longitude: position.coords.longitude,
          };
          setLocation(loc);
          setLoading(false);
          resolve(loc);
        },
        (geoError) => {
          let message = "Ошибка геолокации";
          switch (geoError.code) {
            case geoError.PERMISSION_DENIED:
              message = "Пользователь отклонил запрос на геолокацию";
              break;
            case geoError.POSITION_UNAVAILABLE:
              message = "Данные о местоположении недоступны";
              break;
            case geoError.TIMEOUT:
              message = "Таймаут при получении местоположения";
              break;
            default:
              message = "Неизвестная ошибка геолокации";
              break;
          }

          setError(message);
          setLoading(false);
          reject(new Error(message));
        },
        {
          enableHighAccuracy: true,
          timeout: 10000,
          maximumAge: 60000,
        },
      );
    });
  }, []);

  return {
    location,
    loading,
    error,
    getCurrentPosition,
  };
}
