using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Godot;

namespace HexViz
{
    public partial class SceneManager : Node3D
    {
        [Export] public uint Rows { get; set; } = 100;
        [Export] public uint Cols { get; set; } = 100;
        [Export] public double GapBetweenHexes { get; set; } = 0.00;

        [Export(PropertyHint.File, "*.txt")] public string TestMapPath { get; set; }

        [Export] private MultiMeshInstance3D MapGrid;
        [Export] private CylinderMesh TileMesh;
        [Export] private PackedScene TileCollisionArea;

        private MultiMesh multi_mesh;

        private Transform3D[] tile_positions;
        private Area3D[] tile_areas;
        private readonly HashSet<int> rising = [];
        private readonly HashSet<int> lowering = [];

        private readonly (float, float)[] tile_speeds = [(0.9f, 0.01f), (0.95f, 0.005f), (1f, 0.001f)];

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
            multi_mesh = new MultiMesh
            {
                UseColors = true,
                UseCustomData = true,
                TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                Mesh = TileMesh,
                InstanceCount = (int)(Rows * Cols)

            };
            MapGrid.Multimesh = multi_mesh;

            var radius = TileMesh.TopRadius + GapBetweenHexes;
            var orig_positions = new List<Transform3D>((int)(Rows * Cols));
            var orig_areas = new List<Area3D>((int)(Rows * Cols));

            // calculations on hex offsets taken from here: https://www.redblobgames.com/grids/hexagons/
            var i = 0;
            for (var y = 0u; y < Cols; y++)
                for (var x = 0u; x < Rows; x++)
                {
                    var offset_y = x * radius * 1.5;
                    var offset_x = y * radius * Math.Sqrt(3);
                    if (x % 2 == 1)
                        offset_x += radius * Math.Sqrt(3) / 2;

                    var transform = new Transform3D(Basis.Identity, new((float)offset_x, 0, (float)offset_y));
                    orig_positions.Add(transform);
                    multi_mesh.SetInstanceTransform(i, transform);

                    var collision_area = TileCollisionArea.Instantiate<Area3D>();
                    collision_area.Translate(transform.Origin);
                    var (li, lx, ly) = (i, x, y); // to ensure captured correctly in lambda
                    collision_area.InputEvent += (_, e, _, _, _) => HandleClick(li, lx, ly, e);
                    orig_areas.Add(collision_area);
                    MapGrid.AddChild(collision_area);

                    i++;
                }

            tile_positions = [.. orig_positions];
            tile_areas = [.. orig_areas];
        }

        private void RenderGrid(bool[,] grid)
        {
            var x_scale = (double)Rows / grid.GetLength(0);
            var y_scale = (double)Cols / grid.GetLength(1);

            var rnd = new Random();

            for (var x = 0u; x < Rows; x++)
                for (var y = 0u; y < Cols; y++)
                {
                    var mx = (int)(x / x_scale);
                    var my = (int)(y / y_scale);

                    var i = (int)(y * Cols + x);
                    if (grid[mx, my])
                    {
                        RaiseTile(x, y, ((float)rnd.NextDouble() * 1f) + 1f);
                        multi_mesh.SetInstanceColor(i, Colors.White);
                    }
                    else
                    {
                        LowerTile(x, y);
                        multi_mesh.SetInstanceColor(i, Colors.Black);
                    }
                }
        }

        private void RaiseTile(uint x, uint y, float height, float duration_secs = 2f)
        {
            var i = (int)(y * Cols + x);
            lowering.Remove(i);
            if (rising.Add(i))
                multi_mesh.SetInstanceCustomData(i,
                new TileAnimData(Time.GetTicksMsec(), duration_secs * 1000, height).AsCustomData());
        }

        private void LowerTile(uint x, uint y, float duration_secs = 2f)
        {
            var i = (int)(y * Cols + x);
            rising.Remove(i);
            if (lowering.Add(i))
                multi_mesh.SetInstanceCustomData(i,
                new TileAnimData(Time.GetTicksMsec(), duration_secs * 1000, 0).AsCustomData());
        }

        private void HandleClick(int index, uint x, uint y, InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                if (multi_mesh.GetInstanceColor(index) != Colors.Green)
                {
                    RaiseTile(x, y, 4);
                    multi_mesh.SetInstanceColor(index, Colors.Green);
                }
                else
                {
                    LowerTile(x, y);
                    multi_mesh.SetInstanceColor(index, Colors.Yellow);
                }
            }
        }

        public override void _Process(double delta)
        {
            var rising_indices = rising.ToArray();
            foreach (var i in rising_indices)
            {
                var trans = multi_mesh.GetInstanceTransform(i);
                var data = TileAnimData.FromCustomData(multi_mesh.GetInstanceCustomData(i));

                if (trans.Origin.Y >= tile_positions[i].Origin.Y + data.TargetHeight)
                    rising.Remove(i);
                else
                {
                    var progress = data.Progress(Time.GetTicksMsec());
                    var speed = tile_speeds.First(a => progress <= a.Item1).Item2 / (data.Duration / 1000f);
                    var new_pos = trans.Translated(new(0, speed, 0));
                    tile_areas[i].Transform = new_pos;
                    multi_mesh.SetInstanceTransform(i, new_pos);
                }
            }

            var lowering_indices = lowering.ToArray();
            foreach (var i in lowering_indices)
            {
                var trans = multi_mesh.GetInstanceTransform(i);
                var data = TileAnimData.FromCustomData(multi_mesh.GetInstanceCustomData(i));

                if (trans.Origin.Y <= tile_positions[i].Origin.Y)
                    lowering.Remove(i);
                else
                {
                    // var progress = 1f - data.Progress(Time.GetTicksMsec());
                    // var speed = tile_speeds.Reverse().First(a => progress >= a.Item1).Item2;
                    var new_pos = trans.Translated(new(0, -0.01f, 0));
                    tile_areas[i].Transform = new_pos;
                    multi_mesh.SetInstanceTransform(i, new_pos);
                }
            }
        }
    }
}