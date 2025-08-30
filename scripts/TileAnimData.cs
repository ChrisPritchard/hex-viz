
using Godot;

namespace HexViz
{
    public struct TileAnimData(float startTime, float duration, float targetHeight)
    {
        public float StartTime { get; set; } = startTime;
        public float Duration { get; set; } = duration;
        public float TargetHeight { get; set; } = targetHeight;

        public readonly float Progress(float currentTime) => Mathf.Clamp((currentTime - StartTime) / Duration, 0.0f, 1.0f);

        public readonly Color AsCustomData() => new(StartTime, Duration, TargetHeight);

        public static TileAnimData FromCustomData(Color customData) => new()
        {
            StartTime = customData.R,
            Duration = customData.G,
            TargetHeight = customData.B
        };
    }
}