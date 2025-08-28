using System;
using System.Linq;
using System.Security.AccessControl;
using Godot;

namespace HexViz
{
    public partial class SceneManager : Node3D
    {
        [Export] public PackedScene HexColumn { get; set; }

        [Export] public uint Rows { get; set; } = 50;
        [Export] public uint Cols { get; set; } = 50;
        [Export] public double GapBetweenHexes { get; set; } = 0.08;

        [Export(PropertyHint.File, "*.txt")] public string TestMapPath { get; set; }

        private HexColumn[,] map_grid;

        public override void _Ready()
        {
            SetupGrid();

            var test_map_file = FileAccess.Open(TestMapPath, FileAccess.ModeFlags.Read);
            var test_map_data = test_map_file.GetAsText().Split("\n");
            var dim = test_map_data[0].Split(",").Select(int.Parse).ToArray();
            var test_map = new bool[dim[0], dim[1]];
            for (var i = 0; i < dim[0]; i++)
                for (var j = 0; j < dim[1]; j++)
                    test_map[i, j] = test_map_data[j + 1][i] == '1';

            RaiseShape(test_map);
        }

        private void RaiseShape(bool[,] grid)
        {
            var x_mul = (double)map_grid.GetLength(0) / grid.GetLength(0);
            var y_mul = (double)map_grid.GetLength(1) / grid.GetLength(1);

            for (var i = 0; i < grid.GetLength(0); i++)
                for (var j = 0; j < grid.GetLength(1); j++)
                {
                    var mi = (int)(i / x_mul);
                    var mj = (int)(j / y_mul);

                    if (grid[i, j])
                    {
                        map_grid[mi, mj].Raise();
                        map_grid[mi, mj].SetColour(Colors.White);
                    }
                    else
                    {
                        map_grid[mi, mj].Lower();
                        map_grid[mi, mj].SetColour(Colors.Black);
                    }
                }
        }


        private void SetupGrid()
        {
            map_grid = new HexColumn[Rows, Cols];
            var radius = ((CylinderMesh)HexColumn.Instantiate<HexColumn>().Mesh).TopRadius + GapBetweenHexes;

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

                    map_grid[j, i] = new_col;
                }
        }
    }
}