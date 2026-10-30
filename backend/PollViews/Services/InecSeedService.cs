using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PollViews.Data;
using PollViews.Entities;

namespace PollViews.Services;

public sealed class InecSeedService(IWebHostEnvironment environment, PollViewsDbContext db, ILogger<InecSeedService> logger)
{
    private readonly string _dataDirectory = Path.Combine(environment.ContentRootPath, "Data", "Inec");

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var states = Load<SeedState>("states.json");
        var lgas = Load<SeedLga>("lgas.json");
        var wards = Load<SeedWard>("wards.json");

        var statesByCode = await db.States.ToDictionaryAsync(entity => entity.Code, cancellationToken);
        foreach (var item in states)
        {
            if (!statesByCode.TryGetValue(item.Code, out var state))
            {
                state = new State { Code = item.Code };
                db.States.Add(state);
                statesByCode[item.Code] = state;
            }

            state.Name = item.Name;
            state.InecPortalId = item.PortalId;
        }
        await db.SaveChangesAsync(cancellationToken);

        var lgasByKey = await db.LocalGovernmentAreas
            .Include(entity => entity.State)
            .ToDictionaryAsync(entity => $"{entity.State.Code}:{entity.Code}", cancellationToken);
        foreach (var item in lgas)
        {
            if (!statesByCode.TryGetValue(item.StateCode, out var state)) continue;
            var key = $"{item.StateCode}:{item.Code}";
            if (!lgasByKey.TryGetValue(key, out var lga))
            {
                lga = new LocalGovernmentArea { Code = item.Code, StateId = state.Id };
                db.LocalGovernmentAreas.Add(lga);
                lgasByKey[key] = lga;
            }

            lga.Name = item.Name;
            lga.InecPortalId = item.PortalId;
            lga.StateId = state.Id;
        }
        await db.SaveChangesAsync(cancellationToken);

        var wardsByKey = await db.Wards
            .Include(entity => entity.LocalGovernmentArea)
            .ThenInclude(entity => entity.State)
            .ToDictionaryAsync(entity => $"{entity.LocalGovernmentArea.State.Code}:{entity.LocalGovernmentArea.Code}:{entity.Code}", cancellationToken);
        foreach (var item in wards)
        {
            var lgaKey = $"{item.StateCode}:{item.LgaCode}";
            if (!lgasByKey.TryGetValue(lgaKey, out var lga)) continue;
            var key = $"{item.StateCode}:{item.LgaCode}:{item.Code}";
            if (!wardsByKey.TryGetValue(key, out var ward))
            {
                ward = new Ward { Code = item.Code, LocalGovernmentAreaId = lga.Id };
                db.Wards.Add(ward);
                wardsByKey[key] = ward;
            }

            ward.Name = item.Name;
            ward.InecPortalId = item.PortalId;
            ward.LocalGovernmentAreaId = lga.Id;
        }
        await db.SaveChangesAsync(cancellationToken);

        var pollingUnitWardIds = await db.Wards
            .AsNoTracking()
            .Include(entity => entity.LocalGovernmentArea)
            .ThenInclude(entity => entity.State)
            .ToDictionaryAsync(
                entity => $"{entity.LocalGovernmentArea.State.Code}:{entity.LocalGovernmentArea.Code}:{entity.Code}",
                entity => entity.Id,
                cancellationToken);

        await SeedPollingUnitsAsync(pollingUnitWardIds, cancellationToken);
    }

    private async Task SeedPollingUnitsAsync(IReadOnlyDictionary<string, int> wardsByKey, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_dataDirectory, "polling-units.csv");
        if (!File.Exists(path)) throw new FileNotFoundException($"Seed data file was not found: {path}");

        var expectedCount = File.ReadLines(path).Skip(1).LongCount(line => !string.IsNullOrWhiteSpace(line));
        var currentCount = await db.PollingUnits.LongCountAsync(cancellationToken);
        if (currentCount == expectedCount)
        {
            logger.LogInformation("Polling-unit seed is already complete: {Count} records.", currentCount);
            return;
        }

        var existingFullCodes = (await db.PollingUnits
                .AsNoTracking()
                .Select(entity => entity.FullCode)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        const int batchSize = 1000;
        var batch = new List<PollingUnit>(batchSize);
        var insertedCount = 0;
        var skippedCount = 0;

        foreach (var line in File.ReadLines(path).Skip(1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var item = ParsePollingUnit(line);
            var wardKey = $"{item.StateCode}:{item.LgaCode}:{item.WardCode}";
            if (!wardsByKey.TryGetValue(wardKey, out var wardId) || !existingFullCodes.Add(item.FullCode))
            {
                skippedCount++;
                continue;
            }

            batch.Add(new PollingUnit
            {
                WardId = wardId,
                Code = item.Code,
                Name = item.Name,
                Location = string.IsNullOrWhiteSpace(item.Location) ? null : item.Location,
                FullCode = item.FullCode,
                InecPortalId = string.IsNullOrWhiteSpace(item.PortalId) ? null : item.PortalId
            });

            if (batch.Count < batchSize) continue;
            await SavePollingUnitBatchAsync(batch, cancellationToken);
            insertedCount += batch.Count;
            batch.Clear();
        }

        if (batch.Count > 0)
        {
            await SavePollingUnitBatchAsync(batch, cancellationToken);
            insertedCount += batch.Count;
        }

        logger.LogInformation(
            "Polling-unit seed completed: {InsertedCount} inserted, {SkippedCount} skipped, {TotalCount} total.",
            insertedCount,
            skippedCount,
            currentCount + insertedCount);
    }

    private async Task SavePollingUnitBatchAsync(List<PollingUnit> batch, CancellationToken cancellationToken)
    {
        db.PollingUnits.AddRange(batch);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    private static SeedPollingUnit ParsePollingUnit(string line)
    {
        var columns = ParseCsvLine(line);
        return new SeedPollingUnit(
            columns.ElementAtOrDefault(0) ?? string.Empty,
            columns.ElementAtOrDefault(2) ?? string.Empty,
            columns.ElementAtOrDefault(4) ?? string.Empty,
            columns.ElementAtOrDefault(6) ?? string.Empty,
            columns.ElementAtOrDefault(7) ?? string.Empty,
            columns.ElementAtOrDefault(8) ?? string.Empty,
            columns.ElementAtOrDefault(9) ?? string.Empty,
            columns.ElementAtOrDefault(10) ?? string.Empty);
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var value = new System.Text.StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    value.Append('"');
                    index++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (character == ',' && !quoted)
            {
                values.Add(value.ToString());
                value.Clear();
            }
            else
            {
                value.Append(character);
            }
        }

        values.Add(value.ToString());
        return values;
    }

    private List<T> Load<T>(string fileName)
    {
        var path = Path.Combine(_dataDirectory, fileName);
        if (!File.Exists(path)) throw new FileNotFoundException($"Seed data file was not found: {path}");
        return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path)) ?? [];
    }

    private sealed record SeedState(
        [property: JsonPropertyName("portal_id")] string PortalId,
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("name")] string Name);

    private sealed record SeedLga(
        [property: JsonPropertyName("portal_id")] string PortalId,
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("state_code")] string StateCode);

    private sealed record SeedWard(
        [property: JsonPropertyName("portal_id")] string PortalId,
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("lga_code")] string LgaCode,
        [property: JsonPropertyName("state_code")] string StateCode);

    private sealed record SeedPollingUnit(
        string StateCode,
        string LgaCode,
        string WardCode,
        string Code,
        string Name,
        string Location,
        string FullCode,
        string PortalId);
}
