# SecureAuth

Невеликий ASP.NET Core API для тестового завдання: двофазна автентифікація з `ApiSignature`.

## Як працює автентифікація

1. Клієнт викликає `POST /auth/login` з `login`, `password`, `apiSignature`, `requestDate`.
2. Сервер спочатку перевіряє `ApiSignature` і freshness запиту. Бізнес-логіка не виконується, якщо підпис неправильний або timestamp застарілий.
3. Якщо credentials правильні, сервер створює opaque `simpleToken` з TTL 5 хвилин.
4. Клієнт викликає `POST /auth/token` з `simpleToken`, `apiSignature`, `requestDate`.
5. Сервер знову спочатку перевіряє підпис і freshness, потім одноразово споживає `simpleToken`.
6. Якщо все коректно, сервер створює opaque `fullToken` з TTL 24 години.
7. `POST /auth/logout` перевіряє підпис і freshness, після цього видаляє `fullToken`.

Токени генеруються через `RandomNumberGenerator`. JWT не використовується.

## ApiSignature

```text
ApiSignature = SHA-256(StaticKey + RequestDate)
```

`StaticKey` береться з конфігурації `Security:StaticKey`.
`RequestDate` - Unix timestamp у мілісекундах UTC.
`ApiSignature` передається як hex-рядок SHA-256.

PowerShell-приклад:

```powershell
$requestDate = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$payload = "dev-static-key-change-me$requestDate"
$apiSignature = [Convert]::ToHexString(
    [Security.Cryptography.SHA256]::HashData(
        [Text.Encoding]::UTF8.GetBytes($payload)
    )
).ToLowerInvariant()
```

`requestDate` без freshness не захищає від replay-атак. Якщо зловмисник перехопить підписаний запит, він зможе повторити його пізніше з тим самим timestamp і підписом. Тому сервер відхиляє запити, час яких відрізняється від поточного UTC часу більше ніж на 5 хвилин.

## Помилки

Усі помилки повертаються в одному JSON-форматі:

```json
{
  "error": "invalid_signature",
  "message": "Request signature is invalid."
}
```

## Endpoint-и

У прикладах нижче `requestDate` потрібно замінити на поточний Unix timestamp у мілісекундах, а `apiSignature` - на SHA-256 від `StaticKey + requestDate`.

### POST /auth/login

```json
{
  "login": "demo",
  "password": "DemoPassword123!",
  "apiSignature": "sha256-hex-of-static-key-plus-requestDate",
  "requestDate": 1779100000000
}
```

Успішна відповідь:

```json
{
  "token": "simple-token",
  "expiresAt": "2026-05-18T12:00:00+00:00"
}
```

### POST /auth/token

```json
{
  "simpleToken": "simple-token",
  "apiSignature": "sha256-hex-of-static-key-plus-requestDate",
  "requestDate": 1779100000000
}
```

Успішна відповідь:

```json
{
  "token": "full-token",
  "expiresAt": "2026-05-19T12:00:00+00:00"
}
```

### POST /auth/logout

```json
{
  "fullToken": "full-token",
  "apiSignature": "sha256-hex-of-static-key-plus-requestDate",
  "requestDate": 1779100000000
}
```

Успішна відповідь: `200 OK`.

## Demo user

Локальна конфігурація містить користувача:

- login: `demo`
- password: `DemoPassword123!`

Пароль зберігається в `appsettings.json` тільки як PBKDF2-SHA256 hash.
