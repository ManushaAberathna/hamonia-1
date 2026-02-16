using System;
using Godot;

public partial class Player : CharacterBody2D
{
    // Signals
    [Signal]
    public delegate void ActionCompletedEventHandler(string actionType);

    [Signal]
    public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

    [Signal]
    public delegate void PlayerDeadEventHandler();

    // Stats
    [Export]
    public int MaxHealth = 100;

    [Export]
    public int AttackDamage = 20;

    [Export]
    public int HealAmount = 25;

    [Export]
    public int BlockAmount = 15;

    private int _currentHealth;
    public bool IsAlive => _currentHealth > 0;

    // Combat Manager Reference (CACHED)
    private CombatManager _combatManager;

    // UI References
    private HealthBar _healthBar;

    // State
    private bool _isBlocking = false;
    private string _currentAction = "";

    // Movement
    [Export]
    public float MoveSpeed = 200.0f;
    private bool _canMove = true;

    public override void _Ready()
    {
        _currentHealth = MaxHealth;

        // Get health bar reference
        _healthBar = GetNodeOrNull<HealthBar>("HealthBar");
        if (_healthBar != null)
        {
            _healthBar.Initialize(MaxHealth, _currentHealth);
        }

        // Cache the CombatManager reference
        FindCombatManager();

        EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
    }

    private void FindCombatManager()
    {
        // Try multiple paths to find CombatManager
        _combatManager = GetTree()
            .Root.GetNodeOrNull<CombatManager>("Main/GameController/CombatManager");

        if (_combatManager == null)
        {
            _combatManager = GetNodeOrNull<CombatManager>("../GameController/CombatManager");
        }

        if (_combatManager == null)
        {
            _combatManager = GetTree()
                .CurrentScene.GetNodeOrNull<CombatManager>("GameController/CombatManager");
        }

        if (_combatManager == null)
        {
            var nodes = GetTree().GetNodesInGroup("combat_manager");
            if (nodes.Count > 0)
            {
                _combatManager = nodes[0] as CombatManager;
            }
        }

        if (_combatManager != null)
        {
            GD.Print("CombatManager found and cached!");
        }
        else
        {
            GD.PrintErr("WARNING: CombatManager not found! Player attacks won't work.");
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_canMove && IsAlive)
        {
            HandleMovement(delta);
        }
    }

    private void HandleMovement(double delta)
    {
        Vector2 velocity = Velocity;
        Vector2 direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");

        if (direction != Vector2.Zero)
        {
            velocity = direction * MoveSpeed;
        }
        else
        {
            velocity = velocity.MoveToward(Vector2.Zero, MoveSpeed * 10 * (float)delta);
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    // Called when player successfully sings a note
    public void OnNoteSung(string note, float confidence)
    {
        if (confidence < 0.7f)
        {
            GD.Print($"Note confidence too low: {confidence}. Action failed.");
            return;
        }

        // Check if combat is active
        if (_combatManager == null || !_combatManager.IsCombatActive() || !_combatManager.IsPlayerTurn())
        {
            GD.Print("Not in combat or not player's turn!");
            return;
        }

        // Check if player already acted this turn
        if (_combatManager.HasPlayerActionCompleted())
        {
            GD.Print("Action already performed this turn!");
            return;
        }

        // Get selected enemy
        var enemy = _combatManager.GetSelectedEnemy();
        if (enemy == null)
        {
            GD.Print("No enemy selected!");
            return;
        }

        // Try to match note with enemy's sequence
        bool noteMatched = enemy.CheckNote(note);

        if (noteMatched)
        {
            GD.Print($"Correct note sung: {note}");
            
            // If enemy sequence is complete, it dies automatically in Enemy.CheckNote()
            // Just mark action as completed
            EmitSignal(SignalName.ActionCompleted, "sing_note");
        }
        else
        {
            // Wrong note - still counts as an action but enemy attacks back
            GD.Print($"Wrong note! Expected: {enemy.GetCurrentRequiredNote()}");
            EmitSignal(SignalName.ActionCompleted, "sing_note_wrong");
        }
    }

    public void PerformAction(string actionType)
    {
        if (!IsAlive)
            return;

        if (_combatManager != null && _combatManager.IsPlayerTurn() && _combatManager.HasPlayerActionCompleted())
        {
            GD.Print("Action already performed this turn!");
            return;
        }

        _currentAction = actionType;
        GD.Print($"Player performing action: {actionType}");

        switch (actionType.ToLower())
        {
            case "attack":
                PerformAttack();
                break;
            case "block":
                PerformBlock();
                break;
            case "heal":
                PerformHeal();
                break;
        }

        EmitSignal(SignalName.ActionCompleted, actionType);
    }

    private void PerformAttack()
    {
        if (_combatManager == null)
        {
            GD.PrintErr("CombatManager not found! Cannot attack.");
            FindCombatManager();
            return;
        }

        var enemy = _combatManager.GetSelectedEnemy();
        if (enemy != null)
        {
            enemy.TakeDamage(AttackDamage);
            GD.Print($"Player attacks for {AttackDamage} damage!");
            PlayAttackEffect();
        }
        else
        {
            GD.Print("No enemy selected!");
        }
    }

    private void PerformBlock()
    {
        _isBlocking = true;
        GD.Print($"Player is blocking! Damage reduction: {BlockAmount}");
        PlayBlockEffect();
    }

    private void PerformHeal()
    {
        int healedAmount = Math.Min(HealAmount, MaxHealth - _currentHealth);
        _currentHealth += healedAmount;

        GD.Print($"Player heals for {healedAmount} HP!");

        if (_healthBar != null)
        {
            _healthBar.UpdateHealth(_currentHealth);
        }

        EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
        PlayHealEffect();
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive)
            return;

        if (_isBlocking)
        {
            damage = Math.Max(0, damage - BlockAmount);
            GD.Print($"Blocked! Reduced damage to {damage}");
            _isBlocking = false;
        }

        _currentHealth -= damage;
        _currentHealth = Math.Max(0, _currentHealth);

        GD.Print($"Player takes {damage} damage! Health: {_currentHealth}/{MaxHealth}");

        if (_healthBar != null)
        {
            _healthBar.UpdateHealth(_currentHealth);
        }

        EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);

        if (_currentHealth <= 0)
        {
            Die();
        }
        else
        {
            PlayHurtEffect();
        }
    }

    private void Die()
    {
        GD.Print("Player died!");
        EmitSignal(SignalName.PlayerDead);
        PlayDeathEffect();
        _canMove = false;
    }

    public void ResetBlockState()
    {
        _isBlocking = false;
    }

    private void PlayAttackEffect()
    {
        Modulate = new Color(1, 0.5f, 0.5f);
        GetTree().CreateTimer(0.2).Timeout += () => Modulate = Colors.White;
    }

    private void PlayBlockEffect()
    {
        Modulate = new Color(0.5f, 0.5f, 1);
        GetTree().CreateTimer(0.2).Timeout += () => Modulate = Colors.White;
    }

    private void PlayHealEffect()
    {
        Modulate = new Color(0.5f, 1, 0.5f);
        GetTree().CreateTimer(0.2).Timeout += () => Modulate = Colors.White;
    }

    private void PlayHurtEffect()
    {
        Modulate = new Color(1, 0, 0);
        GetTree().CreateTimer(0.2).Timeout += () => Modulate = Colors.White;
    }

    private void PlayDeathEffect()
    {
        Modulate = new Color(0.3f, 0.3f, 0.3f);
    }
}
