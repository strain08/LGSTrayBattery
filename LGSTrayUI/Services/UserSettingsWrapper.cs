using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Specialized;

namespace LGSTrayUI.Services;

public partial class UserSettingsWrapper : ObservableObject
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "")]
    public StringCollection SelectedDevices => Properties.Settings.Default.SelectedDevices;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "")]
    public StringCollection SelectedSignatures => Properties.Settings.Default.SelectedSignatures;

    public bool NumericDisplay
    {
        get => Properties.Settings.Default.NumericDisplay;
        set
        {
            Properties.Settings.Default.NumericDisplay = value;
            Properties.Settings.Default.Save();

            OnPropertyChanged();
        }
    }

    public void AddDevice(string deviceId)
    {
        if (Properties.Settings.Default.SelectedDevices.Contains(deviceId))
        {
            return;
        }

        Properties.Settings.Default.SelectedDevices.Add(deviceId);
        Properties.Settings.Default.Save();

        OnPropertyChanged(nameof(SelectedDevices));
    }

    public void RemoveDevice(string deviceId)
    {
        Properties.Settings.Default.SelectedDevices.Remove(deviceId);
        Properties.Settings.Default.Save();

        OnPropertyChanged(nameof(SelectedDevices));
    }

    public void AddSignature(string signature)
    {
        if (Properties.Settings.Default.SelectedSignatures.Contains(signature))
        {
            return;
        }

        Properties.Settings.Default.SelectedSignatures.Add(signature);
        Properties.Settings.Default.Save();

        OnPropertyChanged(nameof(SelectedSignatures));
    }

    public void RemoveSignature(string signature)
    {
        Properties.Settings.Default.SelectedSignatures.Remove(signature);
        Properties.Settings.Default.Save();

        OnPropertyChanged(nameof(SelectedSignatures));
    }

    public bool ContainsSignature(string signature)
    {
        return Properties.Settings.Default.SelectedSignatures.Contains(signature);
    }
}
