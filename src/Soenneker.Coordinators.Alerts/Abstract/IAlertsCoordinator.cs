using Soenneker.Coordinators.Base.Abstract;
using Soenneker.Requests.Azure.Alerts;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Coordinators.Alerts.Abstract;

/// <summary>
/// Handling Azure alerts from the controller
/// </summary>
public interface IAlertsCoordinator : IBaseCoordinator
{
    /// <summary>
    /// Creates azure.
    /// </summary>
    /// <param name="apiKey">API key used to authenticate the request.</param>
    /// <param name="request">request that defines the request to send.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>true if creates azure; otherwise, false.</returns>
    ValueTask<bool?> CreateAzure(string apiKey, CasRequest request, CancellationToken cancellationToken);
}
