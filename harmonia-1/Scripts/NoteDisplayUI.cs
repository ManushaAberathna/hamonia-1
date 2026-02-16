using System;
using System.Collections.Generic;
using Godot;

public partial class NoteDisplayUI : Control
{
    // UI Elements
    private Label _currentNoteLabel;
    private Label _confidenceLabel;
    private ProgressBar _confidenceBar;
    private Panel _notePanel;
    private VBoxContainer _noteHistoryContainer;

    // Display settings
    [Export]
    public int MaxHistoryItems = 5;

    [Export]
    public float DisplayDuration = 2.0f;

    // State
    private List<NoteHistoryItem> _noteHistory = new List<NoteHistoryItem>();

    private class NoteHistoryItem
    {
        public Label Label;
        public float TimeRemaining;
    }

    public override void _Ready()
    {
        SetupUI();
    }

    private void SetupUI()
    {
        // Main container setup
        AnchorRight = 0.3f;
        AnchorBottom = 0.4f;
        GrowHorizontal = GrowDirection.End;
        GrowVertical = GrowDirection.End;

        // Create main panel
        _notePanel = new Panel();
        _notePanel.Name = "NotePanel";
        AddChild(_notePanel);

        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0, 0, 0, 0.7f);
        panelStyle.BorderColor = new Color(1, 1, 1, 0.3f);
        panelStyle.SetBorderWidthAll(2);
        panelStyle.CornerRadiusTopLeft = 10;
        panelStyle.CornerRadiusTopRight = 10;
        panelStyle.CornerRadiusBottomLeft = 10;
        panelStyle.CornerRadiusBottomRight = 10;
        _notePanel.AddThemeStyleboxOverride("panel", panelStyle);

        // Main VBox container
        var mainVBox = new VBoxContainer();
        mainVBox.Name = "MainVBox";
        _notePanel.AddChild(mainVBox);

        // Title
        var titleLabel = new Label();
        titleLabel.Text = "SINGING NOTES";
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", 20);
        titleLabel.AddThemeColorOverride("font_color", new Color(1, 0.8f, 0.2f));
        mainVBox.AddChild(titleLabel);

        var separator1 = new HSeparator();
        mainVBox.AddChild(separator1);

        // Current note display
        var currentNoteBox = new VBoxContainer();
        currentNoteBox.Name = "CurrentNoteBox";
        mainVBox.AddChild(currentNoteBox);

        _currentNoteLabel = new Label();
        _currentNoteLabel.Text = "---";
        _currentNoteLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _currentNoteLabel.AddThemeFontSizeOverride("font_size", 48);
        _currentNoteLabel.AddThemeColorOverride("font_color", Colors.White);
        currentNoteBox.AddChild(_currentNoteLabel);

        // Confidence bar
        var confidenceBox = new VBoxContainer();
        confidenceBox.Name = "ConfidenceBox";
        mainVBox.AddChild(confidenceBox);

        _confidenceLabel = new Label();
        _confidenceLabel.Text = "Confidence: 0%";
        _confidenceLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _confidenceLabel.AddThemeFontSizeOverride("font_size", 14);
        confidenceBox.AddChild(_confidenceLabel);

        _confidenceBar = new ProgressBar();
        _confidenceBar.MaxValue = 100;
        _confidenceBar.Value = 0;
        _confidenceBar.ShowPercentage = false;
        _confidenceBar.CustomMinimumSize = new Vector2(0, 20);
        confidenceBox.AddChild(_confidenceBar);

        var greenStyle = new StyleBoxFlat();
        greenStyle.BgColor = new Color(0.2f, 0.8f, 0.2f);
        _confidenceBar.AddThemeStyleboxOverride("fill", greenStyle);

        var separator2 = new HSeparator();
        mainVBox.AddChild(separator2);

        // Note history
        var historyLabel = new Label();
        historyLabel.Text = "Recent Notes:";
        historyLabel.HorizontalAlignment = HorizontalAlignment.Center;
        historyLabel.AddThemeFontSizeOverride("font_size", 14);
        mainVBox.AddChild(historyLabel);

