using System;
using Godot;

namespace HexViz
{
    public partial class HexColumn : MeshInstance3D
    {
        private Tween tween;

        private float top_height;
        private float bottom_height;
        private float animation_duration = 5.0f;

        public override void _Ready()
        {
            bottom_height = GetAabb().Position.Y;
            top_height = GetAabb().Size.Y + bottom_height;
        }

        private void SetupTween()
        {
            if (tween != null && tween.IsRunning())
                tween.Kill();
            tween = CreateTween();
            tween.SetParallel(false);
            tween.SetEase(Tween.EaseType.Out);
            tween.SetTrans(Tween.TransitionType.Expo);
        }

        public void Raise()
        {
            SetupTween();
            tween.TweenProperty(this, "position:y", top_height, animation_duration);
        }

        public void Drop()
        {
            SetupTween();
            tween.TweenProperty(this, "position:y", bottom_height, animation_duration);
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