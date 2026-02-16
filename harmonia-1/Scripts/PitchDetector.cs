using System;
using System.Collections.Generic;
using Godot;

public partial class PitchDetector : Node
{
    // Signals
    [Signal]
    public delegate void NoteSungEventHandler(string note, float confidence, float frequency);

    [Signal]
    public delegate void PitchDetectedEventHandler(float frequency, float confidence);

    /*
    // Audio
    private AudioStreamPlayer _audioPlayer;
    */
    private AudioStreamPlayer _microphonePlayer;
    private AudioEffectCapture _capture;
    private int _captureIndex = -1;

    // Detection settings
    [Export]
    public float MinConfidence = 0.7f; // 70% confidence threshold

    [Export]
    public float MinVolume = 0.01f; // Minimum volume to trigger detection

    [Export]
    public int SampleRate = 44100;

    [Export]
    public int BufferSize = 2048;

    // Note detection
    private const float A4_FREQUENCY = 440.0f;
    private readonly string[] _noteNames =
    {
        "C",
        "C#",
        "D",
        "D#",
        "E",
        "F",
        "F#",
        "G",
        "G#",
        "A",
        "A#",
        "B",
    };

    // State
    private bool _isDetecting = false;
    private float _lastDetectedFrequency = 0.0f;
    private float _detectionCooldown = 0.0f;

    [Export]
    public float CooldownTime = 0.5f; // Time between detections

    public override void _Ready()
    {
        SetupAudioCapture();
    }

    private void SetupAudioCapture()
    {
        // Get or create audio bus for recording
        int busIdx = AudioServer.GetBusIndex("Record");

        if (busIdx == -1)
        {
            // Create Record bus if it doesn't exist
            AudioServer.AddBus();
            busIdx = AudioServer.BusCount - 1;
            AudioServer.SetBusName(busIdx, "Record");
        }

        // Add AudioEffectCapture to the bus
        AudioEffectCapture capture = null;
        for (int i = 0; i < AudioServer.GetBusEffectCount(busIdx); i++)
        {
            var effect = AudioServer.GetBusEffect(busIdx, i);
            if (effect is AudioEffectCapture)
            {
                capture = (AudioEffectCapture)effect;
                _captureIndex = i;
                break;
            }
        }
        /*
        if (capture == null)
        {
            capture = new AudioEffectCapture();
            capture.BufferLength = 0.1f; // 100ms buffer
            AudioServer.AddBusEffect(busIdx, capture);
            _captureIndex = AudioServer.GetBusEffectCount(busIdx) - 1;
        }

        _capture = capture;

        // Setup microphone input
        var idx = AudioServer.GetBusIndex("Record");
        AudioServer.SetBusEnableEffects(idx, true);

        GD.Print("Audio capture setup complete");
        */
        if (capture == null)
        {
            capture = new AudioEffectCapture();
            capture.BufferLength = 0.1f;
            AudioServer.AddBusEffect(busIdx, capture);
        }

        _capture = capture;
        GD.Print("Audio capture setup complete");
    }

    public override void _Process(double delta)
    {
        if (_detectionCooldown > 0)
        {
            _detectionCooldown -= (float)delta;
        }

        if (_isDetecting && _detectionCooldown <= 0)
        {
            DetectPitch();
        }
    }

    public void StartDetection()
    {
        _isDetecting = true;
        /*
        // Start microphone
        if (AudioServer.GetInputDeviceList().Length > 0)
        {
            AudioServer.CaptureStart();
            GD.Print("Microphone detection started");
        }
        else
        {
            GD.PrintErr("No microphone detected!");
        }*/
        // Create microphone stream player if it doesn't exist
        if (_microphonePlayer == null)
        {
            _microphonePlayer = new AudioStreamPlayer();
            AddChild(_microphonePlayer);
            _microphonePlayer.Stream = new AudioStreamMicrophone();
            _microphonePlayer.Bus = "Record";
        }

        _microphonePlayer.Playing = true;
        GD.Print("Microphone detection started");
    }

    public void StopDetection()
    {
        _isDetecting = false;
        /*
        AudioServer.CaptureStop();
        GD.Print("Microphone detection stopped");
        */
        if (_microphonePlayer != null)
        {
            _microphonePlayer.Playing = false;
        }
        GD.Print("Microphone detection stopped");
    }

    private void DetectPitch()
    {
        if (_capture == null || !_capture.CanGetBuffer(BufferSize))
            return;

        // Get audio buffer
        var buffer = _capture.GetBuffer(BufferSize);
        if (buffer.Length == 0)
            return;

        // Calculate volume (RMS)
        float rms = CalculateRMS(buffer);

        if (rms < MinVolume)
            return; // Too quiet

        // Detect frequency using autocorrelation
        float frequency = AutocorrelationDetect(buffer, SampleRate);

        if (frequency > 0)
        {
            float confidence = CalculateConfidence(buffer, frequency);

            if (confidence >= MinConfidence)
            {
                _lastDetectedFrequency = frequency;

                // Convert frequency to note
                string note = FrequencyToNote(frequency, out int octave);

                GD.Print(
                    $"Detected: {note}{octave} ({frequency:F2} Hz) - Confidence: {confidence:F2}"
                );

                EmitSignal(SignalName.PitchDetected, frequency, confidence);
                EmitSignal(SignalName.NoteSung, note, confidence, frequency);

                // Set cooldown
                _detectionCooldown = CooldownTime;
            }
        }
    }

    private float CalculateRMS(Vector2[] buffer)
    {
        float sum = 0;
        foreach (var sample in buffer)
        {
            float mono = (sample.X + sample.Y) / 2;
            sum += mono * mono;
        }
        return Mathf.Sqrt(sum / buffer.Length);
    }

    private float AutocorrelationDetect(Vector2[] buffer, int sampleRate)
    {
        int bufferSize = buffer.Length;
        float[] mono = new float[bufferSize];

        // Convert stereo to mono
        for (int i = 0; i < bufferSize; i++)
        {
            mono[i] = (buffer[i].X + buffer[i].Y) / 2;
        }

        // Autocorrelation
        int minLag = sampleRate / 1000; // 1000 Hz max
        int maxLag = sampleRate / 80; // 80 Hz min

        float maxCorrelation = 0;
        int bestLag = 0;

        for (int lag = minLag; lag < maxLag && lag < bufferSize / 2; lag++)
        {
            float correlation = 0;
            for (int i = 0; i < bufferSize - lag; i++)
            {
                correlation += mono[i] * mono[i + lag];
            }

            if (correlation > maxCorrelation)
            {
                maxCorrelation = correlation;
                bestLag = lag;
            }
        }

        if (bestLag == 0)
            return 0;

        return (float)sampleRate / bestLag;
    }

    private float CalculateConfidence(Vector2[] buffer, float frequency)
    {
        // Simple confidence based on signal clarity
        float rms = CalculateRMS(buffer);

        // Normalize confidence (this is simplified)
        return Mathf.Clamp(rms * 10, 0, 1);
    }

    private string FrequencyToNote(float frequency, out int octave)
    {
        // Calculate semitones from A4 (440 Hz)
        float semitones = 12 * Mathf.Log(frequency / A4_FREQUENCY) / Mathf.Log(2);
        int noteIndex = Mathf.RoundToInt(semitones) % 12;

        // Handle negative note indices
        if (noteIndex < 0)
            noteIndex += 12;

        // Calculate octave
        octave = 4 + Mathf.FloorToInt((semitones + 9) / 12);

        return _noteNames[noteIndex];
    }

    public bool IsDetecting()
    {
        return _isDetecting;
    }
}
