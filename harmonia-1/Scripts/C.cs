/*using System;
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

    // Settings
    [Export]
    public float PlayerDetectionRange = 150.0f;

    [Export]
    public float TurnDelay = 1.0f; // Delay between turns

    private Timer _turnTimer;

    public override void _Ready()
    {
        // Create turn timer
        _turnTimer = new Timer();
        _turnTimer.WaitTime = TurnDelay;
        _turnTimer.OneShot = true;
        _turnTimer.Timeout += OnTurnTimerTimeout;
        AddChild(_turnTimer);
    }

    public void Initialize(Player player)
    {
        _player = player;
        _player.ActionCompleted += OnPlayerActionCompleted;
    }

    public override void _Process(double delta)
    {
        if (!_isCombatActive && _player != null)
        {
            CheckForEnemiesInRange();
        }
    }

    private void CheckForEnemiesInRange()
    {
        // Get all enemies in the scene
        var enemies = GetTree().GetNodesInGroup("enemies").Cast<Enemy>().ToList();
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
        if (_selectedEnemy != null)
        {
            _selectedEnemy.SetSelected(false);
        }

        _selectedEnemy = enemy;
        if (_selectedEnemy != null)
        {
            _selectedEnemy.SetSelected(true);
        }
    }

    public void SelectNextEnemy()
    {
        if (_enemiesInRange.Count == 0)
            return;

        int currentIndex = _enemiesInRange.IndexOf(_selectedEnemy);
        int nextIndex = (currentIndex + 1) % _enemiesInRange.Count;
        SelectEnemy(_enemiesInRange[nextIndex]);
    }

    public void SelectPreviousEnemy()
    {
        if (_enemiesInRange.Count == 0)
            return;

        int currentIndex = _enemiesInRange.IndexOf(_selectedEnemy);
        int prevIndex = (currentIndex - 1 + _enemiesInRange.Count) % _enemiesInRange.Count;
        SelectEnemy(_enemiesInRange[prevIndex]);
    }

    // Called by player when they complete an action
    private void OnPlayerActionCompleted(string actionType)
    {
        if (!_isPlayerTurn)
            return;

        GD.Print($"Player action completed: {actionType}");
        EmitSignal(SignalName.ActionExecuted, actionType, _player, _selectedEnemy);

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

        GD.Print("Enemy turn started");
        EmitSignal(SignalName.TurnChanged, _isPlayerTurn);

        // Execute first enemy action
        ExecuteNextEnemyAction();
    }

    private void ExecuteNextEnemyAction()
    {
        // Check if all enemies have acted
        if (_currentEnemyIndex >= _enemiesInRange.Count)
        {
            // All enemies acted, return to player turn
            _turnTimer.Start();
            return;
        }

        var enemy = _enemiesInRange[_currentEnemyIndex];
        if (enemy.IsAlive)
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
        GD.Print("Player turn started");

        // Remove dead enemies
        _enemiesInRange.RemoveAll(e => !e.IsAlive);

        // Check if combat should end
        if (_enemiesInRange.Count == 0)
        {
            EndCombat(true);
            return;
        }

        // Make sure selected enemy is still valid
        if (_selectedEnemy == null || !_selectedEnemy.IsAlive)
        {
            SelectEnemy(_enemiesInRange[0]);
        }

        EmitSignal(SignalName.TurnChanged, _isPlayerTurn);
    }

    private void EndCombat(bool playerWon)
    {
        _isCombatActive = false;
        _selectedEnemy?.SetSelected(false);
        _selectedEnemy = null;

        GD.Print($"Combat ended. Player won: {playerWon}");
        EmitSignal(SignalName.CombatEnded, playerWon);
    }

    public bool IsPlayerTurn() => _isPlayerTurn;

    public bool IsCombatActive() => _isCombatActive;

    public Enemy GetSelectedEnemy() => _selectedEnemy;

    public List<Enemy> GetEnemiesInRange() => _enemiesInRange;
}
*/
