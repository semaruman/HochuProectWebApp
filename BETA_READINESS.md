# Beta Readiness — Хочу Проект

Документ описывает состояние MVP после подготовки к **закрытой бете (50–200 пользователей)** без реальных платежей.

## Что было найдено (аудит)

| Область | Состояние до изменений |
|--------|-------------------------|
| Архитектура | ASP.NET Core 8 monolith, Vertical Slice, EF Core + PostgreSQL, Identity cookie auth |
| UI | Статический HTML/CSS/JS в `src/Web/wwwroot` |
| Deal flow | `Accept → Fund → InProgress → Submit → Accept` — fund блокировал beta без платежей |
| Файлы | Загрузка deliverables работала, скачивание для заказчика — нет |
| Email | Только in-app notifications; SMTP не подключён |
| Email verification | В Development — auto-confirm |
| Password reset | API есть, UI отсутствовал |
| Admin | Роли не использовались для модерации |
| Legal | Terms/privacy и checkbox при регистрации отсутствовали |
| CI | Не было |
| Backups | Не документированы |
| Тесты | Unit + 2 integration (happy path, concurrent accept) |

### Lifecycle сделки (beta)

```
Proposal accepted → InProgress → Delivered (Submitted) → [RevisionRequired ↔ Delivered] → Completed → Review
```

`Fund` endpoint сохранён (stub), но **не обязателен**: сделка сразу создаётся в `InProgress` с `FundedAt`.

---

## Что исправлено

### Phase 1 — Critical

1. **Скачивание файлов** — `GET /api/files/deliverable-files/{fileId}` с проверкой участника сделки; кнопка «Скачать» в `deal.html`.
2. **Beta flow без оплаты** — `Deal.FromAcceptedBid()` → `InProgress`; кнопка Fund убрана из UI.
3. **Email verification** — токены через Identity, страницы `verify-email.html`, `verify-email-sent.html`, resend API; в production email не auto-confirm.
4. **Forgot/reset password** — `forgot-password.html`, `reset-password.html`, интеграция с API.
5. **Email notifications** — расширен `MarketplaceEventHandler` + `IEmailService` (SMTP или logging); ошибки отправки не ломают транзакции.

### Phase 2 — Trust / UX

6. **RevisionRequired** — статус, `POST /api/deals/{id}/request-revision`, UI с комментарием.
7. **Статусы и next-action** — human-readable labels в `app.js`, подсказки на `deal.html`.
8. **Ожидание проверки** — отображение даты сдачи и времени ожидания на странице сделки.

### Phase 3 — Admin

9. **Роль Admin** — `RoleSeeder`, policy `Admin`, `admin.html`.
10. **Users** — список, block/unblock (`IsBlocked`).
11. **Projects** — hide/restore (`ProjectStatus.Hidden`).
12. **Deals** — просмотр деталей для админа.

### Phase 4 — Legal

13. **terms.html**, **privacy.html** (beta-level, 152-ФЗ basics).
14. **Checkbox при регистрации** — обязателен на frontend и backend; `TermsAcceptedAt`, `PrivacyPolicyAcceptedAt`.

### Phase 5 — Production

15. **Persistent files** — Docker volume `filesdata` в `docker-compose.yml`.
16. **PostgreSQL** — production DB через `ConnectionStrings__Default`.
17. **Backup scripts** — `scripts/backup-postgres.sh`, `scripts/restore-postgres.sh`.
18. **Secrets** — пустые значения в `appsettings.json`; конфигурация через env vars.
19. **CI** — `.github/workflows/ci.yml` (restore, build, test с PostgreSQL service).

### Phase 6 — Quality

20. **Migration** — `20260828035400_BetaReadiness`.
21. **Тесты** — unit (deal flow, terms validator) + integration (download auth, admin, revision, password reset).

---

## Изменённые / добавленные файлы (основные)

### Domain
- `src/Web/Domain/Entities/Deal.cs` — InProgress on create, `RequestRevision`
- `src/Web/Domain/Entities/Project.cs` — `Hide()`, `RestorePublication()`
- `src/Web/Domain/Entities/ApplicationUser.cs` — `IsBlocked`, terms timestamps
- `src/Web/Domain/Enums/DealStatus.cs` — `RevisionRequired`
- `src/Web/Domain/Enums/ProjectStatus.cs` — `Hidden`
- `src/Web/Domain/Events/MarketplaceEvents.cs` — `WorkRevisionRequested`

