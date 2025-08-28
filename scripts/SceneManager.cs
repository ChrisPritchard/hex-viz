using System;
using Godot;

namespace HexViz
{
    public partial class SceneManager : Node3D
    {
        [Export] public PackedScene HexColumn { get; set; }

        [Export] public uint Rows { get; set; } = 50;
        [Export] public uint Cols { get; set; } = 50;
        [Export] public double GapBetweenHexes { get; set; } = 0.08;

        public override void _Ready()
        {
            var radius = ((CylinderMesh)HexColumn.Instantiate<HexColumn>().Mesh).TopRadius + GapBetweenHexes;

            var rnd = new Random();

            // calculations on hex offsets taken from here: https://www.redblobgames.com/grids/hexagons/
            for (var i = 0; i < Cols; i++)
                for (var j = 0; j < Rows; j++)
                {
                    var offset_y = j * radius * 1.5;
                    var offset_x = i * radius * Math.Sqrt(3);
                    if (j % 2 == 1)
                        offset_x += radius * Math.Sqrt(3) / 2;

                    var new_col = HexColumn.Instantiate<HexColumn>();
                    new_col.Translate(new Vector3((float)offset_x, 0, (float)offset_y));
                    AddChild(new_col);

                    if (rnd.NextDouble() > 0.9)
                    {
                        new_col.SetColour(Colors.Red);
                        new_col.Raise();
                    }
                }
        }
    }
}