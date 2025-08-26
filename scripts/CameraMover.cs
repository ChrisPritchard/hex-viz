
using Godot;
using System;

public partial class CameraMover : Camera3D
{
    [Export] public float MoveSpeed = 5.0f;
    [Export] public float FastMultiplier = 2.5f;

    public override void _Process(double delta)
    {
        Vector3 velocity = Vector3.Zero;
        float deltaFloat = (float)delta;

        // Get camera basis vectors
        Vector3 right = GlobalTransform.Basis.X;
        Vector3 up = GlobalTransform.Basis.Y;

        // Horizontal movement
        if (Input.IsKeyPressed(Key.D) || Input.IsActionPressed("ui_right"))
            velocity += right;
        if (Input.IsKeyPressed(Key.A) || Input.IsActionPressed("ui_left"))
            velocity -= right;
        if (Input.IsKeyPressed(Key.W) || Input.IsActionPressed("ui_down"))
            velocity += up;
        if (Input.IsKeyPressed(Key.S) || Input.IsActionPressed("ui_up"))
            velocity -= up;

        // Apply movement
        if (velocity.Length() > 0)
        {
            velocity = velocity.Normalized();
            float currentSpeed = MoveSpeed * (Input.IsKeyPressed(Key.Shift) ? FastMultiplier : 1.0f);
            GlobalTranslate(velocity * currentSpeed * deltaFloat);
        }
    }
}