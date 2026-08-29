[![](https://img.shields.io/nuget/v/soenneker.coordinators.alerts.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.coordinators.alerts/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.coordinators.alerts/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.coordinators.alerts/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.coordinators.alerts.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.coordinators.alerts/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.coordinators.alerts/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.coordinators.alerts/actions/workflows/codeql.yml)

# Soenneker.Coordinators.Alerts

Handling Azure alerts from the controller.

## Install

```bash
dotnet add package Soenneker.Coordinators.Alerts
```

## Quick start

```csharp
using Soenneker.Coordinators.Alerts.Registrars;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
var result = services.AddAlertsCoordinatorAsSingleton();
```

Adds `IAlertsCoordinator` as a singleton service.

## What you get

- `IAlertsCoordinator` — Handling Azure alerts from the controller.
- `AlertsCoordinatorRegistrar` — Handling Azure alerts from the controller.

## API at a glance

| API | What it does | Result / important behavior |
| --- | --- | --- |
| `AlertsCoordinatorRegistrar.AddAlertsCoordinatorAsSingleton(services)` | Adds `IAlertsCoordinator` as a singleton service. | The same service collection, so additional registrations can be chained. |
| `AlertsCoordinatorRegistrar.AddAlertsCoordinatorAsScoped(services)` | Adds `IAlertsCoordinator` as a scoped service. | The same service collection, so additional registrations can be chained. |
