using ChurchProjector.Classes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ChurchProjector.Views.Settings;

public partial class UpdateSupportViewModel : ObservableObject
{
    public UpdateSupportViewModel()
    {
        SupportRequestTypes =
        [
            new SupportRequestTypeOption(
                SupportRequestKind.Error,
                Lang.Resources.ErrorReportOption),
            new SupportRequestTypeOption(
                SupportRequestKind.Feature,
                Lang.Resources.FeatureRequestOption)
        ];
        _selectedSupportRequestType = SupportRequestTypes[0];
    }

    public IReadOnlyList<SupportRequestTypeOption> SupportRequestTypes { get; }

    public string CurrentVersion { get; } =
        Classes.Version.GetCurrentVersion() ?? "-";

    public string LogFolder { get; } =
        Path.Combine(AppContext.BaseDirectory, "logs");

    [ObservableProperty]
    private SupportRequestTypeOption _selectedSupportRequestType = null!;

    [ObservableProperty]
    private string _newestVersion = "-";

    [ObservableProperty]
    private string _releaseStatus = string.Empty;

    [ObservableProperty]
    private string _changelog = string.Empty;

    [ObservableProperty]
    private bool _isLoadingReleaseInfo;

    [ObservableProperty]
    private string _errorSubject = string.Empty;

    [ObservableProperty]
    private string _errorDescription = string.Empty;

    [ObservableProperty]
    private string _contactEmail = string.Empty;

    [ObservableProperty]
    private string _errorReportStatus = string.Empty;

    [ObservableProperty]
    private bool _isSubmittingErrorReport;

    [ObservableProperty]
    private bool _privacyAccepted;

    [RelayCommand]
    public async Task LoadReleaseInfoAsync()
    {
        if (IsLoadingReleaseInfo)
        {
            return;
        }

        IsLoadingReleaseInfo = true;
        ReleaseStatus = Lang.Resources.LoadingReleaseInfo;
        try
        {
            WebsiteRelease? release = await WebsiteService.GetNewestReleaseAsync();
            if (release is null)
            {
                NewestVersion = "-";
                Changelog = string.Empty;
                ReleaseStatus = Lang.Resources.ReleaseInfoFailed;
                return;
            }

            NewestVersion = release.Version;
            Changelog = string.IsNullOrWhiteSpace(release.Body)
                ? Lang.Resources.NoChangelog
                : release.Body;
            ReleaseStatus = VersionsAreEqual(CurrentVersion, NewestVersion)
                ? Lang.Resources.VersionIsCurrent
                : string.Format(Lang.Resources.UpdateAvailable, NewestVersion);
        }
        catch
        {
            NewestVersion = "-";
            Changelog = string.Empty;
            ReleaseStatus = Lang.Resources.ReleaseInfoFailed;
        }
        finally
        {
            IsLoadingReleaseInfo = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSubmitSupportRequest))]
    private async Task SubmitSupportRequestAsync()
    {
        IsSubmittingErrorReport = true;
        ErrorReportStatus = Lang.Resources.ErrorReportSending;
        try
        {
            ErrorReportRequest request = new()
            {
                Subject = ErrorSubject.Trim(),
                Description = ErrorDescription.Trim(),
                Version = CurrentVersion,
                Platform = $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})",
                ContactEmail = ContactEmail.Trim()
            };
            WebsiteRequestResult result =
                SelectedSupportRequestType.Kind == SupportRequestKind.Error
                    ? await WebsiteService.SubmitErrorReportAsync(request)
                    : await WebsiteService.SubmitFeatureRequestAsync(request);

            if (result.IsSuccess)
            {
                ErrorSubject = string.Empty;
                ErrorDescription = string.Empty;
                ContactEmail = string.Empty;
                PrivacyAccepted = false;
                ErrorReportStatus = Lang.Resources.ErrorReportSent;
            }
            else
            {
                ErrorReportStatus = result.Message
                                    ?? Lang.Resources.ErrorReportFailed;
            }
        }
        catch
        {
            ErrorReportStatus = Lang.Resources.ErrorReportFailed;
        }
        finally
        {
            IsSubmittingErrorReport = false;
        }
    }

    private bool CanSubmitSupportRequest()
    {
        return !IsSubmittingErrorReport
               && PrivacyAccepted
               && ErrorSubject.Trim().Length is >= 5 and <= 120
               && ErrorDescription.Trim().Length is >= 20 and <= 5_000;
    }

    partial void OnErrorSubjectChanged(string value)
    {
        SubmitSupportRequestCommand.NotifyCanExecuteChanged();
    }

    partial void OnErrorDescriptionChanged(string value)
    {
        SubmitSupportRequestCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsSubmittingErrorReportChanged(bool value)
    {
        SubmitSupportRequestCommand.NotifyCanExecuteChanged();
    }

    partial void OnPrivacyAcceptedChanged(bool value)
    {
        SubmitSupportRequestCommand.NotifyCanExecuteChanged();
    }

    private static bool VersionsAreEqual(
        string currentVersion,
        string newestVersion)
    {
        static int[] ParseParts(string value)
        {
            string normalized = value.Trim().TrimStart('v', 'V');
            string numericPart = normalized.Split('-', '+')[0];
            if (string.IsNullOrWhiteSpace(numericPart))
            {
                return [-1, -1, -1, -1];
            }

            int[] parts = numericPart
                .Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => int.TryParse(part, out int number) ? number : -1)
                .ToArray();
            Array.Resize(ref parts, 4);
            return parts;
        }

        int[] current = ParseParts(currentVersion);
        int[] newest = ParseParts(newestVersion);
        return !current.Contains(-1)
               && !newest.Contains(-1)
               && current.SequenceEqual(newest);
    }
}

public enum SupportRequestKind
{
    Error,
    Feature
}

public sealed record SupportRequestTypeOption(
    SupportRequestKind Kind,
    string DisplayName);
