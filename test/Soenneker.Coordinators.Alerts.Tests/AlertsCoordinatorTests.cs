using Soenneker.Coordinators.Alerts.Abstract;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.Coordinators.Alerts.Tests;

[ClassDataSource<Host>(Shared = SharedType.PerTestSession)]
public class AlertsCoordinatorTests : HostedUnitTest
{
    private readonly IAlertsCoordinator _util;

    public AlertsCoordinatorTests(Host host) : base(host)
    {
        _util = Resolve<IAlertsCoordinator>(true);
    }

    [Test]
    public void Default()
    {

    }
}
