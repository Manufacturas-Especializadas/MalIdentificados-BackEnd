using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests;

public sealed class ScanningContractTests
{
    private static Dictionary<string, object?> Request(string? mode = "container", int? lineId = 4) => new()
    {
        ["payrollNumber"] = 12345,
        ["expectedPartCode"] = "ABC123",
        ["requiredQuantity"] = 20,
        ["shopOrder"] = null,
        ["scans"] = new[]
        {
            new { scannedPartCode = "ABC123", isCorrect = true, scanDate = "2026-10-09T12:00:00Z", releasedByPayroll = (int?)null }
        },
        ["lineId"] = lineId,
        ["containerNumber"] = "ABC123",
        ["validationMode"] = mode
    };

    private static async Task AssertRejected(TestApi api, Dictionary<string, object?> request, string field)
    {
        var response = await api.Client.PostAsJsonAsync("/api/Scanning/start", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty(field, out _), body.ToString());
        await api.WithDb(async db =>
        {
            Assert.Empty(await db.ContainerValidations.ToListAsync());
            Assert.Empty(await db.ScanDetails.ToListAsync());
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Microchannel_preserves_response_and_scan_semantics_with_or_without_new_fields(bool explicitMode)
    {
        using var api = new TestApi();
        await api.AddPart("ABC123", 8);
        string json = """
            {
              "payrollNumber": 12345,
              "expectedPartCode": "ABC123",
              "requiredQuantity": 20,
              "shopOrder": "SO-001",
              "scans": [
                {"scannedPartCode":"OTHER","isCorrect":true,"scanDate":"2026-10-09T12:00:00Z","releasedByPayroll":456},
                {"scannedPartCode":"ABC123","isCorrect":false,"scanDate":"2026-10-09T12:01:00Z","releasedByPayroll":null}
              ]
            }
            """;
        if (explicitMode)
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, object?>>(json)!;
            payload["lineId"] = 8;
            payload["validationMode"] = "shopOrder";
            json = JsonSerializer.Serialize(payload);
        }
        var response = await api.Client.PostAsync("/api/Scanning/start", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, body.EnumerateObject().Count());
        Assert.Equal("Sesión iniciada correctamente.", body.GetProperty("message").GetString());
        await api.WithDb(async db =>
        {
            var batch = await db.ContainerValidations.Include(v => v.ScanDetails).SingleAsync();
            Assert.Equal(batch.Id, body.GetProperty("validationId").GetInt32());
            Assert.Equal(explicitMode ? (int?)8 : null, batch.LineId);
            Assert.Equal(string.Empty, batch.ContainerNumber);
            Assert.NotNull(batch.IdPartNumber);
            Assert.Equal("SO-001", batch.ShopOrder);
            Assert.Equal(20, batch.RequiredQuantity);
            Assert.Equal(1, batch.ScannedQuantity);
            Assert.Equal("completed", batch.Status);
            Assert.Equal(2, batch.ScanDetails.Count);
            var released = Assert.Single(batch.ScanDetails, s => s.ScannedPartCode == "OTHER");
            Assert.True(released.IsCorrect);
            Assert.Equal(456, released.ReleasedByPayroll);
            Assert.Equal(new DateTime(2026, 10, 9, 12, 0, 0), released.ScanDate);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Legacy_unlisted_or_ambiguous_product_is_saved_without_arbitrary_association(bool ambiguous)
    {
        using var api = new TestApi();
        if (ambiguous)
        {
            await api.AddPart("ABC123", 4);
            await api.AddPart("ABC123", 8);
        }
        var request = Request(null, null);
        request.Remove("containerNumber");
        request["shopOrder"] = "SO-001";
        Assert.Equal(HttpStatusCode.OK, (await api.Client.PostAsJsonAsync("/api/Scanning/start", request)).StatusCode);
        await api.WithDb(async db =>
        {
            var batch = await db.ContainerValidations.SingleAsync();
            Assert.Null(batch.IdPartNumber);
            Assert.Null(batch.LineId);
        });
    }

    [Fact]
    public async Task Container_can_omit_shop_order_and_register_an_unlisted_product()
    {
        using var api = new TestApi();
        var request = Request();
        request.Remove("shopOrder");
        Assert.Equal(HttpStatusCode.OK, (await api.Client.PostAsJsonAsync("/api/Scanning/start", request)).StatusCode);
        await api.WithDb(async db =>
        {
            var batch = await db.ContainerValidations.SingleAsync();
            Assert.Equal(4, batch.LineId);
            Assert.Equal("ABC123", batch.ContainerNumber);
            Assert.Equal("ABC123", batch.ExpectedPartCode);
            Assert.Null(batch.ShopOrder);
            Assert.Null(batch.IdPartNumber);
            Assert.Equal(1, batch.ScannedQuantity);
        });
    }

    [Theory]
    [InlineData("container", 4)]
    [InlineData("shopOrder", 8)]
    public async Task Same_part_code_in_two_lines_resolves_the_requested_line(string mode, int lineId)
    {
        using var api = new TestApi();
        await api.AddPart("ABC123", 4);
        await api.AddPart("ABC123", 8);
        var request = Request(mode, lineId);
        if (mode == "shopOrder")
        {
            request.Remove("containerNumber");
            request["shopOrder"] = "SO-002";
        }
        Assert.Equal(HttpStatusCode.OK, (await api.Client.PostAsJsonAsync("/api/Scanning/start", request)).StatusCode);
        await api.WithDb(async db =>
        {
            var batch = await db.ContainerValidations.Include(v => v.PartNumber).SingleAsync();
            Assert.Equal(lineId, batch.LineId);
            Assert.Equal(lineId, batch.PartNumber!.IdLine);
            Assert.Equal(20, batch.RequiredQuantity);
            Assert.Equal(1, batch.ScannedQuantity);
            Assert.Equal("completed", batch.Status);
        });
    }

    [Theory]
    [InlineData("OTHER")]
    [InlineData("abc123")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Container_requires_a_matching_nonempty_code(string? container)
    {
        using var api = new TestApi();
        var request = Request();
        request["containerNumber"] = container;
        await AssertRejected(api, request, "containerNumber");
    }

    [Fact]
    public async Task Container_trims_only_for_comparison_and_catalog_lookup_preserving_raw_values()
    {
        using var api = new TestApi();
        await api.AddPart("ABC123", 4);
        var request = Request();
        request["expectedPartCode"] = " ABC123 ";
        request["containerNumber"] = "ABC123\r\n";
        Assert.Equal(HttpStatusCode.OK, (await api.Client.PostAsJsonAsync("/api/Scanning/start", request)).StatusCode);
        await api.WithDb(async db =>
        {
            var batch = await db.ContainerValidations.SingleAsync();
            Assert.NotNull(batch.IdPartNumber);
            Assert.Equal(" ABC123 ", batch.ExpectedPartCode);
            Assert.Equal("ABC123\r\n", batch.ContainerNumber);
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(999)]
    [InlineData(9)]
    public async Task New_mode_requires_an_existing_active_line(int? lineId)
    {
        using var api = new TestApi();
        await AssertRejected(api, Request(lineId: lineId), "lineId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("other")]
    [InlineData("Container")]
    public async Task New_fields_require_a_known_explicit_mode(string? mode)
    {
        using var api = new TestApi();
        await AssertRejected(api, Request(mode), "validationMode");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Shop_order_mode_requires_shop_order(string? shopOrder)
    {
        using var api = new TestApi();
        var request = Request("shopOrder", 8);
        request.Remove("containerNumber");
        request["shopOrder"] = shopOrder;
        await AssertRejected(api, request, "shopOrder");
    }

    [Fact]
    public async Task Missing_mode_does_not_bypass_legacy_shop_order_requirement()
    {
        using var api = new TestApi();
        var request = Request(null, null);
        request.Remove("containerNumber");
        request.Remove("shopOrder");
        await AssertRejected(api, request, "shopOrder");
    }

    [Fact]
    public async Task Shop_order_mode_rejects_a_container_instead_of_skipping_its_validation()
    {
        using var api = new TestApi();
        var request = Request("shopOrder", 8);
        request["shopOrder"] = "SO-003";
        await AssertRejected(api, request, "containerNumber");
    }

    [Theory]
    [InlineData(8, true)]
    [InlineData(4, false)]
    public async Task Catalogued_product_requires_an_active_association_to_the_requested_line(int catalogLine, bool active)
    {
        using var api = new TestApi();
        await api.AddPart("ABC123", catalogLine, active);
        await AssertRejected(api, Request(), "expectedPartCode");
    }

    [Fact]
    public async Task Duplicate_active_parts_within_line_are_rejected_without_saving()
    {
        using var api = new TestApi();
        await api.AddPart("ABC123", 4);
        await api.AddPart("ABC123", 4);
        await AssertRejected(api, Request(), "expectedPartCode");
    }

    [Fact]
    public async Task Inactive_duplicate_does_not_make_active_part_ambiguous()
    {
        using var api = new TestApi();
        await api.AddPart("ABC123", 4);
        await api.AddPart("ABC123", 4, false);
        Assert.Equal(HttpStatusCode.OK, (await api.Client.PostAsJsonAsync("/api/Scanning/start", Request())).StatusCode);
    }

    [Fact]
    public async Task Product_code_is_required_for_new_mode()
    {
        using var api = new TestApi();
        var request = Request();
        request["expectedPartCode"] = " ";
        await AssertRejected(api, request, "expectedPartCode");
    }
}