        _noteHistoryContainer = new VBoxContainer();
        _noteHistoryContainer.Name = "HistoryContainer";
        mainVBox.AddChild(_noteHistoryContainer);

        // Set anchors and margins
        _notePanel.SetAnchorsPreset(LayoutPreset.TopWide);
        _notePanel.OffsetLeft = 10;
        _notePanel.OffsetRight = -10;
        _notePanel.OffsetTop = 10;
        _notePanel.OffsetBottom = 300;

        mainVBox.SetAnchorsPreset(LayoutPreset.FullRect);
        mainVBox.OffsetLeft = 10;
        mainVBox.OffsetRight = -10;
        mainVBox.OffsetTop = 10;
        mainVBox.OffsetBottom = -10;
    }

    public override void _Process(double delta)
    {
        // Update note history timers
        for (int i = _noteHistory.Count - 1; i >= 0; i--)
        {
            _noteHistory[i].TimeRemaining -= (float)delta;

            if (_noteHistory[i].TimeRemaining <= 0)
            {
                // Fade out and remove
                _noteHistory[i].Label.QueueFree();
                _noteHistory.RemoveAt(i);
            }
            else
            {
                // Fade alpha based on time remaining
                float alpha = _noteHistory[i].TimeRemaining / DisplayDuration;
                _noteHistory[i].Label.Modulate = new Color(1, 1, 1, alpha);
            }
        }
    }

    public void DisplayNote(string note, float confidence, float frequency)
    {
        // Update current note display
        _currentNoteLabel.Text = note;

        // Flash effect
        _currentNoteLabel.Modulate = new Color(1, 1, 0); // Yellow
        var tween = CreateTween();
        tween.TweenProperty(_currentNoteLabel, "modulate", Colors.White, 0.3f);

        // Update confidence
        int confidencePercent = Mathf.RoundToInt(confidence * 100);
        _confidenceLabel.Text = $"Confidence: {confidencePercent}%";

        var confidenceTween = CreateTween();
        confidenceTween.TweenProperty(_confidenceBar, "value", confidencePercent, 0.2f);

        // Change confidence bar color based on value
        Color barColor;
        if (confidence >= 0.8f)
            barColor = new Color(0.2f, 0.8f, 0.2f); // Green
        else if (confidence >= 0.6f)
            barColor = new Color(0.9f, 0.7f, 0.1f); // Yellow
        else
            barColor = new Color(0.9f, 0.2f, 0.2f); // Red

        var barStyle = new StyleBoxFlat();
        barStyle.BgColor = barColor;
        _confidenceBar.AddThemeStyleboxOverride("fill", barStyle);

        // Add to history
        AddToHistory(note, frequency, confidence);
    }

    private void AddToHistory(string note, float frequency, float confidence)
    {
        // Remove oldest if at max capacity
        if (_noteHistory.Count >= MaxHistoryItems)
        {
            _noteHistory[0].Label.QueueFree();
            _noteHistory.RemoveAt(0);
        }

        // Create history label
        var historyLabel = new Label();
        historyLabel.Text = $"{note} - {frequency:F1} Hz ({confidence * 100:F0}%)";
        historyLabel.AddThemeFontSizeOverride("font_size", 12);
        historyLabel.AddThemeColorOverride("font_color", Colors.LightGray);
        _noteHistoryContainer.AddChild(historyLabel);

        // Add to history list
        _noteHistory.Add(
            new NoteHistoryItem { Label = historyLabel, TimeRemaining = DisplayDuration }
        );
    }

    public void Clear()
    {
        _currentNoteLabel.Text = "---";
        _confidenceBar.Value = 0;
        _confidenceLabel.Text = "Confidence: 0%";

        foreach (var item in _noteHistory)
        {
            item.Label.QueueFree();
        }
        _noteHistory.Clear();
    }

    public void ShowSuccess(bool success)
    {
        if (success)
        {
            _notePanel.Modulate = new Color(0.2f, 0.8f, 0.2f); // Green flash
        }
        else
        {
            _notePanel.Modulate = new Color(0.9f, 0.2f, 0.2f); // Red flash
        }

        var tween = CreateTween();
        tween.TweenProperty(_notePanel, "modulate", Colors.White, 0.5f);
    }
}
