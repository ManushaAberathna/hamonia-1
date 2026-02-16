using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Enemy : CharacterBody2D
{
    // Enemy Types
    public enum EnemyType
    {
        Goblin,
        Orc,
        Dragon
    }

    // Signals
    [Signal]
    public delegate void EnemyDeadEventHandler(Enemy enemy);

    [Signal]
    public delegate void NoteSequenceProgressEventHandler(int currentIndex, int totalNotes);

    // Stats
    [Export]
    public EnemyType Type = EnemyType.Goblin;

    [Export]
    public int MaxHealth = 50;

    [Export]
    public int AttackDamage = 15;

    // Note sequence - array of notes this enemy requires
    [Export]
    public string[] RequiredNoteSequence = { "C", "D" };

    private int _currentNoteIndex = 0; // Track progress in sequence
    private int _currentHealth;
    public bool IsAlive => _currentHealth > 0 && !IsQueuedForDeletion();

    // UI References
    private HealthBar _healthBar;
    private VBoxContainer _noteLabelsContainer;
    private List<Label> _noteLabels = new List<Label>();
    private Sprite2D _selectionIndicator;

    // Visual
    [Export]
    public Color SelectedColor = new Color(1, 1, 0, 0.5f);

    [Export]
    public Color CompletedNoteColor = new Color(0.2f, 0.8f, 0.2f); // Green for completed notes

    [Export]
    public Color CurrentNoteColor = new Color(1, 0.8f, 0.2f); // Yellow for current note

    [Export]
    public Color PendingNoteColor = new Color(0.5f, 0.5f, 0.5f); // Gray for pending notes

    private bool _isSelected = false;
    private bool _isDying = false;

    public override void _Ready()
    {
        _currentHealth = MaxHealth;
        _currentNoteIndex = 0;
        AddToGroup("enemies");

        // Auto-configure based on enemy type if sequence is default
        if (RequiredNoteSequence.Length == 0)
        {
            ConfigureDefaultSequence();
        }

        // Setup health bar
        _healthBar = GetNodeOrNull<HealthBar>("HealthBar");
        if (_healthBar != null)
        {
            _healthBar.Initialize(MaxHealth, _currentHealth);
        }

        // Setup note sequence display
        SetupNoteSequenceDisplay();

        // Setup selection indicator
        _selectionIndicator = GetNodeOrNull<Sprite2D>("SelectionIndicator");
        if (_selectionIndicator != null)
        {
            _selectionIndicator.Visible = false;
        }

        GD.Print(
            $"{Name} initialized - Type: {Type}, Sequence: [{string.Join(", ", RequiredNoteSequence)}]"
        );
    }

    private void ConfigureDefaultSequence()
    {
        // Default sequences based on enemy type
        switch (Type)
        {
            case EnemyType.Goblin:
                RequiredNoteSequence = new[] { "C", "D" };
                MaxHealth = 30;
                AttackDamage = 10;
                break;
            case EnemyType.Orc:
                RequiredNoteSequence = new[] { "C", "E", "G" };
                MaxHealth = 60;
                AttackDamage = 20;
                break;
            case EnemyType.Dragon:
                RequiredNoteSequence = new[] { "C", "E", "G", "C" };
                MaxHealth = 100;
                AttackDamage = 30;
                break;
        }
    }

    private void SetupNoteSequenceDisplay()
    {
        // Create container for note labels
        _noteLabelsContainer = GetNodeOrNull<VBoxContainer>("NoteLabelsContainer");

        if (_noteLabelsContainer == null)
        {
            _noteLabelsContainer = new VBoxContainer();
            _noteLabelsContainer.Name = "NoteLabelsContainer";
            _noteLabelsContainer.Position = new Vector2(-20, -80); // Above health bar
            AddChild(_noteLabelsContainer);
        }

        // Clear existing labels
        foreach (var child in _noteLabelsContainer.GetChildren())
        {
            child.QueueFree();
        }
        _noteLabels.Clear();

        // Create label for each note in sequence
        for (int i = 0; i < RequiredNoteSequence.Length; i++)
        {
            var noteLabel = new Label();
            noteLabel.Text = RequiredNoteSequence[i];
            noteLabel.HorizontalAlignment = HorizontalAlignment.Center;
            noteLabel.AddThemeFontSizeOverride("font_size", 18);

            // Set initial color based on position
            if (i == 0)
            {
                noteLabel.AddThemeColorOverride("font_color", CurrentNoteColor);
            }
            else
            {
                noteLabel.AddThemeColorOverride("font_color", PendingNoteColor);
            }

            _noteLabelsContainer.AddChild(noteLabel);
            _noteLabels.Add(noteLabel);
        }
    }

    public void SetSelected(bool selected)
    {
        if (_isDying || IsQueuedForDeletion())
            return;

        _isSelected = selected;

        if (_selectionIndicator != null && IsInstanceValid(_selectionIndicator))
        {
            _selectionIndicator.Visible = selected;
        }

        if (selected)
        {
            Modulate = SelectedColor;
        }
        else
        {
            Modulate = Colors.White;
        }
    }

    public void PerformAttack(Player player)
    {
        if (!IsAlive || player == null || _isDying)
            return;

        GD.Print($"{Name} attacks player for {AttackDamage} damage!");
        player.TakeDamage(AttackDamage);

        PlayAttackAnimation();
    }

    // Check if sung note matches current required note
    public bool CheckNote(string sungNote)
    {
        if (_currentNoteIndex >= RequiredNoteSequence.Length)
        {
            GD.Print($"Sequence already complete for {Name}!");
            return false;
        }

        string requiredNote = RequiredNoteSequence[_currentNoteIndex];

        if (sungNote.ToUpper() == requiredNote.ToUpper())
        {
            GD.Print(
                $"Correct note! ({_currentNoteIndex + 1}/{RequiredNoteSequence.Length}) for {Name}"
            );

            // Mark current note as completed
            if (_currentNoteIndex < _noteLabels.Count)
            {
                _noteLabels[_currentNoteIndex].AddThemeColorOverride(
                    "font_color",
                    CompletedNoteColor
                );
            }

            _currentNoteIndex++;

            // Highlight next note
            if (_currentNoteIndex < _noteLabels.Count)
            {
                _noteLabels[_currentNoteIndex].AddThemeColorOverride(
                    "font_color",
                    CurrentNoteColor
                );
            }

            EmitSignal(
                SignalName.NoteSequenceProgress,
                _currentNoteIndex,
                RequiredNoteSequence.Length
            );

            // Check if sequence is complete
            if (_currentNoteIndex >= RequiredNoteSequence.Length)
            {
                GD.Print($"Sequence complete! {Name} is defeated!");
                Die();
                return true;
            }

            // Flash effect for correct note
            PlayCorrectNoteEffect();
            return true;
        }
        else
        {
            GD.Print(
                $"Wrong note! Expected {requiredNote}, got {sungNote}. Progress: {_currentNoteIndex}/{RequiredNoteSequence.Length}"
            );

            // Flash effect for wrong note
            PlayWrongNoteEffect();
            return false;
        }
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive || _isDying)
            return;

        _currentHealth -= damage;
        _currentHealth = Math.Max(0, _currentHealth);

        GD.Print($"{Name} takes {damage} damage! Health: {_currentHealth}/{MaxHealth}");

        if (_healthBar != null && IsInstanceValid(_healthBar))
        {
            _healthBar.UpdateHealth(_currentHealth);
        }

        PlayHurtEffect();

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (_isDying)
            return;

        _isDying = true;
        GD.Print($"{Name} died!");

        _isSelected = false;
        if (_selectionIndicator != null && IsInstanceValid(_selectionIndicator))
        {
            _selectionIndicator.Visible = false;
        }

        EmitSignal(SignalName.EnemyDead, this);

        PlayDeathAnimation();

        GetTree().CreateTimer(0.5).Timeout += () =>
        {
            if (IsInstanceValid(this))
            {
                QueueFree();
            }
        };
    }

    private void PlayAttackAnimation()
    {
        if (_isDying || IsQueuedForDeletion())
            return;

        var originalPos = Position;
        Position = Position + new Vector2(-10, 0);

        GetTree().CreateTimer(0.2).Timeout += () =>
        {
            if (IsInstanceValid(this) && !_isDying)
            {
                Position = originalPos;
            }
        };

        Modulate = new Color(1, 0.5f, 0.5f);
        GetTree().CreateTimer(0.2).Timeout += () =>
        {
            if (IsInstanceValid(this) && IsAlive && !_isDying)
            {
                Modulate = _isSelected ? SelectedColor : Colors.White;
            }
        };
    }

    private void PlayHurtEffect()
    {
        if (_isDying || IsQueuedForDeletion())
            return;

        Modulate = new Color(1, 0, 0);
        GetTree().CreateTimer(0.15).Timeout += () =>
        {
            if (IsInstanceValid(this) && IsAlive && !_isDying)
            {
                Modulate = _isSelected ? SelectedColor : Colors.White;
            }
        };
    }

    private void PlayCorrectNoteEffect()
    {
        if (_isDying || IsQueuedForDeletion())
            return;

        // Flash green for correct note
        Modulate = new Color(0.2f, 1, 0.2f);
        GetTree().CreateTimer(0.2).Timeout += () =>
        {
            if (IsInstanceValid(this) && IsAlive && !_isDying)
            {
                Modulate = _isSelected ? SelectedColor : Colors.White;
            }
        };
    }

    private void PlayWrongNoteEffect()
    {
        if (_isDying || IsQueuedForDeletion())
            return;

        // Shake effect for wrong note
        var originalPos = Position;
        var shakeAmount = 5;

        Position = originalPos + new Vector2(shakeAmount, 0);
        GetTree().CreateTimer(0.05).Timeout += () =>
        {
            if (IsInstanceValid(this))
            {
                Position = originalPos + new Vector2(-shakeAmount, 0);
            }
        };
        GetTree().CreateTimer(0.1).Timeout += () =>
        {
            if (IsInstanceValid(this))
            {
                Position = originalPos;
            }
        };
    }

    private void PlayDeathAnimation()
    {
        if (IsQueuedForDeletion())
            return;

        Modulate = new Color(0.5f, 0.5f, 0.5f);

        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.5f);
        tween.Parallel().TweenProperty(this, "position:y", Position.Y + 20, 0.5f);
    }

    public string[] GetRequiredNoteSequence()
    {
        return RequiredNoteSequence;
    }

    public int GetCurrentNoteIndex()
    {
        return _currentNoteIndex;
    }

    public string GetCurrentRequiredNote()
    {
        if (_currentNoteIndex < RequiredNoteSequence.Length)
        {
            return RequiredNoteSequence[_currentNoteIndex];
        }
        return "";
    }

    public bool IsSequenceComplete()
    {
        return _currentNoteIndex >= RequiredNoteSequence.Length;
    }

    public EnemyType GetEnemyType()
    {
        return Type;
    }
}
