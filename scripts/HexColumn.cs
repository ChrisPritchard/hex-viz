using Godot;
using System;

public partial class HexColumn : MeshInstance3D
{
    [Export] private double amplitude = 0.3f;
    [Export] private double frequency = 0.8f;
    [Export] private double phaseOffset = 0.0f;
    [Export] private bool startAutomatically = true;

    private Vector3 originalPosition;
    private double time = 0.0f;
    private bool isFloating = false;

    public override void _Ready()
    {
        originalPosition = GlobalTransform.Origin;

        if (startAutomatically)
        {
            StartFloating();
        }
    }

    public override void _Process(double delta)
    {
        if (!isFloating) return;

        time += delta;
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        // Sine wave calculation with phase offset
        double sineValue = Mathf.Sin((time * frequency) + phaseOffset);
        double newY = originalPosition.Y + (sineValue * amplitude);

        Vector3 newPosition = new Vector3(
            originalPosition.X,
            (float)newY,
            originalPosition.Z
        );

        GlobalTransform = new Transform3D(GlobalTransform.Basis, newPosition);
    }

    // Public methods to control the floating
    public void StartFloating()
    {
        isFloating = true;
        time = 0.0f;
    }

    public void StopFloating()
    {
        isFloating = false;
        // Return to original position
        GlobalTransform = new Transform3D(GlobalTransform.Basis, originalPosition);
    }

    public void SetAmplitude(float newAmplitude)
    {
        amplitude = newAmplitude;
    }

    public void SetFrequency(float newFrequency)
    {
        frequency = newFrequency;
    }

    public void ResetPosition()
    {
        GlobalTransform = new Transform3D(GlobalTransform.Basis, originalPosition);
        time = 0.0f;
    }
}
