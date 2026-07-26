# OpenAPI Response Contracts

Implemented explicit response metadata across the ASP.NET Core controllers, added OpenAPI-only response model types plus scoped schema/operation transformers for safe examples and shared error descriptions, and expanded the integration spec test to assert representative response contracts and that no generated operation has an empty response map.

Runtime API behavior was left unchanged. Domain/exception errors still document `MessageResponse`, framework validation and bare status-code responses still document RFC 7807 `ValidationProblemDetails`/`ProblemDetails`, docs exposure/auth behavior is unchanged, and no Swashbuckle dependency was introduced.

Focused backend build/tests were not run in this environment because `dotnet` is unavailable here.
