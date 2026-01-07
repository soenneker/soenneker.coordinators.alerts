using System;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Soenneker.Coordinators.Alerts.Abstract;
using Soenneker.Coordinators.Base;
using Soenneker.Extensions.Configuration;
using Soenneker.Extensions.DateTimeOffsets;
using Soenneker.Extensions.String;
using Soenneker.Extensions.ValueTask;
using Soenneker.MsTeams.Util.Abstract;
using Soenneker.Requests.Azure.Alerts;
using Soenneker.Utils.Json;
using Soenneker.Utils.TimeZones;

namespace Soenneker.Coordinators.Alerts;

///<inheritdoc cref="IAlertsCoordinator"/>
public sealed class AlertsCoordinator : BaseCoordinator, IAlertsCoordinator
{
    private static readonly AdaptiveSchemaVersion _schema12 = new(1, 2);
    private const string _azureAlertsUrl = "https://portal.azure.com/#blade/Microsoft_Azure_Monitoring/AlertsManagementSummaryBlade";

    private readonly IMsTeamsUtil _msTeamsUtil;

    // Cache config reads (no per-call configuration access)
    private readonly string _azureApiKey;
    private readonly string _environment;

    public AlertsCoordinator(IConfiguration configuration, ILogger<AlertsCoordinator> logger, IMsTeamsUtil msTeamsUtil) : base(configuration, logger)
    {
        _msTeamsUtil = msTeamsUtil;

        _azureApiKey = Config.GetValueStrict<string>("Api:Alerts:AzureApiKey");
        _environment = Config.GetValueStrict<string>("Environment");
    }

    public async ValueTask<bool?> CreateAzure(string apiKey, CasRequest request, CancellationToken cancellationToken)
    {
        if (!string.Equals(_azureApiKey, apiKey, StringComparison.Ordinal))
            throw new Exception($"{nameof(apiKey)} does not validate");

        CasData? data = request.Data;
        CasEssentials? essentials = data?.Essentials;

        if (essentials == null)
        {
            Logger.LogError("Error did not have Essentials");
            return false;
        }

        if (Logger.IsEnabled(LogLevel.Debug))
        {
            string? json = JsonUtil.Serialize(request);
            Logger.LogDebug("Error json: {json}", json);
        }

        var card = new AdaptiveCards.AdaptiveCard(_schema12);
        var container = new AdaptiveContainer();

        string? monitorCondition = essentials.MonitorCondition;

        var titleBlock = new AdaptiveTextBlock
        {
            Text = monitorCondition,
            Size = AdaptiveTextSize.Medium,
            Weight = AdaptiveTextWeight.Bolder,
            Wrap = true
        };

        // Avoid lowercasing/allocations; do case-insensitive comparisons
        if (monitorCondition != null)
        {
            if (string.Equals(monitorCondition, "resolved", StringComparison.OrdinalIgnoreCase))
                titleBlock.Color = AdaptiveTextColor.Good;
            else if (string.Equals(monitorCondition, "fired", StringComparison.OrdinalIgnoreCase))
                titleBlock.Color = AdaptiveTextColor.Attention;
        }

        container.Items.Add(titleBlock);

        container.Items.Add(new AdaptiveTextBlock
        {
            Text = $"Alert for rule {essentials.AlertRule}",
            Size = AdaptiveTextSize.Medium,
            Wrap = true
        });

        // Build facts without Dictionary/LINQ
        AdaptiveFactSet? factSet = null;

        CasCondition? condition = data!.AlertContext?.Condition;
        var allOf = condition?.AllOf;

        if (allOf != null && allOf.Count != 0)
        {
            CasAllOf firstCondition = allOf[0];

            if (!firstCondition.MetricName.IsNullOrEmpty())
            {
                factSet ??= new AdaptiveFactSet { Facts = [] };
                factSet.Facts.Add(new AdaptiveFact("Name:", firstCondition.MetricName));
            }

            // Avoid adding empty/meaningless values
            string metricValue = firstCondition.MetricValue.ToString();
            if (!metricValue.IsNullOrEmpty())
            {
                factSet ??= new AdaptiveFactSet { Facts = [] };
                factSet.Facts.Add(new AdaptiveFact("Value:", metricValue));
            }
        }

        string? severity = essentials.Severity;
        if (!severity.IsNullOrEmpty())
        {
            factSet ??= new AdaptiveFactSet { Facts = [] };
            factSet.Facts.Add(new AdaptiveFact("Severity:", severity));
        }

        if (factSet != null && factSet.Facts.Count != 0)
            container.Items.Add(factSet);

        container.Items.Add(new AdaptiveTextBlock
        {
            Text = _environment,
            Size = AdaptiveTextSize.Small,
            IsSubtle = true,
            Spacing = AdaptiveSpacing.Small
        });

        string? firedDateTime = essentials.FiredDateTime;
        if (!firedDateTime.IsNullOrEmpty())
        {
            DateTimeOffset? parsed = firedDateTime.ToDateTimeOffset();
            if (parsed != null)
            {
                container.Items.Add(new AdaptiveTextBlock
                {
                    Text = parsed.Value.ToTzDateTimeFormat(Tz.Eastern),
                    Size = AdaptiveTextSize.Small,
                    IsSubtle = true,
                    Spacing = AdaptiveSpacing.Small
                });
            }
        }

        card.Actions.Add(new AdaptiveOpenUrlAction
        {
            Title = "View",
            UrlString = _azureAlertsUrl
        });

        card.Body.Add(container);

        await _msTeamsUtil.SendMessageCard(card, "Errors", cancellationToken: cancellationToken)
                          .NoSync();
        return true;
    }
}