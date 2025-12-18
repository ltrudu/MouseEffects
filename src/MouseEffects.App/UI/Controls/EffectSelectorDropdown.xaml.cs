using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using MouseEffects.Core.Effects;

namespace MouseEffects.App.UI.Controls;

/// <summary>
/// TreeView-style dropdown for selecting effects organized by category.
/// </summary>
public partial class EffectSelectorDropdown : System.Windows.Controls.UserControl
{
    private string? _selectedEffectId;

    /// <summary>
    /// Event raised when the selected effect changes.
    /// </summary>
    public event Action<string?>? EffectSelected;

    /// <summary>
    /// Gets or sets the currently selected effect ID. Null means "None" is selected.
    /// </summary>
    public string? SelectedEffectId
    {
        get => _selectedEffectId;
        set
        {
            if (_selectedEffectId != value)
            {
                _selectedEffectId = value;
                UpdateSelectedDisplay();
            }
        }
    }

    public EffectSelectorDropdown()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Populate the dropdown with effects grouped by category.
    /// </summary>
    public void PopulateEffects(IEnumerable<IEffectFactory> factories)
    {
        EffectTreeView.Items.Clear();

        // Group factories by category
        var grouped = factories
            .GroupBy(f => f.Metadata.Category)
            .OrderBy(g => GetCategoryDisplayName(g.Key));

        foreach (var group in grouped)
        {
            var categoryItem = new TreeViewItem
            {
                Header = CreateCategoryHeader(GetCategoryDisplayName(group.Key)),
                IsExpanded = true,
                Focusable = false
            };

            // Add effects under this category, sorted alphabetically
            foreach (var factory in group.OrderBy(f => f.Metadata.Name, StringComparer.OrdinalIgnoreCase))
            {
                var effectItem = new TreeViewItem
                {
                    Header = CreateEffectHeader(factory.Metadata),
                    Tag = factory.Metadata.Id,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                effectItem.MouseLeftButtonUp += EffectItem_Click;
                categoryItem.Items.Add(effectItem);
            }

            EffectTreeView.Items.Add(categoryItem);
        }

        UpdateSelectedDisplay();
    }

    private static string GetCategoryDisplayName(EffectCategory category)
    {
        return category switch
        {
            EffectCategory.Particle => "Particle Effects",
            EffectCategory.Cosmic => "Cosmic & Space",
            EffectCategory.Nature => "Nature",
            EffectCategory.Trail => "Trails",
            EffectCategory.Digital => "Digital & Tech",
            EffectCategory.Artistic => "Artistic",
            EffectCategory.Physics => "Physics",
            EffectCategory.Light => "Light & Glow",
            EffectCategory.Screen => "Screen Effects",
            EffectCategory.Other => "Other",
            _ => category.ToString()
        };
    }

    private FrameworkElement CreateCategoryHeader(string categoryName)
    {
        return new TextBlock
        {
            Text = categoryName,
            FontWeight = FontWeights.SemiBold,
            Foreground = (System.Windows.Media.Brush)FindResource("SystemControlForegroundBaseHighBrush"),
            Opacity = 0.8,
            Margin = new Thickness(0, 4, 0, 2)
        };
    }

    private FrameworkElement CreateEffectHeader(EffectMetadata metadata)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var checkmark = new Path
        {
            Data = Geometry.Parse("M0,5 L3,8 L8,0"),
            Stroke = (System.Windows.Media.Brush)FindResource("SystemControlForegroundAccentBrush"),
            StrokeThickness = 2,
            Visibility = metadata.Id == _selectedEffectId ? Visibility.Visible : Visibility.Collapsed,
            VerticalAlignment = VerticalAlignment.Center,
            Name = "Checkmark"
        };
        Grid.SetColumn(checkmark, 0);

        var textBlock = new TextBlock
        {
            Text = metadata.Name,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (System.Windows.Media.Brush)FindResource("SystemControlForegroundBaseHighBrush")
        };
        Grid.SetColumn(textBlock, 1);

        grid.Children.Add(checkmark);
        grid.Children.Add(textBlock);

        return grid;
    }

    private void NoneOption_Click(object sender, MouseButtonEventArgs e)
    {
        _selectedEffectId = null;
        UpdateSelectedDisplay();
        DropdownToggle.IsChecked = false;
        EffectSelected?.Invoke(null);
    }

    private void EffectItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is TreeViewItem item && item.Tag is string effectId)
        {
            _selectedEffectId = effectId;
            UpdateSelectedDisplay();
            DropdownToggle.IsChecked = false;
            EffectSelected?.Invoke(effectId);
            e.Handled = true;
        }
    }

    private void EffectTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        // Prevent category headers from being "selected"
        if (e.NewValue is TreeViewItem item && item.Tag == null)
        {
            // This is a category header, deselect it
            item.IsSelected = false;
        }
    }

    private void UpdateSelectedDisplay()
    {
        // Update the toggle button text
        if (string.IsNullOrEmpty(_selectedEffectId))
        {
            SelectedEffectText.Text = "None";
            NoneCheckmark.Visibility = Visibility.Visible;
        }
        else
        {
            // Find the effect name from the tree
            string? effectName = FindEffectName(_selectedEffectId);
            SelectedEffectText.Text = effectName ?? _selectedEffectId;
            NoneCheckmark.Visibility = Visibility.Collapsed;
        }

        // Update checkmarks in the tree
        UpdateCheckmarks();
    }

    private string? FindEffectName(string effectId)
    {
        foreach (var categoryItem in EffectTreeView.Items.OfType<TreeViewItem>())
        {
            foreach (var effectItem in categoryItem.Items.OfType<TreeViewItem>())
            {
                if (effectItem.Tag is string id && id == effectId)
                {
                    if (effectItem.Header is Grid grid)
                    {
                        var textBlock = grid.Children.OfType<TextBlock>().FirstOrDefault();
                        return textBlock?.Text;
                    }
                }
            }
        }
        return null;
    }

    private void UpdateCheckmarks()
    {
        foreach (var categoryItem in EffectTreeView.Items.OfType<TreeViewItem>())
        {
            foreach (var effectItem in categoryItem.Items.OfType<TreeViewItem>())
            {
                if (effectItem.Header is Grid grid)
                {
                    var checkmark = grid.Children.OfType<Path>().FirstOrDefault();
                    if (checkmark != null)
                    {
                        bool isSelected = effectItem.Tag is string id && id == _selectedEffectId;
                        checkmark.Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }
        }
    }

    private void DropdownScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Handle mouse wheel scrolling in popup (events don't bubble correctly in popups)
        var scrollViewer = sender as ScrollViewer;
        if (scrollViewer != null)
        {
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta / 3.0);
            e.Handled = true;
        }
    }
}
