using System;
using Godot;

namespace HexViz
{
    public partial class HexColumn : MeshInstance3D
    {
        private Tween tween;

        private float top_height;
        private float bottom_height;
        private float animation_duration = 2.0f;

        [Export] private Area3D CollisionBox;

        public bool Raised { get; private set; }

        public override void _Ready()
        {
            bottom_height = GlobalTransform.Origin.Y;
            top_height = GetAabb().Size.Y * 0.5f + bottom_height;

            CollisionBox.InputEvent += HandleClick;
        }

        private void HandleClick(Node camera, InputEvent @event, Vector3 eventPosition, Vector3 normal, long shapeIdx)
        {
            if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                if (!Raised)
                {
                    Raise();
                    SetColour(Colors.Green);
                }
                else
                {
                    Lower();
                    SetColour(Colors.Yellow);
                }
            }
        }


        private void SetupTween()
        {
            if (tween != null && tween.IsRunning())
                tween.Kill();
            tween = CreateTween();
            tween.SetParallel(false);
            tween.SetEase(Raised ? Tween.EaseType.In : Tween.EaseType.Out);
            tween.SetTrans(Tween.TransitionType.Quad);
        }

        public void Raise()
        {
            SetupTween();
            tween.TweenProperty(this, "position:y", top_height, animation_duration);
            Raised = true;
        }

        public void Lower()
        {
            SetupTween();
            tween.TweenProperty(this, "position:y", bottom_height, animation_duration);
            Raised = false;
        }

        internal void SetColour(Color colour)
        {
            var newMat = new StandardMaterial3D
            {
                AlbedoColor = colour
            };

            SetSurfaceOverrideMaterial(0, newMat);
        }
    }
}