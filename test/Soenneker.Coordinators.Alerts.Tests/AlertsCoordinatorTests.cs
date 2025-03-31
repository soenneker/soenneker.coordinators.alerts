using Soenneker.Coordinators.Alerts.Abstract;
using Soenneker.Tests.FixturedUnit;
using Xunit;

namespace Soenneker.Coordinators.Alerts.Tests;

[Collection("Collection")]
public class AlertsCoordinatorTests : FixturedUnitTest
{
    private readonly IAlertsCoordinator _util;

    public AlertsCoordinatorTests(Fixture fixture, ITestOutputHelper output) : base(fixture, output)
    {
        _util = Resolve<IAlertsCoordinator>(true);
    }

    [Fact]
    public void Default()
    {

    }
}
