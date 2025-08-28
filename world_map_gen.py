import numpy as np
import cartopy.feature as cfeature
import cartopy.crs as ccrs
import matplotlib.pyplot as plt
from matplotlib.path import Path
from matplotlib.transforms import Bbox

# requirements: pip install cartopy matplotlib numpy pillow

def generate_real_world_map(width=50, height=50, projection=ccrs.PlateCarree()):
    """
    Generate a boolean array representing the real world map with land and water areas.
    
    Parameters:
    - width: Width of the grid
    - height: Height of the grid
    - projection: Cartopy projection to use
    
    Returns:
    - A 2D boolean array where True represents land and False represents water
    """
    
    # Create a figure and axis with the specified projection
    fig = plt.figure(figsize=(width/10, height/10), dpi=100)
    ax = plt.axes(projection=projection)
    
    # Set the extent to cover the whole world
    ax.set_global()
    
    # Add land features
    land = cfeature.NaturalEarthFeature('physical', 'land', '110m', 
                                      edgecolor='none', facecolor='black')
    ax.add_feature(land)
    
    # Get the extent of the plot in data coordinates
    extent = ax.get_extent(crs=ccrs.PlateCarree())
    
    # Create a grid of points
    lons = np.linspace(extent[0], extent[1], width)
    lats = np.linspace(extent[2], extent[3], height)
    lon_grid, lat_grid = np.meshgrid(lons, lats)
    
    # Initialize the boolean array
    is_land = np.full((width, height), False, dtype=bool)
    
    # Get the land geometry
    land_geoms = land.geometries()
    
    # Create a path for each land geometry and check if points are inside
    for geom in land_geoms:
        # Convert geometry to path
        paths = geom_to_path(geom)
        
        # Check each point in the grid
        for path in paths:
            # Transform points to the projection of the geometry
            points = projection.transform_points(ccrs.PlateCarree(), lon_grid, lat_grid)
            points_2d = points[:, :, :2].reshape(-1, 2)
            
            # Check which points are inside the path
            inside = path.contains_points(points_2d)
            inside = inside.reshape(width, height)
            
            # Update the land array
            is_land = np.logical_or(is_land, inside)
    
    plt.close(fig)
    return is_land

def geom_to_path(geom):
    """Convert a geometry to a matplotlib Path object."""
    if geom.geom_type == 'Polygon':
        return [Path(np.array(geom.exterior.coords))]
    elif geom.geom_type == 'MultiPolygon':
        paths = []
        for polygon in geom.geoms:
            paths.append(Path(np.array(polygon.exterior.coords)))
        return paths
    return []

def save_map_to_file(is_land, filename='world_map.txt'):
    """Save the boolean array to a text file."""
    with open(filename, 'w') as f:
        f.write(f"{is_land.shape[0]},{is_land.shape[1]}\n")
        for y in range(is_land.shape[1]):
            for x in range(is_land.shape[0]):
                f.write('1' if is_land[x, y] else '0')
            f.write('\n')

def load_map_from_file(filename='world_map.txt'):
    """Load a boolean array from a text file."""
    with open(filename, 'r') as f:
        dimensions = f.readline().strip().split(',')
        width, height = int(dimensions[0]), int(dimensions[1])
        
        is_land = np.full((width, height), False, dtype=bool)
        
        for y in range(height):
            line = f.readline().strip()
            for x in range(width):
                is_land[x, y] = (line[x] == '1')
    
    return is_land

def visualize_map(is_land):
    """Visualize the generated map."""
    plt.figure(figsize=(10, 10))
    plt.imshow(is_land.T, cmap='binary', origin='lower')
    plt.title('World Map (Land = True, Water = False)')
    plt.xlabel('X Coordinate')
    plt.ylabel('Y Coordinate')
    plt.colorbar(label='Land (True) / Water (False)')
    plt.show()

# Example usage
if __name__ == "__main__":
    # Generate the world map (this might take a while)
    print("Generating world map...")
    world_map = generate_real_world_map(50, 50)
    
    # Save to file for later use
    save_map_to_file(world_map, 'world_map_50x50.txt')
    
    # Visualize the result
    visualize_map(world_map)
    
    # To load the map later:
    # loaded_map = load_map_from_file('world_map_50x50.txt')
    
    print("Map generation complete!")