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
    ValueTask<bool?> CreateAzure(string apiKey, CasRequest request, CancellationToken cancellationToken);
}
