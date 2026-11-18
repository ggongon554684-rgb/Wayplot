using System.Text.Json;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;
using WayPlot.domain.Events;
using WayPlot.domain.Interfaces;
using WayPlot.domain.Repositories;
using WayPlot.domain.Services.DataCollection;

namespace WayPlot.domain.Services.Admin;

public sealed class CompanyProfileService(
    ITenantUnitOfWorkFactory uowFactory,
    ICurrentUser user,
    TenantAccessPolicy access,
    IClock clock,
    IDomainEventDispatcher events)
{
    private const string SettingKey = "CompanyProfile";

    public async Task<ServiceResult<CompanyProfileSnapshot>> GetAsync()
    {
        var scope = access.GetDataReadScope("company profile");
        if (!scope.Ok)
            return ServiceResult<CompanyProfileSnapshot>.Fail(scope.Message);

        await using var uow = uowFactory.Create();
        var setting = await uow.Settings.GetForDisplayAsync(SettingKey);
        if (setting is null)
            return ServiceResult<CompanyProfileSnapshot>.Success(new CompanyProfileSnapshot(null));

        try
        {
            var current = JsonSerializer.Deserialize<CompanyProfileVersion>(setting.Value);
            return current is null
                ? ServiceResult<CompanyProfileSnapshot>.Fail("The company profile could not be read.")
                : ServiceResult<CompanyProfileSnapshot>.Success(new CompanyProfileSnapshot(current));
        }
        catch (JsonException)
        {
            return ServiceResult<CompanyProfileSnapshot>.Fail("The company profile could not be read.");
        }
    }

    public async Task<ServiceResult<CompanyProfileSnapshot>> SaveAsync(CompanyProfileRequest request)
    {
        var permission = access.RequireDataMutation("save the company profile");
        if (!permission.Ok)
            return ServiceResult<CompanyProfileSnapshot>.Fail(permission.Message);
        if (user.Role != Roles.TenantAdmin)
            return ServiceResult<CompanyProfileSnapshot>.Fail("Only a Tenant Admin can save the company profile.");

        var normalized = new CompanyProfileRequest(
            request.Name?.Trim() ?? string.Empty,
            request.ContactNumbers?.Trim() ?? string.Empty,
            request.Email?.Trim() ?? string.Empty,
            request.Address?.Trim() ?? string.Empty,
            request.ExpectedVersion);
        var validation = Validate(normalized);
        if (validation is not null)
            return ServiceResult<CompanyProfileSnapshot>.Fail(validation);

        await using var uow = uowFactory.Create();
        var setting = await uow.Settings.GetByKeyAsync(SettingKey);
        CompanyProfileVersion? existing = null;
        if (setting is not null)
        {
            try
            {
                existing = JsonSerializer.Deserialize<CompanyProfileVersion>(setting.Value);
            }
            catch (JsonException)
            {
                return ServiceResult<CompanyProfileSnapshot>.Fail("The company profile could not be read. No changes were made.");
            }
        }

        var currentVersion = existing?.Version ?? 0;
        if (currentVersion != normalized.ExpectedVersion)
            return ServiceResult<CompanyProfileSnapshot>.Fail("The company profile changed concurrently. Reload and try again.");

        var updated = new CompanyProfileVersion(
            currentVersion + 1,
            clock.UtcNow,
            user.UserName,
            normalized.Name,
            normalized.ContactNumbers,
            normalized.Email,
            normalized.Address);
        var value = JsonSerializer.Serialize(updated);
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
            return ServiceResult<CompanyProfileSnapshot>.Fail("The company profile changed concurrently. Reload and try again.");
        }

        await events.DispatchAsync(new TenantSettingChanged(SettingKey, clock.UtcNow));
        return ServiceResult<CompanyProfileSnapshot>.Success(
            new CompanyProfileSnapshot(updated), $"Company profile saved as version {updated.Version}.");
    }

    private static string? Validate(CompanyProfileRequest request)
    {
        if (request.Name.Length is 0 or > 150)
            return "Enter a company name no longer than 150 characters.";
        if (request.ContactNumbers.Length > 200)
            return "Contact numbers must be no longer than 200 characters.";
        if (request.Email.Length > 254)
            return "Email must be no longer than 254 characters.";
        if (request.Address.Length > 500)
            return "Address must be no longer than 500 characters.";
        return null;
    }
}
