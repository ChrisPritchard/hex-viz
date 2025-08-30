using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        [Export] private MultiMeshInstance3D MapGrid;
        [Export] private CylinderMesh TileMesh;

        private Transform3D[] tile_positions;
        private readonly HashSet<int> rising = [];
        private readonly HashSet<int> lowering = [];

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

            RenderGrid(test_map);
        }

        private void SetupGrid()
        {
            var multimesh = new MultiMesh
            {
                UseColors = true,
                TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                Mesh = TileMesh,
                InstanceCount = (int)(Rows * Cols)

            };
            MapGrid.Multimesh = multimesh;

            var radius = TileMesh.TopRadius + GapBetweenHexes;
            var orig_positions = new List<Transform3D>((int)(Rows * Cols));

            // calculations on hex offsets taken from here: https://www.redblobgames.com/grids/hexagons/
            var i = 0;
            for (var y = 0; y < Cols; y++)
                for (var x = 0; x < Rows; x++)
                {
                    var offset_y = x * radius * 1.5;
                    var offset_x = y * radius * Math.Sqrt(3);
                    if (x % 2 == 1)
                        offset_x += radius * Math.Sqrt(3) / 2;

                    var transform = new Transform3D(Basis.Identity, new((float)offset_x, 0, (float)offset_y));
                    orig_positions.Add(transform);
                    multimesh.SetInstanceTransform(i, transform);
                    i++;
                }

            tile_positions = [.. orig_positions];
        }

        private void RenderGrid(bool[,] grid)
        {
            var x_scale = (double)Rows / grid.GetLength(0);
            var y_scale = (double)Cols / grid.GetLength(1);

            for (var x = 0u; x < Rows; x++)
                for (var y = 0u; y < Cols; y++)
                {
                    var mx = (int)(x / x_scale);
                    var my = (int)(y / y_scale);

                    var i = (int)(y * Cols + x);
                    var op = tile_positions[i];
                    if (grid[mx, my])
                    {
                        RaiseTile(x, y);
                        MapGrid.Multimesh.SetInstanceColor(i, Colors.White);
                    }
                    else
                    {
                        LowerTile(x, y);
                        MapGrid.Multimesh.SetInstanceColor(i, Colors.Black);
                    }
                }
        }

        private void RaiseTile(uint x, uint y)
        {
            var i = (int)(y * Cols + x);
            lowering.Remove(i);
            rising.Add(i);
        }

        private void LowerTile(uint x, uint y)
        {
            var i = (int)(y * Cols + x);
            rising.Remove(i);
            lowering.Add(i);
        }

        public override void _Process(double delta)
        {
            foreach (var i in rising)
            {
                var trans = MapGrid.Multimesh.GetInstanceTransform(i);
                if (trans.Origin.Y >= tile_positions[i].Origin.Y + 2)
                    rising.Remove(i);
                else
                    MapGrid.Multimesh.SetInstanceTransform(i, trans.Translated(new(0, 0.01f, 0)));
            }

            foreach (var i in lowering)
            {
                var trans = MapGrid.Multimesh.GetInstanceTransform(i);
                if (trans.Origin.Y <= tile_positions[i].Origin.Y)
                    lowering.Remove(i);
                else
                    MapGrid.Multimesh.SetInstanceTransform(i, trans.Translated(new(0, -0.01f, 0)));
            }
        }

    }
}