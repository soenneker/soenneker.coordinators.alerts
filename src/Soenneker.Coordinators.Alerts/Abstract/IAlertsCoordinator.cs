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
    /// <param name="apiKey">The API key.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task containing the result of the operation.</returns>
    ValueTask<bool?> CreateAzure(string apiKey, CasRequest request, CancellationToken cancellationToken);
}
