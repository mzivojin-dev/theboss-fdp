using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FoaieDeParcurs.App.Services;
using FoaieDeParcurs.Core.Domain;
using FoaieDeParcurs.Core.Entities;
using FoaieDeParcurs.Core.Repositories;
using Microsoft.Maui.ApplicationModel.Communication;

namespace FoaieDeParcurs.App.ViewModels.Dashboard;

/// <summary>
/// Backs the Dashboard — the app's Home tab: this month's stats, a compact editable recent
/// fill-ups list, and batch PDF export (this month / everything not yet marked sent) so the
/// user doesn't have to open and email fill-ups one at a time at the end of the month.
/// </summary>
public sealed partial class DashboardViewModel(
    IFillUpRepository fillUpRepository,
    IRouteSegmentRepository routeSegmentRepository,
    IVehicleProfileRepository vehicleProfileRepository,
    IFoaieDeParcursDocumentService documentService)
    : ObservableObject
{
    private const int RecentFillUpCount = 8;

    private List<FillUp> _allFillUps = [];

    public ObservableCollection<FillUp> RecentFillUps { get; } = [];

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private string _monthLabel = DateTimeOffset.Now.ToString("MMMM yyyy");

    [ObservableProperty]
    private int _monthFillUpCount;

    [ObservableProperty]
    private double _monthTotalKm;

    [ObservableProperty]
    private decimal _monthTotalPaid;

    [ObservableProperty]
    private string _currency = "RON";

    [ObservableProperty]
    private int _unsentCount;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsRefreshing = true;
        try
        {
            _allFillUps = await fillUpRepository.GetAllAsync();
            var segments = await routeSegmentRepository.GetAllAsync();

            var now = DateTimeOffset.Now;
            MonthLabel = now.ToString("MMMM yyyy");
            var summary = DashboardStatistics.ForMonth(_allFillUps, segments, now.Year, now.Month);
            MonthFillUpCount = summary.FillUpCount;
            MonthTotalKm = summary.TotalDistanceKm;
            MonthTotalPaid = summary.TotalAmountPaid;
            Currency = _allFillUps.Find(f => !string.IsNullOrWhiteSpace(f.Currency))?.Currency ?? "RON";
            UnsentCount = _allFillUps.Count(f => !f.EmailSent);

            RecentFillUps.Clear();
            foreach (var fillUp in _allFillUps.Take(RecentFillUpCount))
            {
                RecentFillUps.Add(fillUp);
            }

            IsEmpty = _allFillUps.Count == 0;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private static async Task AddAsync() =>
        await Shell.Current.GoToAsync(nameof(Views.FillUps.FillUpCapturePage));

    [RelayCommand]
    private static async Task ViewAsync(FillUp fillUp) =>
        await Shell.Current.GoToAsync(nameof(Views.FillUps.FillUpCapturePage),
            new Dictionary<string, object> { [FillUps.FillUpCaptureViewModel.FillUpIdQueryKey] = fillUp.Id });

    [RelayCommand]
    private async Task DeleteAsync(FillUp fillUp)
    {
        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Ștergere alimentare",
            $"Ștergeți alimentarea din {fillUp.Timestamp.LocalDateTime:g}? Segmentele de traseu asociate vor fi șterse și ele.",
            "Șterge",
            "Anulează");

        if (!confirmed)
        {
            return;
        }

        await fillUpRepository.DeleteAsync(fillUp.Id);
        await LoadAsync();
    }

    /// <summary>Exports every fill-up from the current calendar month in one email.</summary>
    [RelayCommand]
    private Task ExportThisMonthAsync()
    {
        var now = DateTimeOffset.Now;
        var toExport = _allFillUps.Where(f => f.Timestamp.Year == now.Year && f.Timestamp.Month == now.Month).ToList();
        return ExportAsync(toExport, $"alimentările din {now:MMMM yyyy}");
    }

    /// <summary>Exports every fill-up not yet confirmed sent — "since last export" without needing a separate timestamp to track.</summary>
    [RelayCommand]
    private Task ExportUnsentAsync()
    {
        var toExport = _allFillUps.Where(f => !f.EmailSent).ToList();
        return ExportAsync(toExport, "alimentările netrimise");
    }

    /// <summary>
    /// Builds a PDF per fill-up and hands them all to the platform email compose intent as one
    /// message with multiple attachments — same native ComposeAsync flow as the single-fill-up
    /// email, just with more attachments, so no email credentials ever touch the app. Only marks
    /// EmailSent once the user confirms they actually finished sending (Android can't verify
    /// delivery), so a cancelled or abandoned compose doesn't silently mark things as sent.
    /// </summary>
    private async Task ExportAsync(List<FillUp> fillUps, string description)
    {
        if (fillUps.Count == 0)
        {
            StatusMessage = $"Nu există {description} de exportat.";
            return;
        }

        IsExporting = true;
        StatusMessage = null;
        try
        {
            var attachments = new List<EmailAttachment>();
            foreach (var fillUp in fillUps)
            {
                var path = await documentService.BuildPdfAsync(fillUp.Id);
                if (path is not null)
                {
                    attachments.Add(new EmailAttachment(path));
                }
            }

            if (attachments.Count == 0)
            {
                StatusMessage = "Nu s-a putut exporta nimic.";
                return;
            }

            var profile = await vehicleProfileRepository.GetOrCreateAsync();
            var message = new EmailMessage
            {
                Subject = $"Foi de parcurs - {description}",
                Body = $"Bună ziua,\n\nAtașat găsiți {attachments.Count} foi de parcurs ({description}).\n\nCu stimă,\n{profile.DriverName}",
                To = string.IsNullOrWhiteSpace(profile.EmailRecipient) ? [] : [profile.EmailRecipient]
            };
            foreach (var attachment in attachments)
            {
                message.Attachments.Add(attachment);
            }

            try
            {
                await Email.Default.ComposeAsync(message);
            }
            catch (FeatureNotSupportedException)
            {
                StatusMessage = "Nu există nicio aplicație de email pe acest dispozitiv — PDF-urile au fost generate, dar nu au putut fi transmise pentru trimitere.";
                return;
            }

            var confirmedSent = await Shell.Current.DisplayAlertAsync(
                "Email trimis?",
                $"Ați finalizat trimiterea tuturor celor {attachments.Count} atașamente? (Android nu poate confirma automat acest lucru.)",
                "Da, trimis",
                "Încă nu");

            if (confirmedSent)
            {
                foreach (var fillUp in fillUps)
                {
                    await fillUpRepository.SetEmailSentAsync(fillUp.Id, true);
                }

                StatusMessage = $"Au fost marcate {fillUps.Count} alimentări ca trimise.";
                await LoadAsync();
            }
        }
        finally
        {
            IsExporting = false;
        }
    }
}
