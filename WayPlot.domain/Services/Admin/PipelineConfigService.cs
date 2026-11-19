using System.Text.Json;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;
using WayPlot.domain.Events;
using WayPlot.domain.Interfaces;
using WayPlot.domain.Repositories;
using WayPlot.domain.Services.DataCollection;

namespace WayPlot.domain.Services.Admin;

public sealed class PipelineConfigService(
    ITenantUnitOfWorkFactory uowFactory,
    ICurrentUser user,
    IClock clock,
    TenantAccessPolicy access,
    IPipelineStageProvider stages,
    IDomainEventDispatcher events)
{
    private const string SettingKey = "PipelineStages";

    public async Task<ServiceResult<PipelineStageConfigSnapshot>> GetAsync()
    {
        var scope = access.GetDataReadScope("pipeline stages");
        if (!scope.Ok)
            return ServiceResult<PipelineStageConfigSnapshot>.Fail(scope.Message);

        await using var uow = uowFactory.Create();
        if (!TryRead(await uow.Settings.GetForDisplayAsync(SettingKey), out var snapshot))
            return ServiceResult<PipelineStageConfigSnapshot>.Fail(
                "Pipeline stage configuration could not be read. No changes were made.");

        return ServiceResult<PipelineStageConfigSnapshot>.Success(snapshot);
    }

    public async Task<ServiceResult<PipelineStageAdminSnapshot>> GetAllForAdminAsync()
    {
        var permission = access.RequireTenantAdmin("configure pipeline stages");
        if (!permission.Ok)
            return ServiceResult<PipelineStageAdminSnapshot>.Fail(permission.Message);

        var config = await GetAsync();
        if (!config.Ok || config.Value is null)
            return ServiceResult<PipelineStageAdminSnapshot>.Fail(config.Message);

        await stages.EnsureLoadedAsync();
        var allStages = await stages.GetAllForAdminAsync();
        return ServiceResult<PipelineStageAdminSnapshot>.Success(new PipelineStageAdminSnapshot(
            config.Value.Current?.Version ?? 0,
            allStages));
    }

    public async Task<ServiceResult<PipelineStageConfigSnapshot>> SaveAsync(PipelineStageSaveRequest request)
    {
        var permission = access.RequireTenantAdmin("change pipeline stages");
        if (!permission.Ok)
            return ServiceResult<PipelineStageConfigSnapshot>.Fail(permission.Message);
        if (request is null)
            return ServiceResult<PipelineStageConfigSnapshot>.Fail("Provide the pipeline stage configuration to save.");
        if (request.Stages is null || request.Stages.Any(stage => stage is null))
            return ServiceResult<PipelineStageConfigSnapshot>.Fail(
                "The pipeline configuration must contain all seven configurable stages.");

        var normalizedStages = request.Stages
            .Select(stage => stage with { Name = stage.Name?.Trim() ?? string.Empty })
            .ToList();
        var validation = PipelineStageDefinition.Validate(normalizedStages);
        if (validation is not null)
            return ServiceResult<PipelineStageConfigSnapshot>.Fail(validation);

        var scope = access.GetDataReadScope("pipeline stages");
        if (!scope.Ok)
            return ServiceResult<PipelineStageConfigSnapshot>.Fail(scope.Message);

        await using var uow = uowFactory.Create();
        var setting = await uow.Settings.GetByKeyAsync(SettingKey);
        if (!TryRead(setting, out var current))
            return ServiceResult<PipelineStageConfigSnapshot>.Fail(
                "Pipeline stage configuration could not be read. No changes were made.");

        var currentVersion = current.Current?.Version ?? 0;
        if (request.ExpectedVersion != currentVersion)
            return ServiceResult<PipelineStageConfigSnapshot>.Fail(
                "Pipeline stages changed concurrently. Reload and try again.");

        var leads = await uow.Leads.ListAsync(null, null, scope: scope.Value);
        foreach (var stage in normalizedStages!)
        {
            if (stage.IsActive)
                continue;

            var openLeadCount = leads.Count(lead =>
                lead.Stage == stage.Stage && lead.Stage is not (LeadStage.Closed or LeadStage.Lost));
            if (openLeadCount > 0)
                return ServiceResult<PipelineStageConfigSnapshot>.Fail(
                    $"Cannot turn off {stage.Name}: {openLeadCount} open " +
                    $"{(openLeadCount == 1 ? "lead is" : "leads are")} in this stage.");
        }

        var version = new PipelineStageConfigVersion(
            currentVersion + 1,
            clock.UtcNow,
            user.UserName,
            normalizedStages!);
        var history = current.History.Append(version).ToList();
        var value = PipelineStageConfigJson.Serialize(new PipelineStageConfigHistory(history));

        if (setting is null)
            uow.Settings.Add(new Setting { Key = SettingKey, Value = value });
        else
            setting.Value = value;

        try
        {
            await uow.ExecuteInTransactionAsync(async () => await uow.SaveAsync());
        }
        catch (SettingConcurrencyConflictException)
        {
            return ServiceResult<PipelineStageConfigSnapshot>.Fail(
                "Pipeline stages changed concurrently. Reload and try again.");
        }

        var changedAtUtc = clock.UtcNow;
        await events.DispatchAsync(new TenantSettingChanged(SettingKey, changedAtUtc));
        await events.DispatchAsync(new PipelineConfigChanged(changedAtUtc));
        return ServiceResult<PipelineStageConfigSnapshot>.Success(
            new PipelineStageConfigSnapshot(version, history),
            $"Pipeline stages saved as version {version.Version}.");
    }

    private static bool TryRead(Setting? setting, out PipelineStageConfigSnapshot snapshot)
    {
        if (setting is null)
        {
            snapshot = new PipelineStageConfigSnapshot(null, Array.Empty<PipelineStageConfigVersion>());
            return true;
        }

        try
        {
            var data = PipelineStageConfigJson.Deserialize(setting.Value);
            if (data?.History is null || data.History.Count == 0
                || data.History.Any(version => version is null
                    || PipelineStageDefinition.Validate(version.Stages) is not null))
            {
                snapshot = new PipelineStageConfigSnapshot(null, Array.Empty<PipelineStageConfigVersion>());
                return false;
            }

            var history = data.History.OrderBy(version => version.Version).ToList();
            snapshot = new PipelineStageConfigSnapshot(history[^1], history);
            return true;
        }
        catch (JsonException)
        {
            snapshot = new PipelineStageConfigSnapshot(null, Array.Empty<PipelineStageConfigVersion>());
            return false;
        }
    }
}
