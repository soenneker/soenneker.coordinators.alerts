using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Coordinators.Alerts.Abstract;
using Soenneker.MsTeams.Util.Registrars;

namespace Soenneker.Coordinators.Alerts.Registrars;

/// <summary>
/// Handling Azure alerts from the controller
/// </summary>
public static class AlertsCoordinatorRegistrar
{
    /// <summary>
    /// Adds <see cref="IAlertsCoordinator"/> as a singleton service. <para/>
    /// </summary>
    public static IServiceCollection AddAlertsCoordinatorAsSingleton(this IServiceCollection services)
    {
        services.AddMsTeamsUtilAsSingleton().TryAddSingleton<IAlertsCoordinator, AlertsCoordinator>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IAlertsCoordinator"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddAlertsCoordinatorAsScoped(this IServiceCollection services)
    {
        services.AddMsTeamsUtilAsScoped().TryAddScoped<IAlertsCoordinator, AlertsCoordinator>();

        return services;
    }
}