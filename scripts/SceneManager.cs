using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace HexViz
{
    public partial class SceneManager : Node3D
    {
        [Export] public uint Rows { get; set; } = 100;
        [Export] public uint Cols { get; set; } = 100;
        [Export] public double GapBetweenHexes { get; set; } = 0.00;

        [Export(PropertyHint.File, "*.txt")] public string WorldMapPath { get; set; }
        [Export(PropertyHint.File, "*.txt")] public string CityMapPath { get; set; }

        [Export] private MultiMeshInstance3D MapGrid;
        [Export] private CylinderMesh TileMesh;
        [Export] private PackedScene TileCollisionArea;

        private MultiMesh multi_mesh;

        private Transform3D[] tile_positions;
        private Area3D[] tile_areas;
        private readonly Dictionary<int, Tween> tile_tweens = [];

        private readonly Dictionary<string, bool[,]> maps = [];

        public override void _Ready()
        {
            SetupGrid();
            LoadMaps();
            RenderGrid(maps["world"]);
        }

        public async void ShowWorld()
        {
            await LowerAll();
            RenderGrid(maps["world"]);
        }
        public async void ShowLondon()
        {
            await LowerAll();
            RenderGrid(maps["london"]);
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

        private void LoadMaps()
        {
            var map_info = new[] { ("world", WorldMapPath), ("london", CityMapPath) };
            foreach (var (name, path) in map_info)
            {
                var map_file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
                var map_data = map_file.GetAsText().Split("\n");

                var dim = map_data[0].Split(",").Select(int.Parse).ToArray();
                var (w, h) = (dim[0], dim[1]);
                var map = new bool[w, h];

                map_data = map_data[1..];

                // each line in the map file is a col, with left to right being bottom to top
                for (var y = 0; y < h; y++)
                    for (var x = 0; x < w; x++)
                        map[x, y] = map_data[y][w - x] == '1';

                maps[name] = map;
            }
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
                        RaiseTile(x, y, ((float)rnd.NextDouble() * 1f) + 1f, Colors.White);
                    else
                        LowerTile(x, y, Colors.Black);
                }
        }

        private void SetupTween(int index, bool rising)
        {
            if (tile_tweens.TryGetValue(index, out Tween tween) && tween.IsRunning())
                tween.Kill();

            tween = CreateTween();
            tween.SetParallel(false);
            tween.SetEase(rising ? Tween.EaseType.Out : Tween.EaseType.In);
            tween.SetTrans(Tween.TransitionType.Quad);
            tile_tweens[index] = tween;
        }

        private bool RaiseTile(uint x, uint y, float height, Color new_colour, float duration_secs = 2f)
        {
            var i = (int)(y * Cols + x);
            return RaiseTile(i, height, new_colour, duration_secs);
        }

        private bool RaiseTile(int index, float height, Color new_colour, float duration_secs = 2f)
        {
            if (multi_mesh.GetInstanceColor(index) == Colors.Green)
                return false;

            if (tile_tweens.TryGetValue(index, out Tween tween))
                tween.Kill();

            SetupTween(index, true);
            tile_tweens[index].SetParallel(true);

            var base_start = tile_positions[index].Origin;
            var target = base_start + Vector3.Up * height;
            var actual_start = multi_mesh.GetInstanceTransform(index).Origin;
            tile_tweens[index].TweenMethod(Callable.From<Vector3>(position =>
            {
                var newTransform = new Transform3D(tile_positions[index].Basis, position);
                multi_mesh.SetInstanceTransform(index, newTransform);
                tile_areas[index].GlobalPosition = position;
            }), actual_start, target, duration_secs);

            tile_tweens[index].TweenMethod(Callable.From<Color>(colour =>
            {
                multi_mesh.SetInstanceColor(index, colour);
            }), multi_mesh.GetInstanceColor(index), new_colour, duration_secs);

            return true;
        }

        private bool LowerTile(uint x, uint y, Color new_colour, float duration_secs = 2f)
        {
            var i = (int)(y * Cols + x);
            return LowerTile(i, new_colour, duration_secs);
        }

        private bool LowerTile(int index, Color new_colour, float duration_secs = 2f)
        {
            if (multi_mesh.GetInstanceColor(index) == Colors.Black)
                return false;

            if (tile_tweens.TryGetValue(index, out Tween tween))
                tween.Kill();

            SetupTween(index, false);
            tile_tweens[index].SetParallel(true);

            var start = multi_mesh.GetInstanceTransform(index).Origin;
            var target = tile_positions[index].Origin;
            tile_tweens[index].TweenMethod(Callable.From<Vector3>(position =>
            {
                var newTransform = new Transform3D(tile_positions[index].Basis, position);
                multi_mesh.SetInstanceTransform(index, newTransform);
                tile_areas[index].GlobalPosition = position;
            }), start, target, duration_secs);

            tile_tweens[index].TweenMethod(Callable.From<Color>(colour =>
            {
                multi_mesh.SetInstanceColor(index, colour);
            }), multi_mesh.GetInstanceColor(index), new_colour, duration_secs);

            return true;
        }

        private async Task LowerAll()
        {
            for (var i = 0; i <= Rows * Cols; i++)
                LowerTile(i, Colors.Black, 2f);

            await Task.Delay(2000);
        }

        private void HandleClick(int index, uint x, uint y, InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
            {
                if (multi_mesh.GetInstanceColor(index) == Colors.Green)
                    LowerTile(x, y, Colors.Yellow);
                else
                    RaiseTile(x, y, 4, Colors.Green);
            }
        }
    }
}