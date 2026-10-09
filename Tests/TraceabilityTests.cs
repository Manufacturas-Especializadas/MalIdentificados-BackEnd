using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests;

public sealed class TraceabilityTests
{
    [Fact]
    public async Task Line_filter_is_applied_before_limit_and_does_not_infer_historical_lines()
    {
        using var api = new TestApi();
        await api.AddPart("ABC123", 4);
        int historicalId = 0;
        await api.WithDb(async db =>
        {
            var partId = await db.PartNumbers.Select(p => p.Id).SingleAsync();
            db.ContainerValidations.Add(new ContainerValidation
            {
                LineId = 4, IdPartNumber = partId, ExpectedPartCode = "ABC123", ContainerNumber = "ABC123",
                ScanDetails = new List<ScanDetail> { new() { ScannedPartCode = "ABC123", IsCorrect = true } }
            });
            await db.SaveChangesAsync();
            db.ContainerValidations.AddRange(Enumerable.Range(0, 55).Select(_ => new ContainerValidation { LineId = 8 }));
            var historical = new ContainerValidation { IdPartNumber = partId, LineId = null };
            db.ContainerValidations.Add(historical);
            await db.SaveChangesAsync();
            historicalId = historical.Id;
        });

        var filtered = await api.Client.GetFromJsonAsync<JsonElement[]>("/api/Traceability/validations?lineId=4");
        var batch = Assert.Single(filtered!);
        Assert.Equal(4, batch.GetProperty("lineId").GetInt32());
        Assert.Equal("Línea 4 Empaques", batch.GetProperty("lineName").GetString());
        Assert.Equal("ABC123", batch.GetProperty("containerNumber").GetString());
        Assert.Single(batch.GetProperty("scanDetails").EnumerateArray());

        var global = await api.Client.GetFromJsonAsync<JsonElement[]>("/api/Traceability/validations");
        Assert.Equal(50, global!.Length);
        Assert.Equal(historicalId, global[0].GetProperty("id").GetInt32());
        Assert.Equal(JsonValueKind.Null, global[0].GetProperty("lineId").ValueKind);
        Assert.Equal(JsonValueKind.Null, global[0].GetProperty("lineName").ValueKind);
        var ids = global.Select(v => v.GetProperty("id").GetInt32()).ToList();
        Assert.Equal(ids.OrderByDescending(id => id), ids);
        await api.WithDb(async db => Assert.Null((await db.ContainerValidations.FindAsync(historicalId))!.LineId));
    }

    [Fact]
    public async Task Inactive_lines_remain_available_in_history()
    {
        using var api = new TestApi();
        await api.WithDb(async db =>
        {
            db.ContainerValidations.Add(new ContainerValidation { LineId = 9 });
            await db.SaveChangesAsync();
        });
        Assert.Single((await api.Client.GetFromJsonAsync<JsonElement[]>("/api/Traceability/validations?lineId=9"))!);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    public async Task Invalid_line_filter_returns_bad_request(string lineId)
    {
        using var api = new TestApi();
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Client.GetAsync($"/api/Traceability/validations?lineId={lineId}")).StatusCode);
    }

    [Fact]
    public async Task Unknown_line_filter_returns_an_empty_history()
    {
        using var api = new TestApi();
        Assert.Empty((await api.Client.GetFromJsonAsync<JsonElement[]>("/api/Traceability/validations?lineId=999"))!);
    }

    [Fact]
    public async Task Line_foreign_key_is_nullable_restricts_deletion_and_has_no_new_index()
    {
        using var api = new TestApi();
        await api.WithDb(async db =>
        {
            var entity = db.Model.FindEntityType(typeof(ContainerValidation))!;
            Assert.True(entity.FindProperty(nameof(ContainerValidation.LineId))!.IsNullable);
            var foreignKey = Assert.Single(entity.GetForeignKeys(), fk => fk.Properties.Any(p => p.Name == nameof(ContainerValidation.LineId)));
            Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
            Assert.DoesNotContain(entity.GetIndexes(), i => i.Properties.Any(p => p.Name == nameof(ContainerValidation.LineId)));
            db.ContainerValidations.Add(new ContainerValidation { LineId = 4 });
            await db.SaveChangesAsync();
        });
        await api.WithDb(async db =>
        {
            db.Lines.Remove((await db.Lines.FindAsync(4))!);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        });
    }
}
