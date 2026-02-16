using System;
using Godot;

public partial class HealthBar : Control
{
    private ProgressBar _progressBar;
    private Label _healthLabel;

    private int _maxHealth;
    private int _currentHealth;

    [Export]
    public Color HealthyColor = new Color(0.2f, 0.8f, 0.2f); // Green

    [Export]
    public Color WarningColor = new Color(0.9f, 0.7f, 0.1f); // Yellow

    [Export]
    public Color DangerColor = new Color(0.9f, 0.2f, 0.2f); // Red

    [Export]
    public bool ShowHealthText = true;

    public override void _Ready()
    {
        // Setup progress bar
        _progressBar = GetNodeOrNull<ProgressBar>("ProgressBar");
        if (_progressBar == null)
        {
            // Create progress bar if it doesn't exist
            _progressBar = new ProgressBar();
            _progressBar.Name = "ProgressBar";
            _progressBar.ShowPercentage = false;
            AddChild(_progressBar);
        }

        // Setup label
        _healthLabel = GetNodeOrNull<Label>("HealthLabel");
        if (_healthLabel == null && ShowHealthText)
        {
            _healthLabel = new Label();
            _healthLabel.Name = "HealthLabel";
            _healthLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _healthLabel.AddThemeColorOverride("font_color", Colors.White);
            _healthLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
            _healthLabel.AddThemeFontSizeOverride("font_size", 12);
            AddChild(_healthLabel);
        }

        // Position above parent
        Position = new Vector2(-25, -40); // Adjust as needed
        Size = new Vector2(50, 8);
    }

    public void Initialize(int maxHealth, int currentHealth)
    {
        _maxHealth = maxHealth;
        _currentHealth = currentHealth;

        if (_progressBar != null)
        {
            _progressBar.MaxValue = maxHealth;
            _progressBar.Value = currentHealth;
        }

        UpdateDisplay();
    }

    public void UpdateHealth(int newHealth)
    {
        _currentHealth = Mathf.Clamp(newHealth, 0, _maxHealth);

        if (_progressBar != null)
        {
            // Animate health bar change
            var tween = CreateTween();
            tween.TweenProperty(_progressBar, "value", _currentHealth, 0.3f);
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        // Update color based on health percentage
        float healthPercent = (float)_currentHealth / _maxHealth;

        Color barColor;
        if (healthPercent > 0.6f)
            barColor = HealthyColor;
        else if (healthPercent > 0.3f)
            barColor = WarningColor;
        else
            barColor = DangerColor;

        if (_progressBar != null)
        {
            // Set progress bar color
            var stylebox = new StyleBoxFlat();
            stylebox.BgColor = barColor;
            _progressBar.AddThemeStyleboxOverride("fill", stylebox);
        }

        // Update text
        if (_healthLabel != null && ShowHealthText)
        {
            _healthLabel.Text = $"{_currentHealth}/{_maxHealth}";
        }
    }

    public void SetMaxHealth(int maxHealth)
    {
        _maxHealth = maxHealth;
        if (_progressBar != null)
        {
            _progressBar.MaxValue = maxHealth;
        }
        UpdateDisplay();
    }
}