### Features
- `src/Web/Features/Files/FilesEndpoints.cs` — **new**
- `src/Web/Features/Admin/AdminEndpoints.cs` — **new**
- `src/Web/Features/Auth/AuthEndpoints.cs` — verification, terms, blocked check
- `src/Web/Features/Auth/PasswordResetEndpoints.cs` — email sending
- `src/Web/Features/Deals/DealHandlers.cs` — `RequestRevisionHandler`
- `src/Web/Features/Deals/DealsEndpoints.cs` — request-revision

### Infrastructure
- `src/Web/Infrastructure/Email/*` — **new**
- `src/Web/Infrastructure/DomainEvents/MarketplaceEventHandler.cs` — emails
- `src/Web/Infrastructure/DomainEvents/DomainEventDbContextExtensions.cs` — dispatch after save
- `src/Web/Infrastructure/Persistence/RoleSeeder.cs` — **new**
- `src/Web/Common/Auth/AccountGuards.cs` — active user + admin role

### UI (`wwwroot`)
- `deal.html`, `register.html`, `login.html`, `app.js`, `api.js`
- **new:** `forgot-password.html`, `reset-password.html`, `verify-email.html`, `verify-email-sent.html`, `admin.html`, `terms.html`, `privacy.html`

### Tests
- `tests/Web.IntegrationTests/IntegrationTestHost.cs` — **new**
- `tests/Web.IntegrationTests/BetaReadinessTests.cs` — **new**
- `tests/Web.IntegrationTests/MarketplaceFlowTests.cs` — refactored
- `tests/Web.UnitTests/DomainTests.cs` — beta flow tests

### DevOps
- `.github/workflows/ci.yml` — **new**
- `scripts/backup-postgres.sh`, `scripts/restore-postgres.sh` — **new**
- `src/Web/Infrastructure/Persistence/Migrations/20260828035400_BetaReadiness.cs` — **new**

---

## Migrations

| Migration | Изменения |
|-----------|-----------|
| `20260828035400_BetaReadiness` | `Deals.LastRevisionComment`, `Deals.RevisionRequestedAt`, `AspNetUsers.IsBlocked`, `TermsAcceptedAt`, `PrivacyPolicyAcceptedAt` |

Применяется автоматически при `Database:MigrateOnStartup=true`.

---

## API endpoints (новые / изменённые)

| Method | Path | Описание |
|--------|------|----------|
| GET | `/api/files/deliverable-files/{fileId}` | Скачать файл deliverable (участник сделки) |
| GET | `/api/files/project-attachments/{attachmentId}` | Скачать вложение проекта |
| POST | `/api/deals/{id}/request-revision` | Вернуть на доработку (buyer) |
| POST | `/api/auth/confirm-email` | Подтвердить email |
| POST | `/api/auth/resend-confirmation` | Повторная отправка |
| POST | `/api/auth/forgot-password` | Запрос сброса пароля |
| POST | `/api/auth/reset-password` | Сброс пароля |
| GET | `/api/admin/users` | Список пользователей (Admin) |
| POST | `/api/admin/users/{id}/block` | Заблокировать |
| POST | `/api/admin/users/{id}/unblock` | Разблокировать |
| GET | `/api/admin/projects` | Список проектов |
| POST | `/api/admin/projects/{id}/hide` | Снять с публикации |
| POST | `/api/admin/projects/{id}/restore` | Вернуть в публикацию |
| GET | `/api/admin/deals/{id}` | Просмотр сделки |

**Поведение accept bid:** сделка создаётся сразу в `InProgress` (без обязательного `/fund`).

---

## Тесты

### Unit (`Web.UnitTests`) — 12 тестов
- Deal beta flow: accept → submit → revision → resubmit → accept
- `RegisterValidator_RejectsMissingTerms`
- Domain state machines, validators

### Integration (`Web.IntegrationTests`) — 9 тестов
Требуют PostgreSQL (Testcontainers или `HOCHU_TEST_PG` / `ConnectionStrings__Default`):

- Happy path marketplace flow
- Concurrent accept bid
- Accept bid → InProgress without fund
- Revision → redeliver → accept
- File download: authorized / forbidden / not found
- Admin: 403 for regular user, 200 for admin
- Admin block user + hide project
- Register requires terms
- Password reset valid/invalid token

---

## Environment variables

