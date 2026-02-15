/*using System;
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

    // UI References
    private HealthBar _healthBar;
    private Label _noteLabel;

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

        EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
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

        // Get input direction
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
        // Map notes to actions
        // You can customize this mapping based on your game design
        string action = MapNoteToAction(note);

        if (confidence < 0.7f) // 70% confidence threshold
        {
            GD.Print($"Note confidence too low: {confidence}. Action failed.");
            return;
        }

        PerformAction(action);
    }

    private string MapNoteToAction(string note)
    {
        // Example mapping - customize as needed
        // C, D, E = Attack
        // F, G = Block
        // A, B = Heal

        switch (note.ToUpper())
        {
            case "C":
                //case "D":
                //case "E":
                return "attack";
            //case "F":
            //case "G":
            case "D":
                return "block";
            //case "A":
            //case "B":
            case "E":
                return "heal";
            default:
                return "attack"; // Default action
        }
    }

    public void PerformAction(string actionType)
    {
        if (!IsAlive)
            return;

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
        // Get selected enemy from combat manager
        var combatManager = GetTree().Root.GetNode<CombatManager>("CombatManager");
        if (combatManager != null)
        {
            var enemy = combatManager.GetSelectedEnemy();
            if (enemy != null)
            {
                enemy.TakeDamage(AttackDamage);
                GD.Print($"Player attacks for {AttackDamage} damage!");

                // Play attack animation/effect here
                PlayAttackEffect();
            }
        }
    }

    private void PerformBlock()
    {
        _isBlocking = true;
        GD.Print($"Player is blocking! Damage reduction: {BlockAmount}");

        // Play block animation/effect here
        PlayBlockEffect();

        // Block only lasts for one turn, will be reset on next player turn
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

        // Play heal animation/effect here
        PlayHealEffect();
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive)
            return;

        // Apply blocking reduction
        if (_isBlocking)
        {
            damage = Math.Max(0, damage - BlockAmount);
            GD.Print($"Blocked! Reduced damage to {damage}");
            _isBlocking = false; // Block is consumed
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

        // Play death animation
        PlayDeathEffect();

        // Disable player
        _canMove = false;
    }

    public void ResetBlockState()
    {
        _isBlocking = false;
    }

    // Visual effects placeholders - implement these with your actual animations/particles
    private void PlayAttackEffect()
    {
        // Implement attack visual effect
        Modulate = new Color(1, 0.5f, 0.5f); // Flash red
        GetTree().CreateTimer(0.2).Timeout += () => Modulate = Colors.White;
    }

    private void PlayBlockEffect()
    {
        // Implement block visual effect
        Modulate = new Color(0.5f, 0.5f, 1); // Flash blue
        GetTree().CreateTimer(0.2).Timeout += () => Modulate = Colors.White;
    }

    private void PlayHealEffect()
    {
        // Implement heal visual effect
        Modulate = new Color(0.5f, 1, 0.5f); // Flash green
        GetTree().CreateTimer(0.2).Timeout += () => Modulate = Colors.White;
    }

    private void PlayHurtEffect()
    {
        // Implement hurt visual effect
        Modulate = new Color(1, 0, 0); // Flash bright red
        GetTree().CreateTimer(0.2).Timeout += () => Modulate = Colors.White;
    }

    private void PlayDeathEffect()
    {
        // Implement death animation
        Modulate = new Color(0.3f, 0.3f, 0.3f); // Darken
    }
}
*/
