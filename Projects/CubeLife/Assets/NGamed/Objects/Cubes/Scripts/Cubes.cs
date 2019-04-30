using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

using Edge = System.Tuple<int,int,int,int>;
using Candidate = System.Tuple<Cube, int>;


public class Cubes : MonoBehaviour {
	public int width;
	public int depth;

	public Cube[] cubePrefabs;

	public int borderWidth;

	public string innerBorder;
	public string outerBorder;
	
	public string wildcard;


	private Dictionary<Location, Cube> cubes;

	private MeshCollider surface;


	private void Start() {
		string[] innerBorderConstraint = new string[]{ innerBorder };
		string[] outerBorderConstraint = new string[]{ outerBorder };
		string[] innerOuterBorderConstraint = new string[]{ innerBorder, outerBorder };
		string[] outerInnerBorderConstraint = new string[]{ outerBorder, innerBorder };

		string[] wildcardConstraint = new string[]{ wildcard };

		Dictionary<Constraints, List<Cube>> cubeDatabase = new Dictionary<Constraints, List<Cube>>();

		foreach(Cube cubePrefab in cubePrefabs) {
			var query =
				from front in new string[][]{ cubePrefab.frontConstraints, wildcardConstraint }
				from right in new string[][]{ cubePrefab.rightConstraints, wildcardConstraint }
				from back in new string[][]{ cubePrefab.backConstraints, wildcardConstraint }
				from left in new string[][]{ cubePrefab.leftConstraints, wildcardConstraint }
				select new Constraints(front, right, back, left);

			foreach(Constraints constraints in query) {
				if(!cubeDatabase.ContainsKey(constraints)) {
					cubeDatabase.Add(constraints, new List<Cube>());
				}

				cubeDatabase[constraints].Add(cubePrefab);
			}
		}

		
		List<Location> locations = new List<Location>();

		// Center on the origin.
		int halfWidth = width / 2;
		int halfDepth = depth / 2;
		
		for(int z = -halfDepth - borderWidth; z < depth - halfDepth + borderWidth; z += 1) {
			for(int x = -halfWidth - borderWidth; x < width - halfWidth + borderWidth; x += 1) {
				locations.Add(new Location(x, z));
			}
		}


		Dictionary<Edge, string[]> edges = new Dictionary<Edge, string[]>();

		foreach(Location location in locations) {
			// There will be some overlap but that's okay.
			edges[new Edge(location.x, location.y - 1, location.x, location.y)] = wildcardConstraint;
			edges[new Edge(location.x, location.y, location.x + 1, location.y)] = wildcardConstraint;
			edges[new Edge(location.x, location.y, location.x, location.y + 1)] = wildcardConstraint;
			edges[new Edge(location.x - 1, location.y, location.x, location.y)] = wildcardConstraint;
		}


		// Border.
		foreach(Location location in locations) {
			// There will be some overlap but it should all work out.
			if(location.x < -halfWidth - 1 || location.x > width - halfWidth || location.y < -halfDepth - 1 || location.y > depth - halfDepth) {
				edges[new Edge(location.x, location.y - 1, location.x, location.y)] = outerBorderConstraint;
				edges[new Edge(location.x, location.y, location.x + 1, location.y)] = outerBorderConstraint;
				edges[new Edge(location.x, location.y, location.x, location.y + 1)] = outerBorderConstraint;
				edges[new Edge(location.x - 1, location.y, location.x, location.y)] = outerBorderConstraint;
			}
			else if(location.x == -halfWidth - 1 && location.y == -halfDepth - 1) {
				edges[new Edge(location.x, location.y - 1, location.x, location.y)] = outerBorderConstraint;
				edges[new Edge(location.x, location.y, location.x + 1, location.y)] = outerInnerBorderConstraint;
				edges[new Edge(location.x, location.y, location.x, location.y + 1)] = innerOuterBorderConstraint;
				edges[new Edge(location.x - 1, location.y, location.x, location.y)] = outerBorderConstraint;
			}
			else if(location.x == -halfWidth - 1 && location.y == depth - halfDepth) {
				edges[new Edge(location.x, location.y - 1, location.x, location.y)] = outerInnerBorderConstraint;
				edges[new Edge(location.x, location.y, location.x + 1, location.y)] = innerOuterBorderConstraint;
				edges[new Edge(location.x, location.y, location.x, location.y + 1)] = outerBorderConstraint;
				edges[new Edge(location.x - 1, location.y, location.x, location.y)] = outerBorderConstraint;
			}
			else if(location.x == width - halfWidth && location.y == -halfDepth - 1) {
				edges[new Edge(location.x, location.y - 1, location.x, location.y)] = outerBorderConstraint;
				edges[new Edge(location.x, location.y, location.x + 1, location.y)] = outerBorderConstraint;
				edges[new Edge(location.x, location.y, location.x, location.y + 1)] = outerInnerBorderConstraint;
				edges[new Edge(location.x - 1, location.y, location.x, location.y)] = innerOuterBorderConstraint;
			}
			else if(location.x == width - halfWidth && location.y == depth - halfDepth) {
				edges[new Edge(location.x, location.y - 1, location.x, location.y)] = innerOuterBorderConstraint;
				edges[new Edge(location.x, location.y, location.x + 1, location.y)] = outerBorderConstraint;
				edges[new Edge(location.x, location.y, location.x, location.y + 1)] = outerBorderConstraint;
				edges[new Edge(location.x - 1, location.y, location.x, location.y)] = outerInnerBorderConstraint;
			}
			else if(location.x == -halfWidth - 1) {
				edges[new Edge(location.x, location.y - 1, location.x, location.y)] = outerInnerBorderConstraint;
				edges[new Edge(location.x, location.y, location.x + 1, location.y)] = innerBorderConstraint;
				edges[new Edge(location.x, location.y, location.x, location.y + 1)] = innerOuterBorderConstraint;
				edges[new Edge(location.x - 1, location.y, location.x, location.y)] = outerBorderConstraint;
			}
			else if(location.x == width - halfWidth) {
				edges[new Edge(location.x, location.y - 1, location.x, location.y)] = innerOuterBorderConstraint;
				edges[new Edge(location.x, location.y, location.x + 1, location.y)] = outerBorderConstraint;
				edges[new Edge(location.x, location.y, location.x, location.y + 1)] = outerInnerBorderConstraint;
				edges[new Edge(location.x - 1, location.y, location.x, location.y)] = innerBorderConstraint;
			}
			else if(location.y == -halfDepth - 1) {
				edges[new Edge(location.x, location.y - 1, location.x, location.y)] = outerBorderConstraint;
				edges[new Edge(location.x, location.y, location.x + 1, location.y)] = outerInnerBorderConstraint;
				edges[new Edge(location.x, location.y, location.x, location.y + 1)] = innerBorderConstraint;
				edges[new Edge(location.x - 1, location.y, location.x, location.y)] = innerOuterBorderConstraint;
			}
			else if(location.y == depth - halfDepth) {
				edges[new Edge(location.x, location.y - 1, location.x, location.y)] = innerBorderConstraint;
				edges[new Edge(location.x, location.y, location.x + 1, location.y)] = innerOuterBorderConstraint;
				edges[new Edge(location.x, location.y, location.x, location.y + 1)] = outerBorderConstraint;
				edges[new Edge(location.x - 1, location.y, location.x, location.y)] = outerInnerBorderConstraint;
			}
		}


		Dictionary<Location, Candidate> deployments = new Dictionary<Location, Candidate>();

		foreach(Location location in locations) {
			string[] front = edges[new Edge(location.x, location.y - 1, location.x, location.y)];
			string[] right = edges[new Edge(location.x, location.y, location.x + 1, location.y)];
			string[] back = edges[new Edge(location.x, location.y, location.x, location.y + 1)];
			string[] left = edges[new Edge(location.x - 1, location.y, location.x, location.y)];

			Constraints key = new Constraints(front, right, back, left).invert();


			List<Candidate> candidates = new List<Candidate>();

			Constraints rotatedKey = key;

			for(int rotation = 0; rotation < 4; rotation += 1) {
				if(cubeDatabase.ContainsKey(rotatedKey)) {
					candidates.AddRange(
						from cubePrefab in cubeDatabase[rotatedKey]
						select new Candidate(cubePrefab, rotation)
					);
				}

				rotatedKey = rotatedKey.rotateCCW();
			}

			
			// This should work as we supplied all necessary variations of cubes.
			Candidate candidate = candidates[Random.Range(0, candidates.Count)];

			float totalWeight = 0.0f;

			foreach(Candidate turd in candidates) {
				totalWeight += turd.Item1.weight;
			}

			float value = Random.value;

			float cumulative = 0.0f;

			foreach(Candidate turd in candidates) {
				cumulative += turd.Item1.weight / totalWeight;

				if(value < cumulative) {
					candidate = turd;

					break;
				}
			}

			deployments.Add(location, candidate);


			Constraints edgeKey = new Constraints(candidate.Item1.frontConstraints, candidate.Item1.rightConstraints, candidate.Item1.backConstraints, candidate.Item1.leftConstraints);

			for(int rotation = 0; rotation < candidate.Item2; rotation += 1) {
				edgeKey = edgeKey.rotateCW();
			}

			edges[new Edge(location.x, location.y - 1, location.x, location.y)] = edgeKey.front;
			edges[new Edge(location.x, location.y, location.x + 1, location.y)] = edgeKey.right;
			edges[new Edge(location.x, location.y, location.x, location.y + 1)] = edgeKey.back;
			edges[new Edge(location.x - 1, location.y, location.x, location.y)] = edgeKey.left;
		}


		cubes = new Dictionary<Location, Cube>();

		foreach(Location location in locations) {
			Candidate deployment = deployments[location];

			int x = location.x;
			int z = location.y;
			float angle = deployment.Item2 * 90;

			Vector3 position = new Vector3(x, 0.0f, z);
			Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);

			Cube cube = Instantiate(deployment.Item1, position, rotation, transform);

			cube.name = deployment.Item1.name + " (" + x + ", " + z + ", " + angle + ")";

			cubes.Add(location, cube);
		}

