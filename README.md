# Hex Visualisation

Just something I have been playing around with, an idea I had that was a bit fuzzy in my head, so I needed to flesh it out. Might be interesting to use for a game.

![](./Animation.gif)

Of interest aside from the Godot C# bits, are two python scripts I built with DeepSeek:

- [world_map_gen.py](./world_map_gen.py) which will create a boolean grid of land vs sea data for the world, at specifiable resolutions
- [city_map_gen.py](./city_map_gen.py) which will do the same, but for city outlines. This is less refined - its the 'limits' of the city, and so won't distinctuish out rivers or islands (e.g. for somewhere like New York).