| Variable | Описание | Пример |
|----------|----------|--------|
| `ConnectionStrings__Default` | PostgreSQL connection string | `Host=db;Port=5432;Database=hochuproect;Username=postgres;Password=***` |
| `Database__MigrateOnStartup` | Auto-migrate on start | `true` |
| `FileStorage__Root` | Путь к uploaded files | `/app/App_Data/files` |
| `App__PublicBaseUrl` | Публичный URL для ссылок в email | `https://app.example.com` |
| `Email__Enabled` | Включить SMTP | `true` |
| `Email__Host` | SMTP host | `smtp.example.com` |
| `Email__Port` | SMTP port | `587` |
| `Email__UseSsl` | SSL/TLS | `true` |
| `Email__User` | SMTP user | — |
| `Email__Password` | SMTP password | — |
| `Email__FromAddress` | From address | `noreply@example.com` |
| `Email__FromName` | From name | `Хочу Проект` |
| `Admin__Email` | Начальный admin (создаётся при старте) | `admin@example.com` |
| `Admin__Password` | Пароль admin | — |
| `ASPNETCORE_ENVIRONMENT` | `Development` / `Production` | `Production` |
| `ASPNETCORE_URLS` | Listen URLs | `http://+:8080` |

**Не храните секреты в git.** Используйте env vars, Docker secrets или CI secrets.

При `Email__Enabled=false` используется `LoggingEmailService` (письма в лог).

---

## Локальный запуск

### Docker (рекомендуется)

```bash
docker compose up --build
```

- UI: http://localhost:8080
- Swagger (Development): http://localhost:8080/swagger
- Health: http://localhost:8080/health

### Без Docker

```bash
docker compose up db -d
cd src/Web
dotnet run
```

UI: http://localhost:5121 (или порт из `launchSettings.json`)

### Тесты

```bash
# Unit only (без PostgreSQL)
dotnet test tests/Web.UnitTests

# Integration (нужен Docker для Testcontainers или внешний PG)
dotnet test tests/Web.IntegrationTests
```

---

## Production deployment

### HTTPS и домен

1. Разместите приложение за reverse proxy (nginx, Caddy, Traefik) с TLS-сертификатом (Let's Encrypt).
2. Установите `App__PublicBaseUrl=https://your-domain.com`.
3. Настройте `ForwardedHeaders` при необходимости (за proxy).

Docker-образ слушает HTTP на `:8080`; TLS терминируется на proxy.

### PostgreSQL

- Используйте managed PostgreSQL или отдельный контейнер/VM с volume `pgdata`.
- `ConnectionStrings__Default` — через secrets.

### Файлы (persistent storage)

В `docker-compose.yml` volume `filesdata` монтируется в `/app/App_Data/files`.

**При redeploy файлы не должны теряться** — используйте named volume или внешнее хранилище.

### Backup PostgreSQL

```bash
export PGHOST=localhost PGPORT=5432 PGUSER=postgres PGDATABASE=hochuproect
./scripts/backup-postgres.sh ./backups
```

Рекомендуется: ежедневный cron + хранение off-site (S3, другой сервер).

### Восстановление

```bash
./scripts/restore-postgres.sh ./backups/hochuproect_YYYYMMDDTHHMMSSZ.sql.gz
```

Перед restore остановите приложение или переведите в maintenance mode.

### CI

GitHub Actions: push/PR на `main`, `master`, `VerticalSlice-migration` → restore, build, test с PostgreSQL service.

---

## Проверочный чеклист (UX audit)

| Сценарий | Статус |
|----------|--------|
| Регистрация → verify email → login | ✅ (production: email required) |
| Create project → bid → accept → InProgress | ✅ |
| Upload deliverable → download | ✅ |
| Accept / revision flow | ✅ |
| Review after complete | ✅ |
| Forgot → reset password | ✅ |
| Admin users/projects/deals | ✅ |
| Blocked user cannot act | ✅ |
| Terms checkbox required | ✅ |
| No payment blocker in UI | ✅ |

---

## TODO после запуска beta

- [ ] Настроить реальный SMTP и проверить доставляемость
- [ ] Настроить домен + HTTPS на production
- [ ] Задать `Admin__Email` / `Admin__Password` через secrets
- [ ] Настроить автоматические backup + мониторинг
- [ ] Email reminder если заказчик долго не проверяет сдачу (background job)
- [ ] Удалить или архивировать неиспользуемые Razor Pages (`src/Web/Pages/`)
- [ ] Rate limiting tuning под реальную нагрузку
- [ ] Мониторинг ошибок (Sentry и т.п.)

---

## Remaining after beta

Сознательно **не реализовано**:

- Реальные платежи (`IPaymentService` production)
- Escrow и удержание средств
- Комиссии платформы
- KYC / верификация личности
- Anti-fraud
- SignalR / real-time chat
- Мобильное приложение
- Продвинутый поиск
- Полноценный каталог Services
- Сложное портфолио
- Arbitration system
- Автоматическое завершение сделки по timeout без проверки заказчиком
- Микросервисная архитектура
- S3 storage (abstraction готова через `IFileStorage`, но не подключена)

---

*Документ сгенерирован по итогам подготовки к closed beta. Юридические тексты terms/privacy — beta-level шаблоны, не являются юридической консультацией.*