		// Build surface.
		generateSurface();
	}


	private void generateSurface() {
		// Build navigation mesh.
		NavMeshSurface navigationMeshSurface = GetComponent<NavMeshSurface>();
		navigationMeshSurface.BuildNavMesh();

		// Build surface mesh.
		var triangulation = NavMesh.CalculateTriangulation();

		Mesh mesh = new Mesh();
		mesh.vertices = triangulation.vertices;
		mesh.triangles = triangulation.indices;
		mesh.RecalculateNormals();

		surface = gameObject.AddComponent<MeshCollider>();
		surface.sharedMesh = mesh;
	}

	public Vector3? sampleSurface(Vector3 position, float distance) {
		NavMeshHit hit;

		if(NavMesh.SamplePosition(position, out hit, distance, -1)) {
			// For some reason SamplePosition does not return a position ON the NavMesh.
			return new Vector3(hit.position.x, position.y, hit.position.z);
		}

		return null;
	}

	public Vector3? hitSurface(Ray ray, float distance) {
		RaycastHit hit;

		if(surface.Raycast(ray, out hit, distance)) {
			return hit.point;
		}

		return null;
	}

	public Vector3? pickSurface() {
		Camera camera = Camera.main;

		Ray ray = camera.ScreenPointToRay(Input.mousePosition);

		return hitSurface(ray, camera.farClipPlane);
	}
}
