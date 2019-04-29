using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Cubes : MonoBehaviour {
	public int width;
	public int depth;

	public Cube[] cubePrefabs;


	private Dictionary<Location, Cube> cubes;


	private void Start() {
		Dictionary<Constraints, List<Cube>> cubeDatabase = new Dictionary<Constraints, List<Cube>>();

		foreach(Cube cubePrefab in cubePrefabs) {
			var query =
				from front in new string[][]{ cubePrefab.frontConstraints, new string[]{ "*" } }
				from right in new string[][]{ cubePrefab.rightConstraints, new string[]{ "*" } }
				from back in new string[][]{ cubePrefab.backConstraints, new string[]{ "*" } }
				from left in new string[][]{ cubePrefab.leftConstraints, new string[]{ "*" } }
				select new Constraints(front, right, back, left);

			foreach(Constraints constraints in query) {
				if(!cubeDatabase.ContainsKey(constraints)) {
					cubeDatabase.Add(constraints, new List<Cube>());
				}

				cubeDatabase[constraints].Add(cubePrefab);
			}
		}

		
		List<Location> locations = new List<Location>();

		for(int x = 0; x < width; x += 1) {
			for(int z = 0; z < width; z += 1) {
				locations.Add(new Location(x, z));
			}
		}


		Dictionary<Tuple<int, int, int, int>, string[]> edges = new Dictionary<Tuple<int, int, int, int>, string[]>();

		foreach(Location location in locations) {
			// There will be some overlap but that's okay.
			edges[new Tuple<int, int, int, int>(location.x, location.y - 1, location.x, location.y)] = new string[]{ "*" };
			edges[new Tuple<int, int, int, int>(location.x, location.y, location.x + 1, location.y)] = new string[]{ "*" };
			edges[new Tuple<int, int, int, int>(location.x, location.y, location.x, location.y + 1)] = new string[]{ "*" };
			edges[new Tuple<int, int, int, int>(location.x - 1, location.y, location.x, location.y)] = new string[]{ "*" };
		}


		Dictionary<Location, Tuple<Cube, int>> deployments = new Dictionary<Location, Tuple<Cube, int>>();

		foreach(Location location in locations.ToArray()) {
			string[] front = edges[new Tuple<int, int, int, int>(location.x, location.y - 1, location.x, location.y)];
			string[] right = edges[new Tuple<int, int, int, int>(location.x, location.y, location.x + 1, location.y)];
			string[] back = edges[new Tuple<int, int, int, int>(location.x, location.y, location.x, location.y + 1)];
			string[] left = edges[new Tuple<int, int, int, int>(location.x - 1, location.y, location.x, location.y)];

			Constraints key = new Constraints(front, right, back, left).invert();


			List<Tuple<Cube, int>> candidates = new List<Tuple<Cube, int>>();

			Constraints rotatedKey = key;

			for(int rotation = 0; rotation < 4; rotation += 1) {
				if(cubeDatabase.ContainsKey(rotatedKey)) {
					candidates.AddRange(
						from cubePrefab in cubeDatabase[rotatedKey]
						select new Tuple<Cube, int>(cubePrefab, rotation)
					);
				}

				rotatedKey = rotatedKey.rotateCCW();
			}


			if(candidates.Count > 0) {
				// This should work as we supplied all possible variations of cubes.
				Tuple<Cube, int> candidate = candidates[UnityEngine.Random.Range(0, candidates.Count)];

				deployments.Add(location, candidate);


				Constraints edgeKey = new Constraints(candidate.Item1.frontConstraints, candidate.Item1.rightConstraints, candidate.Item1.backConstraints, candidate.Item1.leftConstraints);

				for(int rotation = 0; rotation < candidate.Item2; rotation += 1) {
					edgeKey = edgeKey.rotateCW();
				}

				edges[new Tuple<int, int, int, int>(location.x, location.y - 1, location.x, location.y)] = edgeKey.front;
				edges[new Tuple<int, int, int, int>(location.x, location.y, location.x + 1, location.y)] = edgeKey.right;
				edges[new Tuple<int, int, int, int>(location.x, location.y, location.x, location.y + 1)] = edgeKey.back;
				edges[new Tuple<int, int, int, int>(location.x - 1, location.y, location.x, location.y)] = edgeKey.left;
			}
			else {
				locations.Remove(location);
			}
		}


		cubes = new Dictionary<Location, Cube>();

		foreach(Location location in locations) {
			Tuple<Cube, int> deployment = deployments[location];

			Vector3 position = new Vector3(location.x - width / 2, 0.0f, location.y - depth / 2);
			Quaternion rotation = Quaternion.AngleAxis(deployment.Item2 * 90, Vector3.up);

			Cube cube = Instantiate(deployment.Item1, position, rotation, transform);

			cubes.Add(location, cube);
		}
	}
}
