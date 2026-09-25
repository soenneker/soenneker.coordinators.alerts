using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using Soenneker.AdaptiveCards.Dtos.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Soenneker.Coordinators.Alerts.Abstract;
using Soenneker.Coordinators.Base;
using Soenneker.Extensions.Configuration;
using Soenneker.Extensions.String;
using Soenneker.Extensions.ValueTask;
using Soenneker.Hashing.Sha256;
using Soenneker.MsTeams.Util.Abstract;
using Soenneker.Requests.Azure.Alerts;

namespace Soenneker.Coordinators.Alerts;

public sealed class AlertsCoordinator : BaseCoordinator, IAlertsCoordinator
{
    private static readonly Sha256HashingUtil _sha256 = new();

    private const string _azureAlertsUrl = "https://portal.azure.com/#blade/Microsoft_Azure_Monitoring/AlertsManagementSummaryBlade";

    private readonly IMsTeamsUtil _msTeamsUtil;

    // Cache config reads (no per-call configuration access)
    private readonly byte[] _azureApiKeyHash;
    private readonly string _environment;

    public AlertsCoordinator(IConfiguration configuration, ILogger<AlertsCoordinator> logger, IMsTeamsUtil msTeamsUtil) : base(configuration, logger)
    {
        _msTeamsUtil = msTeamsUtil;

        string azureApiKey = Config.GetValueStrict<string>("Api:Alerts:AzureApiKey");
        _azureApiKeyHash = _sha256.Hash(Encoding.UTF8.GetBytes(azureApiKey));
        _environment = Config.GetValueStrict<string>("Environment");
    }

    public async ValueTask<bool?> CreateAzure(string apiKey, CasRequest request, CancellationToken cancellationToken)
    {
        byte[] presentedKeyHash = _sha256.Hash(Encoding.UTF8.GetBytes(apiKey));
        if (!CryptographicOperations.FixedTimeEquals(_azureApiKeyHash, presentedKeyHash))
            throw new UnauthorizedAccessException("Azure alert API key is invalid.");

        CasData? data = request.Data;
        CasEssentials? essentials = data?.Essentials;

        if (essentials == null)
        {
            Logger.LogError("Error did not have Essentials");
            return false;
        }

        var card = new AdaptiveCard
        {
            Type = AdaptiveCardType.AdaptiveCard,
            Version = "1.2",
            Schema = "https://adaptivecards.io/schemas/adaptive-card.json",
            Body = new List<ImplementationsOfElement>(),
            Actions = new List<ImplementationsOfAction>()
        };
        var container = new Container { Type = ContainerType.Container, Items = [] };

        string? monitorCondition = essentials.MonitorCondition;

        var titleBlock = new TextBlock
        {
            Type = TextBlockType.TextBlock,
            Text = monitorCondition ?? string.Empty,
            Size = FontSize.FromVariant1(FontSizeVariant1.Medium),
            Weight = FontWeight.FromVariant1(FontWeightVariant1.Bolder),
            Wrap = true
        };

        // Avoid lowercasing/allocations; do case-insensitive comparisons
        if (monitorCondition != null)
        {
            if (monitorCondition.EqualsIgnoreCase("resolved"))
                titleBlock.Color = Colors.FromVariant1(ColorsVariant1.Good);
            else if (monitorCondition.EqualsIgnoreCase("fired"))
                titleBlock.Color = Colors.FromVariant1(ColorsVariant1.Attention);
        }

        container.Items.Add(ImplementationsOfElement.FromVariant16(titleBlock));

        container.Items.Add(ImplementationsOfElement.FromVariant16(new TextBlock
        {
            Type = TextBlockType.TextBlock,
            Text = $"Alert for rule {essentials.AlertRule}",
            Size = FontSize.FromVariant1(FontSizeVariant1.Medium),
            Wrap = true
        }));

        // Build facts without Dictionary/LINQ
        FactSet? factSet = null;

        CasCondition? condition = data!.AlertContext?.Condition;
        var allOf = condition?.AllOf;

        if (allOf != null && allOf.Count != 0)
        {
            CasAllOf firstCondition = allOf[0];

            if (!firstCondition.MetricName.IsNullOrEmpty())
            {
                factSet ??= new FactSet { Type = FactSetType.FactSet, Facts = [] };
                factSet.Facts.Add(new Fact { Title = "Name:", Value = firstCondition.MetricName });
            }

            // Avoid adding empty/meaningless values
            string? metricValue = firstCondition.MetricValue.ToString();
            if (!metricValue.IsNullOrEmpty())
            {
                factSet ??= new FactSet { Type = FactSetType.FactSet, Facts = [] };
                factSet.Facts.Add(new Fact { Title = "Value:", Value = metricValue });
            }
        }

        string? severity = essentials.Severity;
        if (!severity.IsNullOrEmpty())
        {
            factSet ??= new FactSet { Type = FactSetType.FactSet, Facts = [] };
            factSet.Facts.Add(new Fact { Title = "Severity:", Value = severity });
        }

        if (factSet != null && factSet.Facts.Count != 0)
            container.Items.Add(ImplementationsOfElement.FromVariant4(factSet));

        container.Items.Add(ImplementationsOfElement.FromVariant16(new TextBlock
        {
            Type = TextBlockType.TextBlock,
            Text = _environment,
            Size = FontSize.FromVariant1(FontSizeVariant1.Small),
            IsSubtle = true,
            Spacing = Spacing.FromVariant1(SpacingVariant1.Small)
        }));

        string? firedDateTime = essentials.FiredDateTime;
        if (!firedDateTime.IsNullOrEmpty())
        {
            DateTimeOffset? parsed = firedDateTime.ToDateTimeOffset();
            if (parsed != null)
            {
                container.Items.Add(ImplementationsOfElement.FromVariant16(new TextBlock
                {
                    Type = TextBlockType.TextBlock,
                    Text = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(parsed.Value, "America/New_York").ToString("MM/dd/yyyy h:mm:ss tt", CultureInfo.InvariantCulture) + " ET",
                    Size = FontSize.FromVariant1(FontSizeVariant1.Small),
                    IsSubtle = true,
                    Spacing = Spacing.FromVariant1(SpacingVariant1.Small)
                }));
            }
        }

        card.Actions.Value.Add(ImplementationsOfAction.FromVariant2(new ActionOpenUrl
        {
            Type = ActionOpenUrlType.ActionOpenUrl,
            Title = "View",
            Url = _azureAlertsUrl
        }));

        card.Body.Value.Add(ImplementationsOfElement.FromVariant3(container));

        await _msTeamsUtil.SendMessageCard(card, "Errors", cancellationToken: cancellationToken)
                          .NoSync();
        return true;
    }
}
