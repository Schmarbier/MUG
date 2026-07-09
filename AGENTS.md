# AGENTS.md — PersonalFinance

## Propósito
Visor de finanzas personales mono-usuario: un bot lee mensajes de Telegram y un agente los
clasifica como movimientos (ingreso/egreso) por categoría, con resumen mensual.

## Stack
- .NET 10 — dos procesos: `PersonalFinance.Bot` (ingesta Telegram + agente clasificador) y `PersonalFinance.Web` (API/web del resumen).
- EF Core 10 + SQLite (persistencia).
- Telegram.Bot (canal de mensajes) · OllamaSharp (cliente del modelo).
- Ollama corre aparte: modelo `llama3.1` local, configurable vía env `OLLAMA_MODEL`.

## Configuración / secretos
El token del bot de Telegram **no se commitea**. Se lee de `IConfiguration` con la clave
`TelegramBotToken`, así que puede venir de User Secrets (dev local) o de variable de entorno.
No usar `.env`.

Dev local (una sola vez, desde el proyecto del bot):
```
dotnet user-secrets set "TelegramBotToken" "TU_TOKEN"
```
El valor queda fuera del repo (en `~/.microsoft/usersecrets/`). En `appsettings.json` la clave
queda vacía, solo como documentación de que existe. Alternativa: exportar `TelegramBotToken`
como variable de entorno. Ídem `OLLAMA_MODEL`.

## Cómo correr
Prerequisito — Ollama levantado con el modelo:
```
ollama serve
ollama pull llama3.1
```
Instalar / restaurar:
```
dotnet restore
```
Levantar (una terminal por proceso):
```
dotnet run --project src/PersonalFinance.Bot   # bot + clasificador
dotnet run --project src/PersonalFinance.Web   # API / resumen mensual
```
Tests:
```
dotnet test
```

## Qué NO hacer
- El bot NO responde al usuario por Telegram. Solo lee; el "Mensajes guardados" (AC-11) es
  log interno/consola, no un reply. (Fuera de alcance)
- NO agregar sistema de usuarios ni login: la app es mono-usuario. (Fuera de alcance)
- NO re-guardar mensajes ya ingeridos: deduplicar por `message_id` de Telegram y marcar
  `procesado`; el canal es Telegram, no WhatsApp. (Riesgo / Dependencias)
