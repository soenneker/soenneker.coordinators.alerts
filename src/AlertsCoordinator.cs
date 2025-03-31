using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Soenneker.Coordinators.Alerts.Abstract;
using Soenneker.Coordinators.Base;
using Soenneker.Extensions.Configuration;
using Soenneker.Extensions.DateTime;
using Soenneker.Extensions.String;
using Soenneker.MsTeams.Util.Abstract;
using Soenneker.Requests.Azure.Alerts;
using Soenneker.Utils.Json;
using Soenneker.Utils.TimeZones;

namespace Soenneker.Coordinators.Alerts;

///<inheritdoc cref="IAlertsCoordinator"/>
public class AlertsCoordinator : BaseCoordinator, IAlertsCoordinator
{
    private readonly IMsTeamsUtil _msTeamsUtil;

    public AlertsCoordinator(IConfiguration configuration, ILogger<AlertsCoordinator> logger, IMsTeamsUtil msTeamsUtil) : base(configuration, logger)
    {
        _msTeamsUtil = msTeamsUtil;
    }

    public async ValueTask<bool?> CreateAzure(string apiKey, CasRequest request, CancellationToken cancellationToken)
    {
        if (Config.GetValueStrict<string>("Api:Alerts:AzureApiKey") != apiKey)
            throw new Exception($"{nameof(apiKey)} does not validate");

        if (request.Data?.Essentials == null)
        {
            Logger.LogError("Error did not have Essentials");
            return false;
        }

        string? json = JsonUtil.Serialize(request);
        Logger.LogDebug("Error json: {json}", json);

        AdaptiveCards.AdaptiveCard card = new(new AdaptiveSchemaVersion(1, 2));

        var container = new AdaptiveContainer();

        var titleBlock = new AdaptiveTextBlock
        {
            Text = request.Data.Essentials.MonitorCondition,
            Size = AdaptiveTextSize.Medium,
            Weight = AdaptiveTextWeight.Bolder,
            Wrap = true
        };

        switch (request.Data.Essentials.MonitorCondition!.ToLowerInvariantFast())
        {
            case "resolved":
                titleBlock.Color = AdaptiveTextColor.Good;
                break;
            case "fired":
                titleBlock.Color = AdaptiveTextColor.Attention;
                break;
        }

        container.Items.Add(titleBlock);

        container.Items.Add(new AdaptiveTextBlock
        {
            Text = $"Alert for rule {request.Data.Essentials.AlertRule}",
            Size = AdaptiveTextSize.Medium,
            Wrap = true
        });

        var values = new Dictionary<string, string?>();

        CasCondition? condition = request.Data.AlertContext?.Condition;

        if (condition?.AllOf != null && condition.AllOf.Count != 0)
        {
            CasAllOf firstCondition = condition.AllOf.First();

            values.Add("Name:", firstCondition.MetricName);
            values.Add("Value:", firstCondition.MetricValue.ToString());
        }

        values.Add("Severity:", request.Data.Essentials.Severity);

        if (values.Any())
        {
            var factSet = new AdaptiveFactSet {Facts = []};

            foreach ((string key, string? value) in values)
            {
                if (value.IsNullOrEmpty())
                    continue;

                var fact = new AdaptiveFact(key, value);

                factSet.Facts.Add(fact);
            }

            container.Items.Add(factSet);
        }

        container.Items.Add(new AdaptiveTextBlock
        {
            Text = Config.GetValueStrict<string>("Environment"),
            Size = AdaptiveTextSize.Small,
            IsSubtle = true,
            Spacing = AdaptiveSpacing.Small
        });

        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
        if (request.Data.Essentials.FiredDateTime != null)
        {
            DateTime? parsed = request.Data.Essentials.FiredDateTime.ToUtcDateTime();

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

        var action = new AdaptiveOpenUrlAction
        {
            Title = "View",
            UrlString = "https://portal.azure.com/#blade/Microsoft_Azure_Monitoring/AlertsManagementSummaryBlade"
        };

        card.Actions.Add(action);

        card.Body.Add(container);

        await _msTeamsUtil.SendMessageCard(card, "Errors", cancellationToken: cancellationToken);

        return true;
    }
}