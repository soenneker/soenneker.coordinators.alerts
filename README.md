[![](https://img.shields.io/nuget/v/soenneker.coordinators.alerts.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.coordinators.alerts/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.coordinators.alerts/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.coordinators.alerts/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.coordinators.alerts.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.coordinators.alerts/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.coordinators.alerts/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.coordinators.alerts/actions/workflows/codeql.yml)

# Soenneker.Coordinators.Alerts

Validates Azure Monitor common alert schema callbacks, builds an Adaptive Card, and sends it to the Microsoft Teams `Errors` channel.

## Install

```bash
dotnet add package Soenneker.Coordinators.Alerts
```

## Configuration

```json
{
  "Api": {
    "Alerts": {
      "AzureApiKey": "replace-with-a-random-secret"
    }
  },
  "Environment": "Production",
  "MsTeams": {
    "UseQueue": false,
    "Enabled": true,
    "Errors": {
      "Enabled": true,
      "WebhookUrl": "https://..."
    }
  }
}
```

When `MsTeams:UseQueue` is `true`, Microsoft Teams delivery is placed on the configured service bus instead of sent to `MsTeams:Errors:WebhookUrl` immediately.

## Registration

```csharp
using Soenneker.Coordinators.Alerts.Registrars;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddAlertsCoordinatorAsSingleton();
```

Use `AddAlertsCoordinatorAsScoped()` when the coordinator and its Microsoft Teams dependencies should follow a dependency-injection scope.

## Usage

```csharp
using Soenneker.Coordinators.Alerts.Abstract;
using Soenneker.Requests.Azure.Alerts;

public sealed class AzureAlertHandler(IAlertsCoordinator alerts)
{
    public ValueTask<bool?> Handle(string apiKey, CasRequest request, CancellationToken cancellationToken)
    {
        return alerts.CreateAzure(apiKey, request, cancellationToken);
    }
}
```

`CreateAzure` compares the supplied key with `Api:Alerts:AzureApiKey`, then reads `Data.Essentials` from the common alert schema request. A valid alert is formatted with its monitor condition, rule, first metric condition, severity, configured environment, and fired time converted to US Eastern time.

The result is `true` after the Teams utility accepts the card and `false` when alert essentials are missing. An invalid API key throws `UnauthorizedAccessException`. Downstream Teams or service-bus failures propagate to the caller.

## Security and operation

- API keys are compared through fixed-size hashes using a fixed-time comparison. Store the configured key in a secret provider rather than source-controlled JSON.
- Incoming alert payloads are not serialized into logs by this coordinator. Alert fields may still appear in the Teams card and should be treated according to their data sensitivity.
- The card links to Azure's Alerts Management page rather than a resource-specific alert URL.
- With queued delivery, successful completion means the message was accepted by the queue, not delivered to Teams.
