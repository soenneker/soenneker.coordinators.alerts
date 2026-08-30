using Soenneker.Coordinators.Base.Abstract;
using Soenneker.Requests.Azure.Alerts;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Coordinators.Alerts.Abstract;

/// <summary>
/// Validates and forwards Azure Monitor common alert schema payloads to Microsoft Teams.
/// </summary>
public interface IAlertsCoordinator : IBaseCoordinator
{
    /// <summary>
    /// Validates an Azure alert callback and forwards a formatted Adaptive Card to the Errors channel.
    /// </summary>
    /// <param name="apiKey">API key used to authenticate the request.</param>
    /// <param name="request">request that defines the request to send.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns><c>true</c> when the alert is accepted; <c>false</c> when required alert essentials are missing.</returns>
    /// <exception cref="System.UnauthorizedAccessException">The supplied API key does not match the configured key.</exception>
    ValueTask<bool?> CreateAzure(string apiKey, CasRequest request, CancellationToken cancellationToken);
}
