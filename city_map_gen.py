import sys
import numpy as np
import matplotlib.pyplot as plt
from matplotlib.path import Path
import osmnx as ox
from shapely.geometry import Polygon

# Set OSMnx settings
ox.settings.log_console = True
ox.settings.use_cache = True

def get_city_boundary(city_name):
    """
    Get the boundary of a specific city using OSMnx.
    """
    try:
        # Get the city boundary
        city = ox.geocode_to_gdf(city_name)
        geometry = city.geometry.iloc[0]
        
        # Handle different geometry types
        if geometry.geom_type == 'Polygon':
            return geometry
        elif geometry.geom_type == 'MultiPolygon':
            # Use the largest polygon in the multipolygon
            largest_poly = max(geometry.geoms, key=lambda p: p.area)
            return largest_poly
        else:
            raise ValueError(f"Unsupported geometry type: {geometry.geom_type}")
            
    except Exception as e:
        print(f"Error getting boundary for {city_name}: {e}")
        return None

def generate_city_map(city_name, width=100, height=100):
    """
    Generate a boolean array representing a specific city.
    """
    
    # Get the city boundary
    city_polygon = get_city_boundary(city_name)
    
    if city_polygon is None:
        raise ValueError(f"Could not find boundary for '{city_name}'")
    
    # Get the bounding box of the city
    min_lon, min_lat, max_lon, max_lat = city_polygon.bounds
    
    # Add some padding to the bounding box
    padding = 0.01  # degrees
    min_lon -= padding
    max_lon += padding
    min_lat -= padding
    max_lat += padding
    
    # Create a grid of points within the bounding box
    lons = np.linspace(min_lon, max_lon, width)
    lats = np.linspace(min_lat, max_lat, height)
    lon_grid, lat_grid = np.meshgrid(lons, lats)
    
    # Initialize the boolean array
    is_city = np.full((width, height), False, dtype=bool)
    
    # Convert city polygon to a path
    if hasattr(city_polygon, 'exterior'):
        city_path = Path(np.array(city_polygon.exterior.coords))
    else:
        # For MultiPolygon, use the largest polygon
        if city_polygon.geom_type == 'MultiPolygon':
            largest_poly = max(city_polygon.geoms, key=lambda p: p.area)
            city_path = Path(np.array(largest_poly.exterior.coords))
        else:
            raise ValueError(f"Unsupported geometry type: {city_polygon.geom_type}")
    
    # Check each point in the grid
    points = np.column_stack([lon_grid.ravel(), lat_grid.ravel()])
    inside = city_path.contains_points(points)
    inside = inside.reshape(width, height)
    
    # Update the city array
    is_city = np.logical_or(is_city, inside)
    
    return is_city, (min_lon, max_lon, min_lat, max_lat)

def save_map_to_file(is_city, filename='city_map.txt'):
    """Save the boolean array to a text file."""
    with open(filename, 'w') as f:
        f.write(f"{is_city.shape[0]},{is_city.shape[1]}\n")
        for y in range(is_city.shape[1]):
            for x in range(is_city.shape[0]):
                f.write('1' if is_city[x, y] else '0')
            f.write('\n')

def visualize_map(is_city, bbox=None, city_name=""):
    """Visualize the generated city map."""
    plt.figure(figsize=(10, 10))
    
    if bbox:
        extent = [bbox[0], bbox[1], bbox[2], bbox[3]]
        plt.imshow(is_city.T, cmap='binary', origin='lower', extent=extent)
    else:
        plt.imshow(is_city.T, cmap='binary', origin='lower')
    
    plt.title(f'{city_name} Map (City = True, Non-city = False)')
    plt.xlabel('Longitude')
    plt.ylabel('Latitude')
    plt.colorbar(label='City (True) / Non-city (False)')
    plt.show()

if __name__ == "__main__":
    # Default city and dimensions
    city_name = "New York, USA"
    dim = 100
    
    # Parse command line arguments
    if len(sys.argv) >= 2:
        city_name = sys.argv[1]
    if len(sys.argv) >= 3:
        dim = int(sys.argv[2])
    
    print(f"Generating map for {city_name} with dim {dim}...")
    
    try:
        city_map, bbox = generate_city_map(city_name, dim, dim)
        filename = f"{city_name.replace(' ', '_').replace(',', '').lower()}_map_{dim}x{dim}.txt"
        save_map_to_file(city_map, filename)
        
        visualize_map(city_map, bbox, city_name)
        
        print(f"Map generation complete! Saved to {filename}")
    except Exception as e:
        print(f"Error generating map: {e}")
        print("Make sure you've installed osmnx: pip install osmnx")