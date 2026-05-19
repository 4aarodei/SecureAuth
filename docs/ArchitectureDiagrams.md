# SecureAuth: архітектурні нотатки

Це короткий технічний огляд проекту: як запит проходить через API, де лежить основна логіка і чому кілька речей зроблені саме так. Він не замінює код, але допомагає швидко зорієнтуватися перед читанням.

## Основний сценарій

```mermaid
flowchart TD
    Client["Клієнт"] --> Login["POST /auth/login"]
    Login --> Signature1["ValidateApiSignatureFilter"]
    Signature1 -->|valid| LoginAction["AuthController.Login"]
    Signature1 -->|invalid / stale| AuthError["401 JSON error"]
    LoginAction --> AuthLogin["AuthService.Login"]
    AuthLogin --> Users["InMemoryUserStore"]
    AuthLogin --> Hasher["PasswordHasher.Verify"]
    AuthLogin -->|valid credentials| SimpleToken["Generate simpleToken"]
    SimpleToken --> TokenStore1["InMemoryTokenStore.Add"]
    TokenStore1 --> SimpleResponse["200 TokenResponse"]

    Client --> Exchange["POST /auth/token"]
    Exchange --> Signature2["ValidateApiSignatureFilter"]
    Signature2 -->|valid| TokenAction["AuthController.Token"]
    Signature2 -->|invalid / stale| AuthError
    TokenAction --> ExchangeService["AuthService.ExchangeSimpleToken"]
    ExchangeService --> Consume["InMemoryTokenStore.TryConsumeSimpleToken"]
    Consume -->|valid and unused| FullToken["Generate fullToken"]
    Consume -->|missing / expired / reused| SimpleError["401 invalid_simple_token"]
    FullToken --> TokenStore2["InMemoryTokenStore.Add"]
    TokenStore2 --> FullResponse["200 TokenResponse"]

    Client --> Logout["POST /auth/logout"]
    Logout --> Signature3["ValidateApiSignatureFilter"]
    Signature3 -->|valid| LogoutAction["AuthController.Logout"]
    Signature3 -->|invalid / stale| AuthError
    LogoutAction --> LogoutService["AuthService.Logout"]
    LogoutService --> RemoveFull["InMemoryTokenStore.TryRemoveFullToken"]
    RemoveFull -->|removed| Ok["200 OK"]
    RemoveFull -->|missing / expired| FullError["401 invalid_full_token"]
```

## Послідовність login і token exchange

```mermaid
sequenceDiagram
    participant Client as Client
    participant Filter as ValidateApiSignatureFilter
    participant Controller as AuthController
    participant Service as AuthService
    participant Tokens as InMemoryTokenStore

    Client->>Filter: POST /auth/login + credentials + signature
    Filter->>Filter: Validate(apiSignature, requestDate)
    alt Підпис або дата невалідні
        Filter-->>Client: 401 invalid_signature / stale_request
    else Запит валідний
        Filter->>Controller: Pass request
        Controller->>Service: Login(login, password)
        Service->>Service: Verify password hash
        Service->>Tokens: Add(simpleToken)
        Service-->>Controller: TokenResponse(simpleToken)
        Controller-->>Client: 200 OK
    end

    Client->>Filter: POST /auth/token + simpleToken + signature
    Filter->>Filter: Validate(apiSignature, requestDate)
    alt Запит невалідний
        Filter-->>Client: 401 invalid_signature / stale_request
    else Запит валідний
        Filter->>Controller: Pass request
        Controller->>Service: ExchangeSimpleToken(simpleToken)
        Service->>Tokens: TryConsumeSimpleToken(simpleToken)
        alt Токен існує, не протермінований і ще не використаний
            Service->>Tokens: Add(fullToken)
            Service-->>Controller: TokenResponse(fullToken)
            Controller-->>Client: 200 OK
        else Токен невалідний
            Service-->>Controller: null
            Controller-->>Client: 401 invalid_simple_token
        end
    end
```

## Життєвий цикл токенів

```mermaid
stateDiagram-v2
    [*] --> NoToken
    NoToken --> SimpleTokenIssued: /auth/login
    SimpleTokenIssued --> FullTokenIssued: /auth/token
    SimpleTokenIssued --> Expired: simpleToken TTL минув
    FullTokenIssued --> LoggedOut: /auth/logout
    FullTokenIssued --> Expired: fullToken TTL минув
    Expired --> Cleaned: ExpiredTokenCleanupService
    LoggedOut --> [*]
    Cleaned --> [*]
```

## Карта файлів

### Точка входу

- `src/SecureAuth/Program.cs` - конфігурація DI, controllers, Swagger, обробка помилок і запуск API.

### HTTP API

- `src/SecureAuth/Controllers/AuthController.cs` - тонкий контролер для `/auth/login`, `/auth/token`, `/auth/logout`.
- `src/SecureAuth/Contracts/*` - DTO запитів, відповідей і єдиний формат помилок.
- `src/SecureAuth/Filters/ValidateApiSignatureAttribute.cs` - підключає фільтр до контролера.
- `src/SecureAuth/Filters/ValidateApiSignatureFilter.cs` - централізовано перевіряє підпис і freshness до бізнес-логіки.

### Бізнес-логіка

- `src/SecureAuth/Services/AuthService.cs` - координує login, обмін токена і logout.
- `src/SecureAuth/Services/ApiSignatureValidator.cs` - перевіряє `SHA-256(StaticKey + requestDate)` і часове вікно.
- `src/SecureAuth/Services/PasswordHasher.cs` - перевіряє PBKDF2-SHA256 hash пароля.
- `src/SecureAuth/Services/TokenGenerator.cs` - генерує криптографічно стійкі opaque-токени.

### Сховище

- `src/SecureAuth/Storage/InMemoryUserStore.cs` - завантажує demo-користувачів із конфігурації.
- `src/SecureAuth/Storage/InMemoryTokenStore.cs` - thread-safe сховище simple/full токенів.
- `src/SecureAuth/Background/ExpiredTokenCleanupService.cs` - періодично видаляє протерміновані токени.

### Конфігурація і тести

- `src/SecureAuth/appsettings.json` - demo `StaticKey`, TTL і seed-користувач.
- `src/SecureAuth/SecureAuth.http` - ручні HTTP-приклади.
- `test/SecureAuth.Tests/*` - unit та integration tests для ключових сценаріїв.

## Чому підпис винесений у filter

Підпис і freshness тут працюють як захист входу в API, а не як частина login-логіки. Тому `AuthController` і `AuthService` не дублюють перевірку в кожному методі: filter запускається перед action-методом і блокує невалідні запити ще до бізнес-логіки.

## Чому `simpleToken` одноразовий

`simpleToken` існує тільки для короткого проміжного кроку. `InMemoryTokenStore.TryConsumeSimpleToken(...)` не просто читає його, а видаляє атомарно. Через це повторний обмін того самого `simpleToken` неможливий навіть при паралельних запитах.
