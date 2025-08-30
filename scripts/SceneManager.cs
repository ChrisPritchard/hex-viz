using System;
using System.Linq;
using Godot;

namespace HexViz
{
    public partial class SceneManager : Node3D
    {
        [Export] public PackedScene HexColumn { get; set; }

        [Export] public uint Rows { get; set; } = 100;
        [Export] public uint Cols { get; set; } = 100;
        [Export] public double GapBetweenHexes { get; set; } = 0.00;

        [Export(PropertyHint.File, "*.txt")] public string TestMapPath { get; set; }

        private HexColumn[,] map_grid;

        public override void _Ready()
        {
            SetupGrid();

            var test_map_file = FileAccess.Open(TestMapPath, FileAccess.ModeFlags.Read);
            var test_map_data = test_map_file.GetAsText().Split("\n");

            var dim = test_map_data[0].Split(",").Select(int.Parse).ToArray();
            var (w, h) = (dim[0], dim[1]);
            var test_map = new bool[w, h];

            test_map_data = test_map_data[1..];

            // each line in the map file is a col, with left to right being bottom to top
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                    test_map[x, y] = test_map_data[y][w - x] == '1';

            RaiseShape(test_map);
        }

        private void RaiseShape(bool[,] grid)
        {
            var x_scale = (double)Rows / grid.GetLength(0);
            var y_scale = (double)Cols / grid.GetLength(1);
            var rnd = new Random();

            for (var x = 0; x < Rows; x++)
                for (var y = 0; y < Cols; y++)
                {
                    var mx = (int)(x / x_scale);
                    var my = (int)(y / y_scale);

                    if (grid[mx, my])
                    {
                        map_grid[x, y].Raise(rnd.NextDouble() * 5);
                        map_grid[x, y].SetColour(Colors.White);
                    }
                    else
                    {
                        map_grid[x, y].Lower(rnd.NextDouble() * 5);
                        map_grid[x, y].SetColour(Colors.Black);
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