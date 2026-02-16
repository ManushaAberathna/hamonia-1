using System;
using System.Collections.Generic;
using Godot;

// Helper class to generate random note sequences for enemies
public partial class EnemySpawner : Node
{
    private static readonly string[] AllNotes = { "C", "D", "E", "F", "G", "A", "B" };
    private Random _random = new Random();

    // Generate a random note sequence based on enemy type
    public string[] GenerateNoteSequence(Enemy.EnemyType type)
    {
        int sequenceLength = type switch
        {
            Enemy.EnemyType.Goblin => 2,
            Enemy.EnemyType.Orc => 3,
            Enemy.EnemyType.Dragon => 4,
            _ => 2
        };

        return GenerateRandomSequence(sequenceLength);
    }

    // Generate a random sequence of specified length
    public string[] GenerateRandomSequence(int length)
    {
        string[] sequence = new string[length];
        
        for (int i = 0; i < length; i++)
        {
            sequence[i] = AllNotes[_random.Next(AllNotes.Length)];
        }

        return sequence;
    }

    // Generate a sequence ensuring no consecutive repeats
    public string[] GenerateNonRepeatingSequence(int length)
    {
        string[] sequence = new string[length];
        string lastNote = "";

        for (int i = 0; i < length; i++)
        {
            string note;
            do
            {
                note = AllNotes[_random.Next(AllNotes.Length)];
            } while (note == lastNote && AllNotes.Length > 1);

            sequence[i] = note;
            lastNote = note;
        }

        return sequence;
    }

    // Pre-defined sequences for consistency
    public static class PresetSequences
    {
        // Goblin variants
        public static readonly string[] Goblin1 = { "C", "D" };
        public static readonly string[] Goblin2 = { "C", "E" };
        public static readonly string[] Goblin3 = { "D", "F" };
        public static readonly string[] Goblin4 = { "E", "G" };
        public static readonly string[] Goblin5 = { "F", "A" };

        // Orc variants
        public static readonly string[] Orc1 = { "C", "D", "E" };
        public static readonly string[] Orc2 = { "C", "E", "G" };
        public static readonly string[] Orc3 = { "D", "F", "A" };
        public static readonly string[] Orc4 = { "E", "G", "B" };
        public static readonly string[] Orc5 = { "F", "A", "C" };

        // Dragon variants
        public static readonly string[] Dragon1 = { "C", "E", "G", "C" };
        public static readonly string[] Dragon2 = { "D", "F", "A", "D" };
        public static readonly string[] Dragon3 = { "E", "G", "B", "E" };
        public static readonly string[] Dragon4 = { "C", "D", "E", "F" };
        public static readonly string[] Dragon5 = { "G", "F", "E", "D" };

        // Get all variants for a type
        public static string[][] GetVariants(Enemy.EnemyType type)
        {
            return type switch
            {
                Enemy.EnemyType.Goblin => new[] { Goblin1, Goblin2, Goblin3, Goblin4, Goblin5 },
                Enemy.EnemyType.Orc => new[] { Orc1, Orc2, Orc3, Orc4, Orc5 },
                Enemy.EnemyType.Dragon => new[] { Dragon1, Dragon2, Dragon3, Dragon4, Dragon5 },
                _ => new[] { Goblin1 }
            };
        }
    }

    // Get a random preset sequence for an enemy type
    public string[] GetRandomPresetSequence(Enemy.EnemyType type)
    {
        var variants = PresetSequences.GetVariants(type);
        return variants[_random.Next(variants.Length)];
    }

    // Create an enemy with a specific sequence
    public Enemy CreateEnemy(Enemy.EnemyType type, Vector2 position, string[] customSequence = null)
    {
        var enemy = new Enemy();
        enemy.Type = type;
        enemy.Position = position;

        if (customSequence != null)
        {
            enemy.RequiredNoteSequence = customSequence;
        }
        else
        {
            enemy.RequiredNoteSequence = GetRandomPresetSequence(type);
        }

        // Set stats based on type
        switch (type)
        {
            case Enemy.EnemyType.Goblin:
                enemy.MaxHealth = 30;
                enemy.AttackDamage = 10;
                break;
            case Enemy.EnemyType.Orc:
                enemy.MaxHealth = 60;
                enemy.AttackDamage = 20;
                break;
            case Enemy.EnemyType.Dragon:
                enemy.MaxHealth = 100;
                enemy.AttackDamage = 30;
                break;
        }

        return enemy;
    }

    // Spawn a random enemy
    public Enemy SpawnRandomEnemy(Vector2 position)
    {
        var types = Enum.GetValues<Enemy.EnemyType>();
        var randomType = types[_random.Next(types.Length)];
        return CreateEnemy(randomType, position);
    }

    // Print all preset sequences (for documentation)
    public void PrintAllPresets()
    {
        GD.Print("=== GOBLIN VARIANTS ===");
        for (int i = 0; i < 5; i++)
        {
            var variants = PresetSequences.GetVariants(Enemy.EnemyType.Goblin);
            GD.Print($"Goblin{i + 1}: [{string.Join(", ", variants[i])}]");
        }

        GD.Print("\n=== ORC VARIANTS ===");
        for (int i = 0; i < 5; i++)
        {
            var variants = PresetSequences.GetVariants(Enemy.EnemyType.Orc);
            GD.Print($"Orc{i + 1}: [{string.Join(", ", variants[i])}]");
        }

        GD.Print("\n=== DRAGON VARIANTS ===");
        for (int i = 0; i < 5; i++)
        {
            var variants = PresetSequences.GetVariants(Enemy.EnemyType.Dragon);
            GD.Print($"Dragon{i + 1}: [{string.Join(", ", variants[i])}]");
        }
    }
}
