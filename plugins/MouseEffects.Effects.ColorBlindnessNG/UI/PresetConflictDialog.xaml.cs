using System.IO;
using System.Windows;

namespace MouseEffects.Effects.ColorBlindnessNG.UI;

public enum ConflictResolution
{
    Cancel,
    Overwrite,
    Rename
}

public partial class PresetConflictDialog : Window
{
    private readonly PresetManager _presetManager;
    private readonly string _originalName;

    public ConflictResolution Resolution { get; private set; } = ConflictResolution.Cancel;
    public string NewName { get; private set; } = string.Empty;

    public PresetConflictDialog(PresetManager presetManager, string existingName, string suggestedNewName)
    {
        InitializeComponent();
        _presetManager = presetManager;
        _originalName = existingName;

        PresetNameRun.Text = existingName;
        NewNameTextBox.Text = suggestedNewName;
        NewNameTextBox.SelectAll();

        ValidateNewName();
    }

    private void RadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (NewNameTextBox != null)
        {
            NewNameTextBox.IsEnabled = RenameRadio.IsChecked == true;
            ValidateNewName();
        }
    }

    private void NewNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        ValidateNewName();
    }

    private void ValidateNewName()
    {
        if (OverwriteRadio.IsChecked == true)
        {
            ErrorText.Visibility = Visibility.Collapsed;
            OkButton.IsEnabled = true;
            return;
        }

        var name = NewNameTextBox.Text.Trim();
        ErrorText.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(name))
        {
            OkButton.IsEnabled = false;
            return;
        }

        // Check for invalid filename characters
        var invalidChars = Path.GetInvalidFileNameChars();
        if (name.Any(c => invalidChars.Contains(c)))
        {
            ErrorText.Text = "Name contains invalid characters.";
            ErrorText.Visibility = Visibility.Visible;
            OkButton.IsEnabled = false;
            return;
        }

        // Check for duplicates (but allow the original name for overwrite scenario)
        if (_presetManager.PresetExists(name))
        {
            ErrorText.Text = "A preset with this name already exists.";
            ErrorText.Visibility = Visibility.Visible;
            OkButton.IsEnabled = false;
            return;
        }

        OkButton.IsEnabled = true;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (OverwriteRadio.IsChecked == true)
        {
            Resolution = ConflictResolution.Overwrite;
            NewName = _originalName;
        }
        else
        {
            Resolution = ConflictResolution.Rename;
            NewName = NewNameTextBox.Text.Trim();
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Resolution = ConflictResolution.Cancel;
        DialogResult = false;
        Close();
    }
}
