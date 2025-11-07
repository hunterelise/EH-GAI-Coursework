using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public sealed class Map
{
	/// <summary>Max possible ally units</summary>
	private const int MaxAllyUnits = 15;

	/// <summary>Max possible enemy units</summary>
	private const int MaxEnemyUnits = 25;

	public enum Terrain
	{
		Water,
		Mud,
		Grass,
		Tree
	}

	/// <summary>Horizontal size of the map</summary>
	public const int MapWidth = 100;

	/// <summary>Vertical size of the map</summary>
	public const int MapHeight = 100;

	/// <summary>Total size of the map</summary>
	public const int MapSize = MapWidth * MapHeight;

	/// <summary>The seed value used to generate the map/terrain</summary>
	public int TerrainSeed { get; private set; } = 0;

	/// <summary>The seed value used to generate the agent positions in the map</summary>
	public int LocationsSeed { get; private set; } = 0;

	/// <summary>
	/// Returns the X coordinate of supplied map index
	/// </summary>
	/// <param name="index">The index of the map element to retrieve. This must be in the inclusive range 0 to (MapSize-1)</param>
	/// <returns>Y coordinate that represents the map index supplied</returns>
	public int MapIndexToX(int index)
	{
		return index % MapWidth;
	}

	/// <summary>
	/// Returns the Y coordinate of supplied map index
	/// </summary>
	/// <param name="index">The index of the map element to retrieve. This must be in the inclusive range 0 to (MapSize-1)</param>
	/// <returns>Y coordinate that represents the map index supplied</returns>
	public int MapIndexToY(int index)
	{
		return index / MapWidth;
	}

	/// <summary>
	/// Returns the map index from the supllied coordinates
	/// </summary>
	/// <param name="x">Horizontal axis coordinate that must be in the inclusive range 0 to (MapWidth-1)</param>
	/// <param name="y">Vertical axis coordinate that must be in the inclusive range 0 to (MapHeight-1)</param>
	/// <returns>Map index of the supplied coordiantes</returns>
	public int MapIndex(int x, int y)
	{
		return x + y * MapWidth;
	}

	/// <summary>
	/// Returns the terrain value of the map at the supplied index
	/// </summary>
	/// <param name="index">The index of the map element to retrieve. This must be in the inclusive range 0 to (MapSize-1)</param>
	/// <returns>Terrain enum value at the supplied index</returns>
	public Terrain GetTerrainAt(int index)
	{
		return (Terrain)mapData[index];
	}

	/// <summary>
	/// Returns the terrain value of the map at the supplied coordinates
	/// </summary>
	/// <param name="x">Horizontal axis coordinate that must be in the inclusive range 0 to (MapWidth-1)</param>
	/// <param name="y">Vertical axis coordinate that must be in the inclusive range 0 to (MapHeight-1)</param>
	/// <returns>Terrain enum value at the supplied coordinates</returns>
	public Terrain GetTerrainAt(int x, int y)
	{
		return (Terrain)mapData[x + y * MapWidth];
	}

	/// <summary>
	/// Returns the raw data value of the map at the supplied coordinates
	/// </summary>
	/// <param name="index">The index of the map element to retrieve. This must be in the inclusive range 0 to (MapSize-1)</param>
	/// <returns>Value at the supplied index which will match a value from the Terrain enum</returns>
	public byte GetMapDataAt(int index)
	{
		return mapData[index];
	}

	/// <summary>
	/// Returns the raw data value of the map at the supplied coordinates
	/// </summary>
	/// <param name="x">Horizontal axis coordinate that must be in the inclusive range 0 to (MapWidth-1)</param>
	/// <param name="y">Vertical axis coordinate that must be in the inclusive range 0 to (MapHeight-1)</param>
	/// <returns>Value at the supplied coordinates which will match a value from the Terrain enum</returns>
	public byte GetMapDataAt(int x, int y)
	{
		return mapData[x + y * MapWidth];
	}

	/// <summary>
	/// Returns a copy of the map data
	/// </summary>
	/// <returns>Copy of the map data</returns>
	public byte[] GetMapData()
	{
		var copy = new byte[MapSize];
		Array.Copy(mapData, copy, mapData.Length);
		return copy;
	}

	/// <summary>
	/// Returns a copy of starting enemy house locations
	/// </summary>
	/// <returns>A copy of starting enemy house locations</returns>
	public List<int> GetInitialEnemyHouseLocations()
	{
		return new List<int>(initialEnemyHouseLocations);
	}

	/// <summary>
	/// Returns a copy of starting enemy locations
	/// </summary>
	/// <returns>A copy of starting enemy locations</returns>
	public List<int> GetInitialEnemyLocations()
	{
		return new List<int>(initialEnemyLocations);
	}

	/// <summary>
	/// Returns a copy of the starting ally locations
	/// </summary>
	/// <returns>A copy of the starting ally locations</returns>
	public List<int> GetInitialAllyLocations()
	{
		return new List<int>(initialAllyLocations);
	}


	/// <summary>
	/// Returns if the terrain is passable at the specified location
	/// </summary>
	/// <param name="mapIndex">single coordinate position in map</param>
	/// <returns>true if terrain is navigatable at specified location, otherwise false</returns>
	public bool IsNavigatable(int mapIndex)
	{
		return mapData[mapIndex] < (byte)Terrain.Tree;
	}

	/// <summary>
	/// Returns if the terrain is passable at the specified location
	/// </summary>
	/// <param name="x">Horizontal position in map</param>
	/// <param name="y">Vertical position in map</param>
	/// <returns>true if terrain is navigatable at specified location, otherwise false</returns>
	public bool IsNavigatable(int x, int y)
	{
		if(x < 0 || x >= MapWidth || y < 0 || y >= MapHeight)
		{
			return false;
		}

		return IsNavigatable(x + y * MapWidth);
	}

	#region private interface

	/// <summary>25 "good" terrain seeds for map</summary>
	private readonly int[] GoodSeeds = new int[]
	{
		414038828, 414234828, 414292109, 414329078, 414372453,
		414408968, 414431718, 414471953, 414531718, 414585343,
		414622921, 414698703, 414746015, 414787531, 414842390,
		415004875, 415155421, 415202375, 415224078, 415383609,
		415407578, 415431671, 415455500, 415507984, 415533187
	};

	private byte[] mapData;

	private readonly float[] terrainHeights = new float[4];

	private List<int> initialEnemyHouseLocations;
	private List<int> initialEnemyLocations;
	private List<int> initialAllyLocations;


	public Map()
	{
		// Suppress unreachable code warnings. Legitimate use case here to provide compile time changability for TestingVariables.TerrainSeed
		// Could have used conditional compilation but didn't want to introduce this to this codebase as it would be extra complexity
#pragma warning disable CS0162
		if (TestingVariables.TerrainSeed >= 0)
		{
			TerrainSeed = TestingVariables.TerrainSeed;
		}
		else
		{
			// This selects one of the premade seeds that was found by trial and error
			var seedSelectorRandom = new System.Random(Math.Abs(Environment.TickCount));
			TerrainSeed = GoodSeeds[seedSelectorRandom.Next(GoodSeeds.Length)];
		}
#pragma warning restore CS0162

		var terrainRandom = new System.Random(TerrainSeed);

		for (int terrainIndex = 0; terrainIndex < terrainHeights.Length; ++terrainIndex)
		{
			terrainHeights[terrainIndex] = (float)terrainRandom.NextDouble();
		}

		SetMinimumWalkAreaProbability(0.7f);

		// Set cumulative hieghts for terrain, therefore last entry equals maxHeight
		for (int terrainIndex = 1; terrainIndex < terrainHeights.Length; ++terrainIndex)
		{
			terrainHeights[terrainIndex] += terrainHeights[terrainIndex - 1];
		}
		float heightTotal = terrainHeights[terrainHeights.Length - 1];

		float[,] octaveStartCoords = new float[,]
		{
			{
				(float)terrainRandom.NextDouble() * 255.0f,
				(float)terrainRandom.NextDouble() * 255.0f,
				(float)terrainRandom.NextDouble() * 255.0f,
				(float)terrainRandom.NextDouble() * 255.0f
			},
			{
				(float)terrainRandom.NextDouble() * 255.0f,
				(float)terrainRandom.NextDouble() * 255.0f,
				(float)terrainRandom.NextDouble() * 255.0f,
				(float)terrainRandom.NextDouble() * 255.0f
			},
		};

		const float stepY = 0.05f;
		const float stepX = 0.05f;
		mapData = new byte[MapSize];
		for (int y = 0; y < MapHeight; ++y)
		{
			for (int x = 0; x < MapWidth; ++x)
			{
				float height = 0.0f;

				float octave = 1.0f;
				for (int octaveIndex = 0; octaveIndex < octaveStartCoords.GetLength(0); ++octaveIndex)
				{
					height += FastNoiseLite.SinglePerlin(TerrainSeed, octaveStartCoords[0, octaveIndex] + stepX * x * octave, octaveStartCoords[1, octaveIndex] + stepY * y * octave) / octave;
					octave *= 2.0f;
				}

				height *= heightTotal;

				// Its possible the height can go over the max height with the addition of octaves. To properly rescale, the terrain heights would have to be set first,
				// then get the largest height and calculate a scaling factor to apply to all heights to get everything rescaled properly. That's a lot of work, take
				// the easy way out and just cap it especially as we don't care about the heights at all, just what terrain type it is
				if (height > heightTotal)
				{
					height = heightTotal;
				}

				for (int terrainIndex = 0; terrainIndex < terrainHeights.Length; ++terrainIndex)
				{
					if (height <= terrainHeights[terrainIndex])
					{
						mapData[x + y * MapWidth] = (byte)terrainIndex;
						break;
					}
				}
			}
		}

		// Suppress unreachable code warnings. Legitimate use case here to provide compile time changability for TestingVariables.LocationsSeed
#pragma warning disable CS0162
		if (TestingVariables.LocationsSeed < 0)
		{
			LocationsSeed = Math.Abs(Environment.TickCount);
		}
		else
		{
			LocationsSeed = TestingVariables.LocationsSeed;
		}
#pragma warning restore CS0162

		var agentsRandom = new System.Random(LocationsSeed);

		// Determine biggest landmass of nodes
		var largestWalkableArea = GetLargestWalkableArea();
		largestWalkableArea = RemoveEdgesOfLargestWalkableArea(largestWalkableArea);

		// Get which nodes in the landmass could be used for enemy house placement but segregate these by a grid
		// to prevent houses overlapping or being too near with each other
		var gridIndexToPossibleEnemyNodes = CreateEnemyPlacementGrids(largestWalkableArea, 5, 5);

		var gridIndexes = gridIndexToPossibleEnemyNodes.Keys.ToList();
		var playerGridIndex = gridIndexes[agentsRandom.Next(gridIndexes.Count)];
		var playersPossibleNodes = gridIndexToPossibleEnemyNodes[playerGridIndex];
		gridIndexToPossibleEnemyNodes.Remove(playerGridIndex);

		var allyHouse = playersPossibleNodes[agentsRandom.Next(playersPossibleNodes.Count)];
		initialAllyLocations = CalculateUnitLocations(agentsRandom, new List<int>() { allyHouse }, MaxAllyUnits, 8 + 16 + 24);

		initialEnemyHouseLocations = CalculateEnemyHouseLocations(agentsRandom, 2, Math.Min(6, gridIndexToPossibleEnemyNodes.Count), gridIndexToPossibleEnemyNodes);
		initialEnemyLocations = CalculateUnitLocations(agentsRandom, initialEnemyHouseLocations, MaxEnemyUnits, 8 + 16 + 24);

		//foreach (var enemy in initialEnemyLocations)
		//{
		//	mapData[enemy] = 4;
		//}
		//foreach (var player in initialPlayerLocations)
		//{
		//	mapData[player] = 5;
		//}
		//mapData[playerHouse] = 6;
		//foreach (var house in initialEnemyHouseLocations)
		//{
		//	mapData[house] = 6;
		//}

		UnityEngine.Debug.Log("Terrain Seed: " + TerrainSeed + ", Location Seed: " + LocationsSeed);
	}

	private List<int> CalculateUnitLocations(System.Random random, List<int> unitHouseNodes, int maxUnits, int areaSize)
	{
		var unitsPerHouse = maxUnits / unitHouseNodes.Count;

		var unitLocations = new List<int>();

		foreach (var house in unitHouseNodes)
		{
			var possibleEnemyPlacementsSet = FloodFill(house, areaSize); // This should be a different algorithm - concentric squares?
			possibleEnemyPlacementsSet.Remove(house);

			var possibleEnemyPlacements = possibleEnemyPlacementsSet.ToList();

			for (int enemyIndex = 0; enemyIndex < unitsPerHouse; ++enemyIndex)
			{
				var randomEnemyIndex = random.Next(possibleEnemyPlacements.Count);
				var enemy = possibleEnemyPlacements[randomEnemyIndex];
				possibleEnemyPlacements.RemoveAt(randomEnemyIndex);

				unitLocations.Add(enemy);
			}
		}
		return unitLocations;
	}

	private List<int> CalculateEnemyHouseLocations(System.Random random, int minHouses, int maxHouses, Dictionary<int, List<int>> gridIndexToPossibleEnemyNodes)
	{
		var enemyGridsCount = random.Next(minHouses, maxHouses);
		var enemyGridsToConsider = gridIndexToPossibleEnemyNodes.Keys.ToList();
		while (enemyGridsToConsider.Count > enemyGridsCount)
		{
			enemyGridsToConsider.RemoveAt(random.Next(enemyGridsToConsider.Count));
		}

		var enemyHouseNodes = new List<int>(enemyGridsCount);
		foreach (var gridIndex in enemyGridsToConsider)
		{
			var enemyNodes = gridIndexToPossibleEnemyNodes[gridIndex];

			while (true)
			{
				var isHouseNodeValid = true;
				var randomEnemyNodeIndex = random.Next(enemyNodes.Count);
				var houseNode = enemyNodes[randomEnemyNodeIndex];

				var x = houseNode % MapWidth;
				var y = houseNode / MapWidth;

				for (int mapY = y - 1; mapY <= y + 1; ++mapY)
				{
					for (int mapX = x - 1; mapX <= x + 1; ++mapX)
					{
						if (mapX == x && mapY == y)
						{
							continue;
						}

						var terrainType = (Terrain)mapData[mapX + mapY * MapWidth];
						if (terrainType == Terrain.Water || terrainType == Terrain.Tree)
						{
							isHouseNodeValid = false;
							break;
						}
					}

					if (isHouseNodeValid == false)
					{
						break;
					}
				}

				if (isHouseNodeValid)
				{
					enemyHouseNodes.Add(houseNode);
					enemyNodes.RemoveAt(randomEnemyNodeIndex);
					break;
				}
			}
		}
		return enemyHouseNodes;
	}

	private HashSet<int> GetLargestWalkableArea()
	{
		var largestWalkableArea = new HashSet<int>();
		var floodFillTests = new bool[mapData.Length];
		for (int mapIndex = 0; mapIndex < mapData.Length; ++mapIndex)
		{
			if (floodFillTests[mapIndex] || IsNavigatable(mapIndex) == false)
			{
				continue;
			}

			var area = FloodFill(mapIndex);
			foreach (var areaIndex in area)
			{
				floodFillTests[areaIndex] = true;
			}

			if (largestWalkableArea.Count == 0 || largestWalkableArea.Count < area.Count)
			{
				largestWalkableArea = area;
			}
		}

		//foreach (var mapIndex in largestWalkableArea)
		//{
		//	mapData[mapIndex] = 5;
		//}

		return largestWalkableArea;
	}


	private HashSet<int> RemoveEdgesOfLargestWalkableArea(HashSet<int> largestWalkableArea)
	{
		var largestWalkableAreaCopy = new HashSet<int>(largestWalkableArea);
		foreach (var areaIndex in largestWalkableAreaCopy)
		{
			var x = areaIndex % MapWidth;
			var y = areaIndex / MapWidth;

			if (x <= 0 || x >= (MapWidth - 1) || y <= 0 || y >= (MapHeight - 1))
			{
				largestWalkableArea.Remove(areaIndex);
				continue;
			}


			bool removed = false;

			for (int mapY = y - 1; mapY <= y + 1; ++mapY)
			{
				for (int mapX = x - 1; mapX <= x + 1; ++mapX)
				{
					if (mapX == x && mapY == y)
					{
						continue;
					}

					if (mapX < 0 || mapX >= MapWidth || mapY < 0 || mapY >= MapHeight)
					{
						continue;
					}

					var terrainType = (Terrain)mapData[mapX + mapY * MapWidth];
					if (terrainType == Terrain.Water || terrainType == Terrain.Tree)
					{
						largestWalkableArea.Remove(areaIndex);
						removed = true;
						break;
					}
				}

				if (removed)
				{
					break;
				}
			}
		}

		//foreach (var mapIndex in largestWalkableArea)
		//{
		//	mapData[mapIndex] = 5;
		//}

		return largestWalkableArea;
	}

	private Dictionary<int, List<int>> CreateEnemyPlacementGrids(HashSet<int> validNodes, int gridSizeX, int gridSizeY)
	{
		var gridIndexToNodes = new Dictionary<int, List<int>>();

		foreach (var node in validNodes)
		{
			var x = node % MapWidth;
			var y = node / MapWidth;
			var gridX = x / gridSizeX;
			var gridY = y / gridSizeY;
			var gridIndex = gridX + gridY * gridSizeX;

			// Don't include the outside nodes of a grid in placement as it separates houses from each other, therefore
			// cannot have touching houses and all houses will be at least 2 nodes apart
			if (((x % gridSizeX) == 0) || ((x % gridSizeX) == gridSizeX - 1) || ((y % gridSizeY) == 0) || ((y % gridSizeY) == gridSizeY - 1))
			{
				continue;
			}

			if (gridIndexToNodes.ContainsKey(gridIndex))
			{
				gridIndexToNodes[gridIndex].Add(node);
			}
			else
			{
				gridIndexToNodes[gridIndex] = new List<int>() { node };
			}
		}
		return gridIndexToNodes;
	}

	private void SetMinimumWalkAreaProbability(float walkAreaAmount)
	{
		if (walkAreaAmount < 0.0f || walkAreaAmount > 1.0f)
		{
			throw new ArgumentOutOfRangeException("walkAreaAmount must be between 0-1");
		}

		float maxHeight = 0.0f;
		foreach (var height in terrainHeights)
		{
			maxHeight += height;
		}

		var water = terrainHeights[(int)Terrain.Water];
		var mud = terrainHeights[(int)Terrain.Mud];
		var grass = terrainHeights[(int)Terrain.Grass];

		// Ensures that at least walkAreaAmount of spaces are grass and mud
		if ((mud + grass + water) / maxHeight < walkAreaAmount)
		{
			var walkableScaleFactor = walkAreaAmount / ((water + mud + grass) / maxHeight);
			terrainHeights[(int)Terrain.Water] *= walkableScaleFactor;
			terrainHeights[(int)Terrain.Mud] *= walkableScaleFactor;
			terrainHeights[(int)Terrain.Grass] *= walkableScaleFactor;

			var nonWalkableScaleFactor = (1.0f - walkAreaAmount) / (terrainHeights[(int)Terrain.Tree] / maxHeight);
			terrainHeights[(int)Terrain.Tree] *= nonWalkableScaleFactor;
		}
	}


	private HashSet<int> FloodFill(int startIndex, int iterations = int.MaxValue)
	{
		// Used to return the results
		var closedList = new HashSet<int>();

		// Quick way to track what nodes are on the open list without having to iterate through it while keeping memory to a minimum
		var onOpenList = new HashSet<int>();
		var openList = new Queue<int>();

		openList.Enqueue(startIndex);

		var isStartWalkable = IsNavigatable(startIndex);


		while (openList.Count > 0 && iterations > 0)
		{
			--iterations;

			var currentIndex = openList.Dequeue();
			closedList.Add(currentIndex);

			var x = currentIndex % MapWidth;
			var y = currentIndex / MapWidth;

			var left = x - 1;
			var right = x + 1;
			var bottom = y - 1;
			var top = y + 1;

			if (left >= 0)
			{
				left = left + y * MapWidth;
				if (IsNavigatable(left) == isStartWalkable && onOpenList.Contains(left) == false && !closedList.Contains(left))
				{
					openList.Enqueue(left);
					onOpenList.Add(left);
				}
			}
			if (right < MapWidth)
			{
				right = right + y * MapWidth;
				if (IsNavigatable(right) == isStartWalkable && onOpenList.Contains(right) == false && !closedList.Contains(right))
				{
					openList.Enqueue(right);
					onOpenList.Add(right);
				}
			}
			if (bottom >= 0)
			{
				bottom = x + bottom * MapWidth;
				if (IsNavigatable(bottom) == isStartWalkable && onOpenList.Contains(bottom) == false && !closedList.Contains(bottom))
				{
					openList.Enqueue(bottom);
					onOpenList.Add(bottom);
				}
			}
			if (top < MapHeight)
			{
				top = x + top * MapWidth;
				if (IsNavigatable(top) == isStartWalkable && onOpenList.Contains(top) == false && !closedList.Contains(top))
				{
					openList.Enqueue(top);
					onOpenList.Add(top);
				}
			}
		}

		return closedList;
	}
#endregion
}