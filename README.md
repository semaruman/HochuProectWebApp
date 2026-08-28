# Хочу Проект — инженерная freelance-биржа (MVP)

Монолит на **ASP.NET Core 8** с Vertical Slice Architecture, Razor Pages + Minimal API, Identity, EF Core и PostgreSQL.

## Быстрый старт (Docker)

```bash
docker compose up --build
```

Откройте http://localhost:8080

Интерфейс — статический HTML/CSS/JS в `src/Web/wwwroot` (главная, проекты, сделки, вход). API — `/api/*`, Swagger — `/swagger`.

## Локальная разработка

1. Поднимите Postgres: `docker compose up db -d`
2. В `src/Web`: `dotnet run`
3. UI: http://localhost:5121 · Swagger: `/swagger` · Health: `/health`

## Структура

```
src/Web/
  Features/          # Vertical slices (Auth, Projects, Bids, Deals, Chat, ...)
  Domain/            # Entities + enums
  Infrastructure/    # EF, Identity, files, payments stub, audit
  Pages/             # Razor UI
tests/
  Web.UnitTests/
  Web.IntegrationTests/
```

## Основной сценарий MVP (closed beta)

Регистрация → подтверждение email → создание/публикация проекта → отклик → принятие отклика (сделка сразу **в работе**) → чат → сдача → приёмка или доработка → отзыв.

Оплата в beta **не используется** (`StubPaymentService` остаётся в коде, но не блокирует flow). Подробности — в [BETA_READINESS.md](BETA_READINESS.md).
