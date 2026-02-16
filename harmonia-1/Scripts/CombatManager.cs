using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class CombatManager : Node
{
    // Signals
    [Signal]
    public delegate void TurnChangedEventHandler(bool isPlayerTurn);

    [Signal]
    public delegate void CombatEndedEventHandler(bool playerWon);

    [Signal]
    public delegate void ActionExecutedEventHandler(string actionType, Node2D actor, Node2D target);

    // References
    private Player _player;
    private List<Enemy> _enemiesInRange = new List<Enemy>();
    private Enemy _selectedEnemy;

    // Combat State
    private bool _isPlayerTurn = true;
    private bool _isCombatActive = false;
    private int _currentEnemyIndex = 0;
    private bool _playerActionCompletedThisTurn = false; // Prevent multiple actions per turn

    // Settings
    [Export]
    public float PlayerDetectionRange = 150.0f;

    [Export]
    public float TurnDelay = 1.0f; // Delay between turns

    private Timer _turnTimer;

    public override void _Ready()
    {
        // Add to group so Player can find us easily
        AddToGroup("combat_manager");

        // Create turn timer
        _turnTimer = new Timer();
        _turnTimer.WaitTime = TurnDelay;
        _turnTimer.OneShot = true;
        _turnTimer.Timeout += OnTurnTimerTimeout;
        AddChild(_turnTimer);

        GD.Print("CombatManager ready and added to 'combat_manager' group");
    }

    public void Initialize(Player player)
    {
        _player = player;
        _player.ActionCompleted += OnPlayerActionCompleted;
        GD.Print("CombatManager initialized with Player");
    }

    public override void _Process(double delta)
    {
        if (!_isCombatActive && _player != null)
        {
            CheckForEnemiesInRange();
        }

        // Clean up dead/disposed enemies from the list
        if (_isCombatActive)
        {
            CleanupDeadEnemies();
        }
    }

    private void CleanupDeadEnemies()
    {
        // Remove enemies that are dead, queued for deletion, or invalid
        _enemiesInRange.RemoveAll(e =>
            e == null || !GodotObject.IsInstanceValid(e) || !e.IsAlive || e.IsQueuedForDeletion()
        );

        // If selected enemy is gone, select next valid one
        if (
            _selectedEnemy != null
            && (
                !GodotObject.IsInstanceValid(_selectedEnemy)
                || !_selectedEnemy.IsAlive
                || _selectedEnemy.IsQueuedForDeletion()
            )
        )
        {
            _selectedEnemy = null;

            if (_enemiesInRange.Count > 0 && _isCombatActive)
            {
                SelectEnemy(_enemiesInRange[0]);
            }
        }
    }

    private void CheckForEnemiesInRange()
    {
        // Get all enemies in the scene
        var enemies = GetTree()
            .GetNodesInGroup("enemies")
            .Cast<Enemy>()
            .Where(e => GodotObject.IsInstanceValid(e) && !e.IsQueuedForDeletion())
            .ToList();

        _enemiesInRange.Clear();

        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive)
            {
                float distance = _player.GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if (distance <= PlayerDetectionRange)
                {
                    _enemiesInRange.Add(enemy);

                    // Start combat if not already active
                    if (!_isCombatActive)
                    {
                        StartCombat();
                    }
                }
            }
        }

        // End combat if no enemies in range
        if (_enemiesInRange.Count == 0 && _isCombatActive)
        {
            EndCombat(true);
        }
    }

    private void StartCombat()
    {
        _isCombatActive = true;
        _isPlayerTurn = true;
        _currentEnemyIndex = 0;

        GD.Print("Combat started!");
        EmitSignal(SignalName.TurnChanged, _isPlayerTurn);

        // Auto-select first enemy
        if (_enemiesInRange.Count > 0)
        {
            SelectEnemy(_enemiesInRange[0]);
        }
    }

    public void SelectEnemy(Enemy enemy)
    {
        // Validate enemy before selecting
        if (enemy == null || !GodotObject.IsInstanceValid(enemy) || !enemy.IsAlive)
        {
            GD.Print("Cannot select invalid or dead enemy");
            return;
        }

        // Deselect current enemy (with null check)
        if (_selectedEnemy != null && GodotObject.IsInstanceValid(_selectedEnemy))
        {
            _selectedEnemy.SetSelected(false);
        }

        _selectedEnemy = enemy;
        if (_selectedEnemy != null)
        {
            _selectedEnemy.SetSelected(true);
            GD.Print($"Selected enemy: {_selectedEnemy.Name}");
        }
    }

    public void SelectNextEnemy()
    {
        if (_enemiesInRange.Count == 0)
            return;

        // Find current index
        int currentIndex = -1;
        if (_selectedEnemy != null && GodotObject.IsInstanceValid(_selectedEnemy))
        {
            currentIndex = _enemiesInRange.IndexOf(_selectedEnemy);
        }

        // Select next valid enemy
        int nextIndex = (currentIndex + 1) % _enemiesInRange.Count;

        // Make sure we select a valid enemy
        int attempts = 0;
        while (attempts < _enemiesInRange.Count)
        {
            var nextEnemy = _enemiesInRange[nextIndex];
            if (nextEnemy != null && GodotObject.IsInstanceValid(nextEnemy) && nextEnemy.IsAlive)
            {
                SelectEnemy(nextEnemy);
                return;
            }
            nextIndex = (nextIndex + 1) % _enemiesInRange.Count;
            attempts++;
        }

        GD.Print("No valid enemy to select");
    }

    public void SelectPreviousEnemy()
    {
        if (_enemiesInRange.Count == 0)
            return;

        // Find current index
        int currentIndex = -1;
        if (_selectedEnemy != null && GodotObject.IsInstanceValid(_selectedEnemy))
        {
            currentIndex = _enemiesInRange.IndexOf(_selectedEnemy);
        }

        // Select previous valid enemy
        int prevIndex = (currentIndex - 1 + _enemiesInRange.Count) % _enemiesInRange.Count;

        // Make sure we select a valid enemy
        int attempts = 0;
        while (attempts < _enemiesInRange.Count)
        {
            var prevEnemy = _enemiesInRange[prevIndex];
            if (prevEnemy != null && GodotObject.IsInstanceValid(prevEnemy) && prevEnemy.IsAlive)
            {
                SelectEnemy(prevEnemy);
                return;
            }
            prevIndex = (prevIndex - 1 + _enemiesInRange.Count) % _enemiesInRange.Count;
            attempts++;
        }

        GD.Print("No valid enemy to select");
    }

    // Called by player when they complete an action
    private void OnPlayerActionCompleted(string actionType)
    {
        if (!_isPlayerTurn || _playerActionCompletedThisTurn)
            return; // Ignore if not player turn or action already performed this turn

        _playerActionCompletedThisTurn = true; // Mark action as completed
        GD.Print($"Player action completed: {actionType}");

        // Only emit signal if enemy is still valid
        if (_selectedEnemy != null && GodotObject.IsInstanceValid(_selectedEnemy))
        {
            EmitSignal(SignalName.ActionExecuted, actionType, _player, _selectedEnemy);
        }
        else
        {
            EmitSignal(SignalName.ActionExecuted, actionType, _player, (Node2D)null);
        }

        // Switch to enemy turn after a delay
        _turnTimer.Start();
    }

    private void OnTurnTimerTimeout()
    {
        if (_isPlayerTurn)
        {
            // Switch to enemy turn
            StartEnemyTurn();
        }
        else
        {
            // Switch back to player turn
            StartPlayerTurn();
        }
    }

    private void StartEnemyTurn()
    {
        _isPlayerTurn = false;
        _currentEnemyIndex = 0;

        // Clean up dead enemies before enemy turn
        CleanupDeadEnemies();

        GD.Print("Enemy turn started");
        EmitSignal(SignalName.TurnChanged, _isPlayerTurn);

        // Execute first enemy action
        ExecuteNextEnemyAction();
    }

    private void ExecuteNextEnemyAction()
    {
        // Clean up before each action
        CleanupDeadEnemies();

        // Check if all enemies have acted
        if (_currentEnemyIndex >= _enemiesInRange.Count)
        {
            // All enemies acted, return to player turn
            _turnTimer.Start();
            return;
        }

        var enemy = _enemiesInRange[_currentEnemyIndex];
        if (enemy != null && GodotObject.IsInstanceValid(enemy) && enemy.IsAlive)
        {
            // Enemy attacks player
            enemy.PerformAttack(_player);
            EmitSignal(SignalName.ActionExecuted, "attack", enemy, _player);
        }

        _currentEnemyIndex++;

        // Schedule next enemy action
        GetTree().CreateTimer(0.5).Timeout += ExecuteNextEnemyAction;
    }

    private void StartPlayerTurn()
    {
        _isPlayerTurn = true;
        _playerActionCompletedThisTurn = false; // Reset action flag for new turn
        GD.Print("Player turn started");

        // Remove dead enemies
        CleanupDeadEnemies();

        // Check if combat should end
        if (_enemiesInRange.Count == 0)
        {
            EndCombat(true);
            return;
        }

        // Make sure selected enemy is still valid
        if (
            _selectedEnemy == null
            || !GodotObject.IsInstanceValid(_selectedEnemy)
            || !_selectedEnemy.IsAlive
        )
        {
            SelectEnemy(_enemiesInRange[0]);
        }

        EmitSignal(SignalName.TurnChanged, _isPlayerTurn);
    }

    private void EndCombat(bool playerWon)
    {
        _isCombatActive = false;

        // Safely deselect enemy
        if (_selectedEnemy != null && GodotObject.IsInstanceValid(_selectedEnemy))
        {
            _selectedEnemy.SetSelected(false);
        }
        _selectedEnemy = null;

        GD.Print($"Combat ended. Player won: {playerWon}");
        EmitSignal(SignalName.CombatEnded, playerWon);
    }

    public bool IsPlayerTurn() => _isPlayerTurn;

    public bool IsCombatActive() => _isCombatActive;

    public Enemy GetSelectedEnemy() => _selectedEnemy;

    public List<Enemy> GetEnemiesInRange() => _enemiesInRange;

    public bool HasPlayerActionCompleted() => _playerActionCompletedThisTurn;
}
