using System.IO;
using System.Windows;

namespace MouseEffects.Effects.ColorBlindnessNG.UI;

public partial class PresetNameDialog : Window
{
    private readonly PresetManager _presetManager;
    private readonly bool _checkForDuplicates;

    public string PresetName { get; private set; } = string.Empty;

    public PresetNameDialog(PresetManager presetManager, string initialName = "", bool checkForDuplicates = true)
    {
        InitializeComponent();
        _presetManager = presetManager;
        _checkForDuplicates = checkForDuplicates;
        PresetNameTextBox.Text = initialName;
        PresetNameTextBox.SelectAll();
        PresetNameTextBox.Focus();
    }

    private void PresetNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        ValidateInput();
    }

    private void ValidateInput()
    {
        var name = PresetNameTextBox.Text.Trim();
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

        // Check for duplicates
        if (_checkForDuplicates && _presetManager.PresetExists(name))
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
        PresetName = PresetNameTextBox.Text.Trim();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
