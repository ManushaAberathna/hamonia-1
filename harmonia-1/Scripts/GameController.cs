using System;
using Godot;

public partial class GameController : Node
{
    // References to major systems
    private CombatManager _combatManager;
    private PitchDetector _pitchDetector;
    private NoteDisplayUI _noteDisplayUI;
    private Player _player;

    // UI Elements
    private Label _turnIndicator;
    private Label _instructionLabel;

    public override void _Ready()
    {
        // Get or create systems
        _combatManager = GetNodeOrNull<CombatManager>("CombatManager");
        if (_combatManager == null)
        {
            _combatManager = new CombatManager();
            _combatManager.Name = "CombatManager";
            AddChild(_combatManager);
        }

        _pitchDetector = GetNodeOrNull<PitchDetector>("PitchDetector");
        if (_pitchDetector == null)
        {
            _pitchDetector = new PitchDetector();
            _pitchDetector.Name = "PitchDetector";
            AddChild(_pitchDetector);
        }

        _noteDisplayUI = GetNodeOrNull<NoteDisplayUI>("NoteDisplayUI");
        if (_noteDisplayUI == null)
        {
            _noteDisplayUI = new NoteDisplayUI();
            _noteDisplayUI.Name = "NoteDisplayUI";
            AddChild(_noteDisplayUI);
        }

        // Get player
        _player = GetTree().Root.GetNode<Player>("Main/Player"); // Adjust ("Player") to your actual path
        if (_player == null)
        {
            GD.PrintErr("Player not found! Make sure there's a Player node in the scene.");
            return;
        }

        // Initialize combat manager with player
        _combatManager.Initialize(_player);

        // Connect signals
        ConnectSignals();

        // Setup UI
        SetupUI();

        // Start pitch detection
        _pitchDetector.StartDetection();

        GD.Print("Game Controller initialized!");
    }

    private void ConnectSignals()
    {
        // Combat Manager signals
        _combatManager.TurnChanged += OnTurnChanged;
        _combatManager.CombatEnded += OnCombatEnded;
        _combatManager.ActionExecuted += OnActionExecuted;

        // Pitch Detector signals
        _pitchDetector.NoteSung += OnNoteSung;

        // Player signals
        _player.HealthChanged += OnPlayerHealthChanged;
        _player.PlayerDead += OnPlayerDead;
    }

    private void SetupUI()
    {
        // Create turn indicator
        _turnIndicator = new Label();
        _turnIndicator.Name = "TurnIndicator";
        _turnIndicator.Text = "YOUR TURN";
        _turnIndicator.Position = new Vector2(20, 20);
        _turnIndicator.AddThemeFontSizeOverride("font_size", 24);
        _turnIndicator.AddThemeColorOverride("font_color", new Color(0.2f, 0.8f, 0.2f));
        AddChild(_turnIndicator);

        // Create instruction label
        _instructionLabel = new Label();
        _instructionLabel.Name = "InstructionLabel";
        _instructionLabel.Text = "Sing a note to attack!";
        _instructionLabel.Position = new Vector2(20, 60);
        _instructionLabel.AddThemeFontSizeOverride("font_size", 16);
        _instructionLabel.AddThemeColorOverride("font_color", Colors.White);
        AddChild(_instructionLabel);
    }

    public override void _Process(double delta)
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (!_combatManager.IsCombatActive())
            return;

        // Enemy selection with Q/E keys (or Tab)
        if (Input.IsActionJustPressed("select_next_enemy"))
        {
            _combatManager.SelectNextEnemy();
        }

        if (Input.IsActionJustPressed("select_previous_enemy"))
        {
            _combatManager.SelectPreviousEnemy();
        }

        // Manual action triggers (for testing without singing)
        if (Input.IsActionJustPressed("manual_attack"))
        {
            if (_combatManager.IsPlayerTurn())
            {
                _player.PerformAction("attack");
            }
        }

        if (Input.IsActionJustPressed("manual_block"))
        {
            if (_combatManager.IsPlayerTurn())
            {
                _player.PerformAction("block");
            }
        }

        if (Input.IsActionJustPressed("manual_heal"))
        {
            if (_combatManager.IsPlayerTurn())
            {
                _player.PerformAction("heal");
            }
        }
    }

    // Signal handlers
    private void OnTurnChanged(bool isPlayerTurn)
    {
        if (isPlayerTurn)
        {
            _turnIndicator.Text = "YOUR TURN";
            _turnIndicator.AddThemeColorOverride("font_color", new Color(0.2f, 0.8f, 0.2f));
            _instructionLabel.Text = "Sing a note to attack!";

            // Reset player block state on new turn
            _player.ResetBlockState();
        }
        else
        {
            _turnIndicator.Text = "ENEMY TURN";
            _turnIndicator.AddThemeColorOverride("font_color", new Color(0.9f, 0.2f, 0.2f));
            _instructionLabel.Text = "Enemy is attacking...";
        }
    }

    private void OnCombatEnded(bool playerWon)
    {
        if (playerWon)
        {
            _instructionLabel.Text = "Victory! All enemies defeated!";
            _instructionLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.8f, 0.2f));
        }
        else
        {
            _instructionLabel.Text = "Defeat...";
            _instructionLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.2f, 0.2f));
        }

        _turnIndicator.Visible = false;
    }

    private void OnActionExecuted(string actionType, Node2D actor, Node2D target)
    {
        GD.Print($"Action: {actionType} from {actor.Name} to {target?.Name ?? "none"}");
    }

    private void OnNoteSung(string note, float confidence, float frequency)
    {
        // Display note in UI
        _noteDisplayUI.DisplayNote(note, confidence, frequency);

        // If it's player's turn and combat is active, perform action
        if (_combatManager.IsPlayerTurn() && _combatManager.IsCombatActive())
        {
            // Check if note matches target enemy's required note (optional feature)
            var selectedEnemy = _combatManager.GetSelectedEnemy();
            bool isCorrectNote =
                selectedEnemy != null
                && note.ToUpper() == selectedEnemy.GetRequiredNote().ToUpper();

            if (confidence >= _pitchDetector.MinConfidence)
            {
                _noteDisplayUI.ShowSuccess(true);
                _player.OnNoteSung(note, confidence);
            }
            else
            {
                _noteDisplayUI.ShowSuccess(false);
                GD.Print("Note confidence too low!");
            }
        }
    }

    private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
    {
        GD.Print($"Player Health: {currentHealth}/{maxHealth}");
    }

    private void OnPlayerDead()
    {
        _instructionLabel.Text = "Game Over - You Died!";
        _instructionLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.2f, 0.2f));
        _turnIndicator.Visible = false;

        // Stop pitch detection
        _pitchDetector.StopDetection();
    }

    public override void _ExitTree()
    {
        // Clean up
        if (_pitchDetector != null && _pitchDetector.IsDetecting())
        {
            _pitchDetector.StopDetection();
        }
    }
}
