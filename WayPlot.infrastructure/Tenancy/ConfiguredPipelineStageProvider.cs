using System.Collections.Concurrent;
using System.Text.Json;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Events;
using WayPlot.domain.Interfaces;
using WayPlot.domain.Repositories;
using WayPlot.domain.Services.DataCollection;

namespace WayPlot.infrastructure.Tenancy;

public sealed class ConfiguredPipelineStageProvider(
    ITenantUnitOfWorkFactory uowFactory,
    ICurrentUser user,
    TenantAccessPolicy access)
    : IPipelineStageProvider, IDomainEventHandler<PipelineConfigChanged>
{
    private const string SettingKey = "PipelineStages";
    private readonly ConcurrentDictionary<Guid, IReadOnlyList<PipelineStageSetting>> _cache = new();
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _loadGates = new();

    public async Task EnsureLoadedAsync()
    {
        if (user.TenantId is not { } tenantId || _cache.ContainsKey(tenantId))
            return;

        var gate = _loadGates.GetOrAdd(tenantId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            if (_cache.ContainsKey(tenantId))
                return;

            await using var uow = uowFactory.Create();
            var setting = await uow.Settings.GetForDisplayAsync(SettingKey);
            _cache[tenantId] = ReadConfiguration(setting?.Value);
        }
        finally
        {
            gate.Release();
        }
    }

    public IReadOnlyList<PipelineStageInfo> GetStages() =>
        PipelineStageDefinition.GetStages(GetCurrentConfiguration());

    public IReadOnlyList<PipelineStageSetting> GetConfiguration() => GetCurrentConfiguration();

    public string GetDisplayName(LeadStage stage) =>
        PipelineStageDefinition.GetDisplayName(stage, GetCurrentConfiguration());

    public LeadStage ClosingStage => PipelineStageDefinition.ClosingStage(GetCurrentConfiguration());

    public async Task<IReadOnlyList<PipelineStageAdminInfo>> GetAllForAdminAsync()
    {
        var permission = access.RequireTenantAdmin("view configurable pipeline stages");
        if (!permission.Ok)
            throw new UnauthorizedAccessException(permission.Message);

        await EnsureLoadedAsync();
        var scope = access.GetDataReadScope("pipeline stage counts");
        if (!scope.Ok)
            throw new InvalidOperationException(scope.Message);

        await using var uow = uowFactory.Create();
        var leads = await uow.Leads.ListAsync(null, null, scope: scope.Value);
        var openLeadCounts = leads
            .Where(lead => lead.Stage is not (LeadStage.Closed or LeadStage.Lost))
            .GroupBy(lead => lead.Stage)
            .ToDictionary(group => group.Key, group => group.Count());
        return PipelineStageDefinition.GetAllForAdmin(GetCurrentConfiguration(), openLeadCounts);
    }

    public void Invalidate() => _cache.Clear();

    public void ClearCache() => Invalidate();

    public Task HandleAsync(PipelineConfigChanged domainEvent, CancellationToken ct = default)
    {
        Invalidate();
        return Task.CompletedTask;
    }

    private IReadOnlyList<PipelineStageSetting> GetCurrentConfiguration() =>
        user.TenantId is { } tenantId && _cache.TryGetValue(tenantId, out var configuration)
            ? configuration
            : PipelineStageDefinition.Defaults;

    private static IReadOnlyList<PipelineStageSetting> ReadConfiguration(string? json)
    {
        if (json is null)
            return PipelineStageDefinition.Defaults;

        try
        {
            var history = PipelineStageConfigJson.Deserialize(json);
            if (history?.History is null || history.History.Count == 0
                || history.History.Any(version => version is null
                    || PipelineStageDefinition.Validate(version.Stages) is not null))
                throw new JsonException("The pipeline configuration history is empty or invalid.");

            var current = history.History.OrderBy(version => version.Version).Last();
            return current.Stages;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                "Pipeline stage configuration could not be read. Reload the pipeline or contact your Tenant Admin.", ex);
        }
    }
}
