# Deploy (Portainer + Repository)

## 1. Что в репозитории

- docker-compose: ../../docker-compose.yml
- nginx config: ./nginx/conf.d/minimal-taxi.conf
- PostGIS DB init: ./postgres/init/01-create-databases.sql
- env template: ../../.env.example

## 2. Подготовка .env

1. Скопируйте ../../.env.example в .env на сервере.
2. Заполните секреты и ключи.
3. Проверьте, что `FRONTEND_BASE_URL=https://minimal-taxi.ru`.

Важно: в `appsettings.json` не нужно писать `${...}`. В ASP.NET Core переменные окружения автоматически переопределяют значения из `appsettings` по имени ключа.

Примеры соответствий:

- `ConnectionStrings:AuthDb` -> `ConnectionStrings__AuthDb`
- `ConnectionStrings:MinimalTaxiServiceDb` -> `ConnectionStrings__MinimalTaxiServiceDb`
- `ConnectionStrings:Redis` -> `ConnectionStrings__Redis`
- `Jwt:SecretKey` -> `Jwt__SecretKey`
- `Email:Pass` -> `Email__Pass`
- `ProfileSync:InternalKey` -> `ProfileSync__InternalKey`
- `InternalApi:Key` -> `InternalApi__Key`

Именно поэтому compose передает env в таком формате с `__`, и приложение подхватывает их без дополнительных ссылок в json.

## 3. Portainer (Repository)

1. Добавьте stack из Git repository.
2. Укажите compose file path: `docker-compose.yml`.
3. Подключите env variables из `.env`.
4. Deploy stack.

## 4. Что поднимается

- `postgres` на базе `postgis/postgis:16-3.4`
- `redis` (для distributed cache + HybridCache)
- `auth-service` (ASP.NET Core)
- `minimal-taxi-service` (ASP.NET Core)
- `frontend` (nginx со статикой)
- `nginx` reverse-proxy на `minimal-taxi.ru`

## 5. Маршруты nginx

- `/` -> frontend
- `/api/*` -> auth-service
- `/taxi/*` -> minimal-taxi-service
- `/taxi/trips/events` -> SSE c отключенным buffering

## 6. Важно

- Internal sync ключ должен совпадать:
  - `ProfileSync__InternalKey` (auth-service)
  - `InternalApi__Key` (minimal-taxi-service)
- Для HTTPS добавьте TLS-терминацию (например, внешний reverse-proxy/certbot).
