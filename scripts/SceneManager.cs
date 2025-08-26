using System;
using System.Diagnostics;
using Godot;

public partial class SceneManager : Node3D
{
    [Export] public PackedScene HexColumn { get; set; }

    [Export] public uint Rows { get; set; } = 10;
    [Export] public uint Cols { get; set; } = 10;

    public override void _Ready()
    {
        var gap = 0.05;
        var radius = ((CylinderMesh)HexColumn.Instantiate<HexColumn>().Mesh).TopRadius + gap;


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
            }
    }
}
