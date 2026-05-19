# SecureAuth

SecureAuth - невеликий ASP.NET Core 8 Web API для тестового завдання з двоетапною автентифікацією. Проект показує повний потік: перевірка логіна і пароля, видача короткого `simpleToken`, одноразовий обмін на `fullToken`, logout і підпис кожного запиту через `ApiSignature`.

У проекті навмисно не використовуються JWT, готові auth-фреймворки або база даних. Так легше побачити саме реалізацію вимог: як генеруються opaque-токени, як перевіряється підпис, як працює freshness window і як сервер не дає використати `simpleToken` двічі.

## Зміст

- [Можливості](#можливості)
- [Як працює auth flow](#як-працює-auth-flow)
- [Безпекова модель](#безпекова-модель)
- [Запуск проекту](#запуск-проекту)
- [Demo user і пароль](#demo-user-і-пароль)
- [ApiSignature](#apisignature)
- [Endpoints](#endpoints)
- [Формат помилок](#формат-помилок)
- [Конфігурація](#конфігурація)
- [Тести](#тести)
- [Структура проекту](#структура-проекту)
- [Обмеження поточної реалізації](#обмеження-поточної-реалізації)

## Можливості

- `POST /auth/login` перевіряє credentials і повертає короткоживучий `simpleToken`.
- `POST /auth/token` приймає `simpleToken`, видаляє його зі сховища і повертає `fullToken`.
- `POST /auth/logout` видаляє активний `fullToken`.
- Кожен `/auth/*` запит має містити `apiSignature` і `requestDate`.
- Підпис і freshness перевіряються до бізнес-логіки, у `ValidateApiSignatureFilter`.
- Токени є opaque-рядками, а не JWT.
- Паролі зберігаються у форматі PBKDF2-SHA256 hash.
- Сховище токенів thread-safe, побудоване на `ConcurrentDictionary`.
- Прострочені токени періодично прибирає `ExpiredTokenCleanupService`.
- Помилки повертаються в одному JSON-форматі.
- Є unit та integration тести для основних сценаріїв.

## Як працює auth flow

1. Клієнт надсилає `POST /auth/login` з `login`, `password`, `apiSignature` і `requestDate`.
2. API спочатку перевіряє підпис і timestamp.
3. Якщо підпис валідний, `AuthService` перевіряє пароль через `PasswordHasher`.
4. Сервер генерує `simpleToken` і зберігає його як короткоживучий токен.
5. Клієнт надсилає `POST /auth/token` з отриманим `simpleToken`.
6. Сервер атомарно видаляє `simpleToken` і повертає `fullToken`.
7. Клієнт може завершити сесію через `POST /auth/logout`, передавши `fullToken`.

Ключовий момент: `simpleToken` можна використати тільки один раз. Якщо два запити одночасно спробують обміняти той самий `simpleToken`, успішним буде тільки один.

## Безпекова модель

У цьому проекті є кілька окремих шарів захисту:

- `ApiSignature` підтверджує, що клієнт знає `StaticKey`.
- `requestDate` разом із freshness window зменшує ризик replay-атак.
- Пароль не зберігається у plain text.
- Токени генеруються через `RandomNumberGenerator`, а не через `Guid.NewGuid()`.
- Порівняння hash/signature виконується через `CryptographicOperations.FixedTimeEquals`.
- `simpleToken` видаляється атомарно, тому повторне використання блокується навіть при паралельних запитах.

Важливо: `StaticKey` у `appsettings.json` є demo-значенням. Для реального середовища його треба винести в secrets/env-змінні і замінити на достатньо довгий випадковий ключ.

## Запуск проекту

Потрібен .NET SDK 8 або новіший SDK, який вміє збирати `net8.0`.

```powershell
dotnet restore SecureAuth.sln
dotnet run --project src/SecureAuth/SecureAuth.csproj
```

У Development доступний Swagger. Локальна адреса береться з `src/SecureAuth/Properties/launchSettings.json`, за замовчуванням:

```text
http://localhost:5015/swagger
```

Також у проекті є файл з ручними HTTP-прикладами:

```text
src/SecureAuth/SecureAuth.http
```

## Demo user і пароль

У `appsettings.json` є demo-користувач:

```text
login: demo
```

Plain text пароль не зберігається в репозиторії. Це зроблено навмисно: навіть для demo-сценарію краще не залишати готовий пароль у коді або документації.

Для локальної перевірки можна:

1. Взяти власний пароль.
2. Згенерувати для нього PBKDF2-SHA256 hash.
3. Підставити hash у локальну конфігурацію або env-змінну.

Приклад генерації hash:

```powershell
$password = Read-Host "Password"
$salt = [Security.Cryptography.RandomNumberGenerator]::GetBytes(16)
$hash = [Security.Cryptography.Rfc2898DeriveBytes]::Pbkdf2(
    $password,
    $salt,
    100000,
    [Security.Cryptography.HashAlgorithmName]::SHA256,
    32
)

"pbkdf2-sha256:100000:{0}:{1}" -f [Convert]::ToBase64String($salt), [Convert]::ToBase64String($hash)
```

Приклад env-змінної для локального запуску:

```powershell
$env:Security__SeedUsers__0__Login = "demo"
$env:Security__SeedUsers__0__PasswordHash = "pbkdf2-sha256:100000:..."
```

## ApiSignature

Кожен auth-запит підписується так:

```text
ApiSignature = SHA-256(StaticKey + RequestDate)
```

Де:

- `StaticKey` - секретний рядок з `Security:StaticKey`;
- `RequestDate` - Unix timestamp у мілісекундах UTC;
- `ApiSignature` - SHA-256 hash у lowercase hex-форматі.

PowerShell-приклад:

```powershell
$requestDate = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$payload = "dev-static-key-change-me$requestDate"
$apiSignature = [Convert]::ToHexString(
    [Security.Cryptography.SHA256]::HashData(
        [Text.Encoding]::UTF8.GetBytes($payload)
    )
).ToLowerInvariant()

"requestDate=$requestDate"
"apiSignature=$apiSignature"
```

Чому потрібен freshness check:

`requestDate` сам по собі не захищає від replay-атак. Якщо атакувальник перехопить валідний підписаний запит, він зможе повторити його пізніше з тим самим timestamp і підписом. Тому сервер приймає тільки ті запити, час яких відрізняється від поточного UTC не більше ніж на `Security:RequestFreshnessMinutes`.

За замовчуванням freshness window дорівнює 5 хвилинам.

## Endpoints

Усі endpoints приймають JSON body. `apiSignature` і `requestDate` передаються в body, а не в headers.

У прикладах нижче `requestDate` і `apiSignature` треба замінити на актуальні значення. Старий timestamp майже напевно дасть `401 stale_request`.

### POST /auth/login

Перевіряє логін і пароль, після чого повертає `simpleToken`.

Request:

```json
{
  "login": "demo",
  "password": "your-local-password",
  "apiSignature": "sha256-hex-of-static-key-plus-requestDate",
  "requestDate": 1779100000000
}
```

Response `200 OK`:

```json
{
  "token": "simple-token",
  "expiresAt": "2026-05-18T12:00:00+00:00"
}
```

Можливі помилки:

- `400 invalid_request`
- `401 invalid_signature`
- `401 stale_request`
- `401 invalid_credentials`

### POST /auth/token

Обмінює `simpleToken` на `fullToken`. Після успішного обміну `simpleToken` видаляється.

Request:

```json
{
  "simpleToken": "simple-token",
  "apiSignature": "sha256-hex-of-static-key-plus-requestDate",
  "requestDate": 1779100000000
}
```

Response `200 OK`:

```json
{
  "token": "full-token",
  "expiresAt": "2026-05-19T12:00:00+00:00"
}
```

Можливі помилки:

- `400 invalid_request`
- `401 invalid_signature`
- `401 stale_request`
- `401 invalid_simple_token`

### POST /auth/logout

Видаляє активний `fullToken`.

Request:

```json
{
  "fullToken": "full-token",
  "apiSignature": "sha256-hex-of-static-key-plus-requestDate",
  "requestDate": 1779100000000
}
```

Response:

```text
200 OK
```

Можливі помилки:

- `400 invalid_request`
- `401 invalid_signature`
- `401 stale_request`
- `401 invalid_full_token`

## Формат помилок

Усі помилки повертаються в одному форматі:

```json
{
  "error": "invalid_signature",
  "message": "The request signature is missing or invalid."
}
```

Основні коди:

| HTTP status | `error` | Коли виникає |
| --- | --- | --- |
| 400 | `invalid_request` | Некоректний JSON або відсутні обов'язкові поля |
| 401 | `invalid_signature` | Підпис відсутній, має неправильний формат або не збігається |
| 401 | `stale_request` | `requestDate` поза дозволеним часовим вікном |
| 401 | `invalid_credentials` | Невірний логін або пароль |
| 401 | `invalid_simple_token` | `simpleToken` відсутній, протермінований або вже використаний |
| 401 | `invalid_full_token` | `fullToken` відсутній, протермінований або вже видалений |
| 404 | `not_found` | Немає такого endpoint |
| 405 | `method_not_allowed` | Endpoint є, але HTTP method неправильний |
| 500 | `internal_server_error` | Неочікувана помилка сервера |

## Конфігурація

Основна секція знаходиться в `Security`.

| Key | Default | Опис |
| --- | --- | --- |
| `Security:StaticKey` | `dev-static-key-change-me` | Demo-ключ для підпису запитів |
| `Security:RequestFreshnessMinutes` | `5` | Максимальна різниця між `requestDate` і поточним UTC |
| `Security:SimpleTokenTtlMinutes` | `5` | Час життя `simpleToken` |
| `Security:FullTokenTtlHours` | `24` | Час життя `fullToken` |
| `Security:CleanupIntervalMinutes` | `1` | Як часто background service прибирає прострочені токени |
| `Security:SeedUsers` | `demo` user | Початкові користувачі для in-memory сховища |

Для production-подібного запуску мінімум треба замінити:

- `Security:StaticKey`
- `Security:SeedUsers`
- будь-які demo-значення з `appsettings.json`

## Тести

Запуск:

```powershell
dotnet test SecureAuth.sln
```

Що перевіряється:

- успішний login;
- одноразове використання `simpleToken`;
- logout для `fullToken`;
- невалідний `ApiSignature`;
- застарілий `requestDate`;
- malformed request body;
- відсутні обов'язкові поля;
- конкурентне споживання одного токена;
- валідація `SecurityOptions`;
- інтеграційний flow через ASP.NET Core test host.

## Структура проекту

```text
SecureAuth
├── src/SecureAuth
│   ├── Background
│   │   └── ExpiredTokenCleanupService.cs
│   ├── Config
│   │   ├── SecurityOptions.cs
│   │   └── SeedUserOptions.cs
│   ├── Contracts
│   │   ├── LoginRequest.cs
│   │   ├── TokenRequest.cs
│   │   ├── LogoutRequest.cs
│   │   ├── TokenResponse.cs
│   │   └── ErrorResponse.cs
│   ├── Controllers
│   │   └── AuthController.cs
│   ├── Filters
│   │   ├── ValidateApiSignatureAttribute.cs
│   │   └── ValidateApiSignatureFilter.cs
│   ├── Models
│   ├── Services
│   │   ├── ApiSignatureValidator.cs
│   │   ├── AuthService.cs
│   │   ├── PasswordHasher.cs
│   │   └── TokenGenerator.cs
│   ├── Storage
│   │   ├── InMemoryTokenStore.cs
│   │   └── InMemoryUserStore.cs
│   └── Program.cs
├── test/SecureAuth.Tests
├── docs
└── SecureAuth.sln
```

## Обмеження поточної реалізації

Це тестовий проект, тому деякі речі залишені простими:

- користувачі і токени зберігаються in-memory;
- після перезапуску API всі активні токени зникають;
- немає refresh token flow;
- немає rate limiting;
- `StaticKey` один для всіх demo-клієнтів;
- `ApiSignature` підписує тільки `StaticKey + RequestDate`, а не весь request body;
- немає audit log або persistence для login/logout подій.

Для production-версії я б у першу чергу виніс токени в persistent/distributed storage, додав rate limiting, rotation для ключів, нормальне керування користувачами і підпис більшої частини запиту, щоб сильніше прив'язати signature до конкретного payload.

## Додаткова документація

- [Технічне завдання](docs/TECH_TASK.md)
- [Архітектурні нотатки](docs/ArchitectureDiagrams.md)
- [HTTP-приклади](src/SecureAuth/SecureAuth.http)
