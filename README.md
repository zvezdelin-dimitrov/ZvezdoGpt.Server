# ZvezdoGpt.WebApi

.NET 9 web API that provides OpenAI-compatible chat completion and embedding endpoints, with user authentication, Cosmos DB-based caching, and model management. It is designed for secure, scalable, and efficient AI chat applications.

## Features

- **OpenAI-Compatible Endpoints**: Supports `/v1/chat/completions` and `/v1/models` for easy integration.
- **Azure AD Authentication**: Uses JWT Bearer tokens and Microsoft Identity for secure user authentication.
- **API Key Management**: Users can save and retrieve their own API keys.
- **Model Management**: Users can set and get their preferred AI model.
- **Cosmos DB Integration**: Caches question/answer pairs and user data for performance and persistence.
- **Streaming Responses**: Chat completions are streamed using Server-Sent Events (SSE).
- **Docker Support**: Ready-to-use Dockerfile for containerized deployment.

## Endpoints

| Endpoint                                 | Method | Auth Required | Description                                 |
|-------------------------------------------|--------|---------------|---------------------------------------------|
| `/v1/chat/completions`                    | POST   | No            | OpenAI-compatible chat completions (API key in Bearer token) |
| `/v9/chat/completions`                    | POST   | No            | Chat completions (API key in `X-API-KEY` header) |
| `/v1/models`, `/v9/models`                | GET    | No            | List supported models                       |
| `/v9/user/apikey`                         | POST   | Yes           | Save user API key                           |
| `/v9/user/preferred-model`                | POST   | Yes           | Save preferred model                        |
| `/v9/user/preferred-model`                | GET    | Yes           | Get preferred model                         |

## Authentication

- **Azure AD**: Most user endpoints require JWT Bearer authentication.
- **API Key**: Chat endpoints accept either a Bearer token (OpenAI-compatible) or an `X-API-KEY` header.

## Configuration

- **Cosmos DB**: Set connection details in `appsettings.json` or environment variables.
- **Embedding Model**: Specify the embedding model name in configuration.
- **Vector Distance Threshold**: Configure similarity threshold for answer caching.

## Running with Docker

```sh
docker build -t zvezdogpt-webapi .
docker run -p 8081:8081 zvezdogpt-webapi
```
