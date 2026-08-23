using FoaieDeParcurs.Core.Domain;
using FoaieDeParcurs.Core.Entities;

namespace FoaieDeParcurs.Tests.Domain;

public sealed class FoaieDeParcursDocumentBuilderTests
{
    private static VehicleProfile Profile() => new()
    {
        CompanyName = "Acme SRL",
        Cui = "RO12345678",
        DriverName = "Mihai Zivojinovic",
        VehiclePlate = "B-01-ABC",
        VehicleMakeModel = "Dacia Duster",
        VehicleCategory = "M1",
        FuelType = FuelType.Motorina,
        FuelConsumptionNormPer100Km = 6.8
    };

    [Fact]
    public void Build_CopiesVehicleProfileAndFillUpFields()
    {
        var t0 = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);
        var fillUp = new FillUp
        {
            Timestamp = t0,
            StationName = "E70, Bucuresti",
            LitersFilled = 42.5,
            AmountPaid = 320.75m,
            Currency = "RON",
            OdometerReading = 85000
        };

        var document = FoaieDeParcursDocumentBuilder.Build(Profile(), fillUp, previousFillUp: null, segments: []);

        Assert.Equal("Acme SRL", document.CompanyName);
        Assert.Equal("RO12345678", document.Cui);
        Assert.Equal("Mihai Zivojinovic", document.DriverName);
        Assert.Equal("B-01-ABC", document.VehiclePlate);
        Assert.Equal("Dacia Duster", document.VehicleMakeModel);
        Assert.Equal("M1", document.VehicleCategory);
        Assert.Equal("Motorina", document.FuelType);
        Assert.Equal(6.8, document.FuelConsumptionNormPer100Km);
        Assert.Equal(42.5, document.LitersFilled);
        Assert.Equal(320.75m, document.AmountPaid);
        Assert.Equal("RON", document.Currency);
        Assert.Equal("E70, Bucuresti", document.StationName);
        Assert.Equal(85000, document.OdometerReading);
        Assert.Equal(t0, document.FillUpDate);
        Assert.Equal(t0, document.PeriodEnd);
    }

    [Fact]
    public void Build_MapsEverySegmentToARow_InChronologicalOrder()
    {
        var t0 = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);
        var fillUp = new FillUp { Timestamp = t0, LitersFilled = 40, AmountPaid = 300 };

        var segments = new List<RouteSegment>
        {
            new()
            {
                StartLocationName = "Brasov", EndLocationName = "Cluj-Napoca",
                StartTimestamp = t0.AddHours(-1), EndTimestamp = t0, DistanceKm = 20, Purpose = "Deplasare de serviciu"
            },
            new()
            {
                StartLocationName = "Depot X", EndLocationName = "Brasov",
                StartTimestamp = t0.AddHours(-2), EndTimestamp = t0.AddHours(-1), DistanceKm = 30, Purpose = "Deplasare de serviciu"
            }
        };

        var document = FoaieDeParcursDocumentBuilder.Build(Profile(), fillUp, previousFillUp: null, segments);

        Assert.Equal(2, document.Segments.Count);
        Assert.Equal("Depot X", document.Segments[0].From);
        Assert.Equal("Brasov", document.Segments[0].To);
        Assert.Equal("Brasov", document.Segments[1].From);
        Assert.Equal("Cluj-Napoca", document.Segments[1].To);
        Assert.Equal(50, document.TotalDistanceKm);
    }

    [Fact]
    public void Build_UsesThePreviousFillUpsTimestamp_AsThePeriodStart()
    {
        var previousTimestamp = new DateTimeOffset(2026, 5, 20, 9, 0, 0, TimeSpan.Zero);
        var previousFillUp = new FillUp { Timestamp = previousTimestamp, LitersFilled = 30, AmountPaid = 200 };
        var fillUp = new FillUp { Timestamp = previousTimestamp.AddDays(5), LitersFilled = 40, AmountPaid = 300 };

        var document = FoaieDeParcursDocumentBuilder.Build(Profile(), fillUp, previousFillUp, segments: []);

        Assert.Equal(previousTimestamp, document.PeriodStart);
    }

    [Fact]
    public void Build_UsesTheFillUpsOwnTimestamp_AsThePeriodStart_WhenThereIsNoPreviousFillUp()
    {
        var t0 = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);
        var fillUp = new FillUp { Timestamp = t0, LitersFilled = 40, AmountPaid = 300 };

        var document = FoaieDeParcursDocumentBuilder.Build(Profile(), fillUp, previousFillUp: null, segments: []);

        Assert.Equal(t0, document.PeriodStart);
    }
}
