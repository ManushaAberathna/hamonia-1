/*using System;
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
    public bool IsAlive => _currentHealth > 0;

    // UI References
    private HealthBar _healthBar;
    private Label _noteLabel;
    private Sprite2D _selectionIndicator;

    // Visual
    [Export]
    public Color SelectedColor = new Color(1, 1, 0, 0.5f); // Yellow highlight
    private bool _isSelected = false;

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
        _isSelected = selected;

        if (_selectionIndicator != null)
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
        if (!IsAlive || player == null)
            return;

        GD.Print($"Enemy attacks player for {AttackDamage} damage!");
        player.TakeDamage(AttackDamage);

        // Play attack animation
        PlayAttackAnimation();
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive)
            return;

        _currentHealth -= damage;
        _currentHealth = Math.Max(0, _currentHealth);

        GD.Print($"Enemy takes {damage} damage! Health: {_currentHealth}/{MaxHealth}");

        if (_healthBar != null)
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
        GD.Print("Enemy died!");
        EmitSignal(SignalName.EnemyDead, this);

        // Play death animation
        PlayDeathAnimation();

        // Optional: Drop loot, give XP, etc.

        // Remove from scene after death animation
        GetTree().CreateTimer(0.5).Timeout += () => QueueFree();
    }

    private void PlayAttackAnimation()
    {
        // Move towards player slightly
        var originalPos = Position;
        Position = Position + new Vector2(-10, 0);

        GetTree().CreateTimer(0.2).Timeout += () => Position = originalPos;

        // Flash effect
        Modulate = new Color(1, 0.5f, 0.5f);
        GetTree().CreateTimer(0.2).Timeout += () =>
        {
            if (IsAlive)
                Modulate = _isSelected ? SelectedColor : Colors.White;
        };
    }

    private void PlayHurtEffect()
    {
        // Flash red
        Modulate = new Color(1, 0, 0);
        GetTree().CreateTimer(0.15).Timeout += () =>
        {
            if (IsAlive)
                Modulate = _isSelected ? SelectedColor : Colors.White;
        };
    }

    private void PlayDeathAnimation()
    {
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
*/
