# Qubix

Qubix — учебное веб-приложение для создания и проведения квизов в реальном
времени. Организатор настраивает квиз, создаёт комнату и управляет показом
вопросов, а участники подключаются по шестизначному коду и отвечают со своих
устройств.

## Возможности

### Для организатора

- регистрация и авторизация с ролью `Organizer`;
- создание, настройка, публикация и архивирование квизов;
- возврат архивного квиза в черновик;
- добавление текстовых вопросов и вопросов с изображением по `ImageUrl`;
- вопросы с одиночным (`SingleChoice`) и множественным (`MultipleChoice`)
  выбором;
- настройка времени и количества баллов для каждого вопроса;
- создание комнаты и управление сессией;
- просмотр участников, промежуточного и итогового лидерборда;
- история проведённых игр.

### Для участника

- регистрация и авторизация с ролью `Participant`;
- подключение к активной комнате по шестизначному коду;
- получение вопросов в реальном времени;
- отправка одного ответа только во время показа вопроса;
- отображение таймера и блокировка формы после ответа;
- просмотр лидерборда и истории участия.

## Технологии

### Backend

- C# и ASP.NET Core Web API;
- ASP.NET Core Identity и cookie-аутентификация;
- Entity Framework Core;
- PostgreSQL 16;
- SignalR для обмена событиями в реальном времени;
- Problem Details для единого формата ошибок;
- xUnit и интеграционные тесты.

### Frontend

- React;
- TypeScript;
- Vite;
- React Router;
- `@microsoft/signalr`;
- HTML и CSS без отдельной UI-библиотеки.

### Инфраструктура

- Docker и Docker Compose;
- Nginx для раздачи frontend и проксирования `/api` и `/hubs`;
- автоматическое применение миграций при запуске backend-контейнера;
- постоянные Docker volumes для PostgreSQL и ключей ASP.NET Core Data
  Protection.

## Структура проекта

```text
Qubix/
├── backend/
│   ├── Qubix.Api/               # REST API, SignalR Hub и конфигурация
│   ├── Qubix.Core/              # доменные сущности, enum и правила
│   ├── Qubix.Infrastructure/    # EF Core, PostgreSQL и Identity
│   ├── Qubix.UnitTests/         # модульные тесты
│   └── Qubix.IntegrationTests/  # интеграционные тесты API
├── frontend/                    # React + TypeScript + Vite
├── docker-compose.yml
└── Qubix.sln
```

## Быстрый запуск через Docker

Для запуска достаточно установленных
[Docker Desktop](https://www.docker.com/products/docker-desktop/) и Docker
Compose.

### 1. Настроить переменные окружения

В корне проекта создайте `.env` на основе примера:

```powershell
Copy-Item .env.example .env
```

Откройте `.env` и задайте пароль PostgreSQL:

```dotenv
POSTGRES_DB=qubix
POSTGRES_USER=qubix
POSTGRES_PASSWORD=укажите_свой_пароль
POSTGRES_PORT=5432
APP_PORT=8080
```

Файл `.env` добавлен в `.gitignore`. Не публикуйте его и не сохраняйте в нём
рабочие пароли для общедоступного репозитория.

### 2. Собрать и запустить приложение

```powershell
docker compose up --build -d
```

Docker Compose запустит:

- `postgres` — PostgreSQL 16 Alpine;
- `backend` — ASP.NET Core API и SignalR;
- `frontend` — собранное React-приложение под управлением Nginx.

После запуска приложение доступно по адресу:

**http://localhost:8080**

Миграции базы данных и роли `Participant` и `Organizer` создаются
автоматически при запуске backend.

### 3. Проверить контейнеры

```powershell
docker compose ps
```

У PostgreSQL должен отображаться статус `healthy`, а backend и frontend должны
быть запущены.

Для просмотра журналов:

```powershell
docker compose logs -f backend frontend
```

### 4. Остановить приложение

```powershell
docker compose down
```

Данные PostgreSQL сохраняются в Docker volume и будут доступны после
следующего запуска.

## Локальная сборка и тестирование

Требования для запуска без полной контейнеризации:

- .NET SDK 10;
- Node.js 24 или новее;
- npm 11 или новее;
- PostgreSQL.

Перед запуском backend задайте строку подключения к своей базе данных:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=qubix;Username=qubix;Password=YOUR_PASSWORD"
```

Собрать backend и запустить все тесты:

```powershell
dotnet build Qubix.sln
dotnet test Qubix.sln --no-build
```

Собрать frontend:

```powershell
Set-Location frontend
npm ci
npm run lint
npm run build
```

Для разработки backend запускается отдельно:

```powershell
dotnet run --project backend/Qubix.Api --launch-profile https
```

В режиме Development Swagger UI будет доступен по адресу
**https://localhost:7251/swagger**.

Frontend запускается во втором терминале:

```powershell
Set-Location frontend
npm run dev
```

Vite проксирует REST-запросы `/api` и SignalR-подключения `/hubs` на локальный
backend.

## Основной сценарий работы

1. Организатор регистрируется с ролью `Organizer`.
2. Создаёт квиз, добавляет вопросы и правильные варианты ответа.
3. Публикует квиз и создаёт комнату.
4. Участники регистрируются с ролью `Participant` и вводят код комнаты.
5. Организатор запускает сессию и по очереди открывает вопросы.
6. Участники отвечают до окончания времени показа.
7. После закрытия вопросов обновляется лидерборд.
8. После завершения результаты сохраняются в истории.

## Макеты и материалы

- [Макеты Qubix в Figma](https://www.figma.com/design/Iv7JRvMpx03Dqfj7m0202B/Qubix-%E2%80%94-%D0%BC%D0%B8%D0%BD%D0%B8%D0%BC%D0%B0%D0%BB%D1%8C%D0%BD%D1%8B%D0%B5-%D0%BC%D0%B0%D0%BA%D0%B5%D1%82%D1%8B-MVP?node-id=0-1&t=xiD7ezi9owc0egHr-1)
- [Пользовательский сценарий в Miro](https://miro.com/app/board/uXjVH4eJR-U=/?share_link_id=758051321190)
- [Репозиторий Qubix на GitHub](https://github.com/Fountainebleu/Qubix)

## Статус проекта

Реализован и протестирован MVP: создание квиза, проведение игры в реальном
времени, подсчёт баллов, лидерборд и сохранение истории. Проект запускается
локально одной командой Docker Compose.
