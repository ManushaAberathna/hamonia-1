using System;
using Godot;

public partial class Enemy : CharacterBody2D
{
    // Signals
    [Signal]
    public delegate void EnemyDeadEventHandler(Enemy enemy);

    // Stats
    [Export]
    public int MaxHealth = 50;

    [Export]
    public int AttackDamage = 15;

    [Export]
    public string RequiredNote = "C"; // The note player must sing to defeat this enemy

    private int _currentHealth;
    public bool IsAlive => _currentHealth > 0 && !IsQueuedForDeletion();

    // UI References
    private HealthBar _healthBar;
    private Label _noteLabel;
    private Sprite2D _selectionIndicator;

    // Visual
    [Export]
    public Color SelectedColor = new Color(1, 1, 0, 0.5f); // Yellow highlight
    private bool _isSelected = false;
    private bool _isDying = false; // Track if enemy is in death process

    public override void _Ready()
    {
        _currentHealth = MaxHealth;
        AddToGroup("enemies");

        // Setup health bar
        _healthBar = GetNodeOrNull<HealthBar>("HealthBar");
        if (_healthBar != null)
        {
            _healthBar.Initialize(MaxHealth, _currentHealth);
        }

        // Setup note label
        _noteLabel = GetNodeOrNull<Label>("NoteLabel");
        if (_noteLabel != null)
        {
            _noteLabel.Text = RequiredNote;
        }

        // Setup selection indicator
        _selectionIndicator = GetNodeOrNull<Sprite2D>("SelectionIndicator");
        if (_selectionIndicator != null)
        {
            _selectionIndicator.Visible = false;
        }
    }

    public void SetSelected(bool selected)
    {
        // Don't modify if we're dying or queued for deletion
        if (_isDying || IsQueuedForDeletion())
            return;

        _isSelected = selected;

        if (_selectionIndicator != null && IsInstanceValid(_selectionIndicator))
        {
            _selectionIndicator.Visible = selected;
        }

        // Optional: Add outline or glow effect when selected
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

        GD.Print($"Enemy attacks player for {AttackDamage} damage!");
        player.TakeDamage(AttackDamage);

        // Play attack animation
        PlayAttackAnimation();
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive || _isDying)
            return;

        _currentHealth -= damage;
        _currentHealth = Math.Max(0, _currentHealth);

        GD.Print($"Enemy takes {damage} damage! Health: {_currentHealth}/{MaxHealth}");

        if (_healthBar != null && IsInstanceValid(_healthBar))
        {
            _healthBar.UpdateHealth(_currentHealth);
        }

        // Play hurt effect
        PlayHurtEffect();

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (_isDying)
            return; // Prevent multiple death calls

        _isDying = true;
        GD.Print("Enemy died!");

        // Clear selection immediately
        _isSelected = false;
        if (_selectionIndicator != null && IsInstanceValid(_selectionIndicator))
        {
            _selectionIndicator.Visible = false;
        }

        // Emit signal BEFORE starting death animation
        EmitSignal(SignalName.EnemyDead, this);

        // Play death animation
        PlayDeathAnimation();

        // Queue for deletion after animation
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

        // Move towards player slightly
        var originalPos = Position;
        Position = Position + new Vector2(-10, 0);

        GetTree().CreateTimer(0.2).Timeout += () =>
        {
            if (IsInstanceValid(this) && !_isDying)
            {
                Position = originalPos;
            }
        };

        // Flash effect
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

        // Flash red
        Modulate = new Color(1, 0, 0);
        GetTree().CreateTimer(0.15).Timeout += () =>
        {
            if (IsInstanceValid(this) && IsAlive && !_isDying)
            {
                Modulate = _isSelected ? SelectedColor : Colors.White;
            }
        };
    }

    private void PlayDeathAnimation()
    {
        if (IsQueuedForDeletion())
            return;

        // Fade out and fall
        Modulate = new Color(0.5f, 0.5f, 0.5f);

        // Tween to fade and fall
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, 0.5f);
        tween.Parallel().TweenProperty(this, "position:y", Position.Y + 20, 0.5f);
    }

    public string GetRequiredNote()
    {
        return RequiredNote;
    }
}
