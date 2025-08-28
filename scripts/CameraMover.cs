
using Godot;

namespace HexViz
{
    public partial class CameraMover : Camera3D
    {
        [Export] public float MoveSpeed = 5.0f;
        [Export] public float FastMultiplier = 2.5f;

        public override void _Process(double delta)
        {
            var velocity = Vector3.Zero;
            var deltaFloat = (float)delta;

            var right = GlobalTransform.Basis.X;
            var up = GlobalTransform.Basis.Y;

            if (Input.IsKeyPressed(Key.D) || Input.IsActionPressed("ui_right"))
                velocity += right;
            if (Input.IsKeyPressed(Key.A) || Input.IsActionPressed("ui_left"))
                velocity -= right;
            if (Input.IsKeyPressed(Key.W) || Input.IsActionPressed("ui_down"))
                velocity += up;
            if (Input.IsKeyPressed(Key.S) || Input.IsActionPressed("ui_up"))
                velocity -= up;

            if (velocity.Length() > 0)
            {
                velocity = velocity.Normalized();
                float currentSpeed = MoveSpeed * (Input.IsKeyPressed(Key.Shift) ? FastMultiplier : 1.0f);
                GlobalTranslate(velocity * currentSpeed * deltaFloat);
            }
        }
    }
}