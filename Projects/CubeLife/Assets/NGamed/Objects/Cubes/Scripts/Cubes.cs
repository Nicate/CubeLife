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

	public string cubeeAgentName;
	public string satanAgentName;


	private Dictionary<Location, Cube> cubes;

	private NavMeshSurface cubeeNavigationMesh;
	private NavMeshSurface satanNavigationMesh;

	private MeshCollider cubeeSurface;
	private MeshCollider satanSurface;

	private float[] cubeeWeights;
	private float[] satanWeights;


	private void Awake() {
		foreach(NavMeshSurface navigationMesh in GetComponents<NavMeshSurface>()) {
			string name = NavMesh.GetSettingsNameFromID(navigationMesh.agentTypeID);
			
			if(name == cubeeAgentName) {
				cubeeNavigationMesh = navigationMesh;
				
				cubeeSurface = gameObject.AddComponent<MeshCollider>();
			}
			else if(name == satanAgentName) {
				satanNavigationMesh = navigationMesh;

				satanSurface = gameObject.AddComponent<MeshCollider>();
			}
		}
	}


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

		// Build surfaces.
		cubeeWeights = generateSurface(cubeeNavigationMesh, cubeeSurface);
		satanWeights = generateSurface(satanNavigationMesh, satanSurface);
	}


	private float[] generateSurface(NavMeshSurface navigationMesh, MeshCollider surface) {
		// Disable all surfaces so we can build only the one we are supposed to.
		foreach(NavMeshSurface navMeshSurface in GetComponents<NavMeshSurface>()) {
			navMeshSurface.enabled = false;
		}

		foreach(MeshCollider meshCollider in GetComponents<MeshCollider>()) {
			meshCollider.enabled = false;
		}

		// Build navigation mesh.
		navigationMesh.enabled = true;

		navigationMesh.BuildNavMesh();

		// Build surface mesh.
		var triangulation = NavMesh.CalculateTriangulation();

		Mesh mesh = new Mesh();
		mesh.vertices = triangulation.vertices;
		mesh.triangles = triangulation.indices;
		mesh.RecalculateNormals();
		
		surface.sharedMesh = mesh;

		// Calculate the surface area weights.
		Vector3[] vertices = triangulation.vertices;
		int[] triangles = triangulation.indices;

		float[] weights = new float[triangles.Length];
		float totalWeight = 0.0f;

		for(int index = 0; index < triangles.Length; index += 3) {
			int index0 = triangles[index + 0];
			int index1 = triangles[index + 1];
			int index2 = triangles[index + 2];

			Vector3 v0 = vertices[index0];
			Vector3 v1 = vertices[index1];
			Vector3 v2 = vertices[index2];

			float a = Vector3.Distance(v0, v1);
			float b = Vector3.Distance(v1, v2);
			float c = Vector3.Distance(v2, v0);

			// Single line version of Heron's formula.
			float area = 0.25f * Mathf.Sqrt((a + b + c) * (-a + b + c) * (a - b + c) * (a + b - c));

			weights[index / 3] = area;
			totalWeight += area;
		}

		weights[0] = weights[0] / totalWeight;

		for(int index = 1; index < weights.Length; index += 1) {
			weights[index] = weights[index - 1] + weights[index] / totalWeight;
		}

		// Reenable all surfaces.
		foreach(NavMeshSurface navigationMeshSurface in GetComponents<NavMeshSurface>()) {
			navigationMeshSurface.enabled = true;
		}

		foreach(MeshCollider meshCollider in GetComponents<MeshCollider>()) {
			meshCollider.enabled = true;
		}

		return weights;
	}


	private Vector3? hitSurface(MeshCollider surface, Ray ray, float distance) {
		RaycastHit hit;

		if(surface.Raycast(ray, out hit, distance)) {
			return hit.point;
		}

		return null;
	}

	private Vector3? pickSurface(MeshCollider surface, Camera camera) {
		Ray ray = camera.ScreenPointToRay(Input.mousePosition);

		return hitSurface(surface, ray, camera.farClipPlane);
	}


	private Vector3 sampleSurface(MeshCollider surface, float[] weights) {
		// First pick a random triangle.
		float value = Random.value;

		int triangleIndex = 3 * weights.Length - 3;

		for(int index = 0; index < weights.Length; index += 1) {
			if(value < weights[index]) {
				triangleIndex = 3 * index;

				break;
			}
		}

		int[] triangles = surface.sharedMesh.triangles;
		Vector3[] vertices = surface.sharedMesh.vertices;

		int vertexIndex0 = triangles[triangleIndex + 0];
		int vertexIndex1 = triangles[triangleIndex + 1];
		int vertexIndex2 = triangles[triangleIndex + 2];

		Vector3 vertex0 = vertices[vertexIndex0];
		Vector3 vertex1 = vertices[vertexIndex1];
		Vector3 vertex2 = vertices[vertexIndex2];

		// Then pick random barycentric coordinates within the triangle.
		float lambda0 = Random.value;
		float lambda1 = Random.value;
		float lambda2 = Random.value;

		return (lambda0 * vertex0 + lambda1 * vertex1 + lambda2 * vertex2) / (lambda0 + lambda1 + lambda2);
	}


	public Vector3? pickCubeeSurface() {
		return pickSurface(cubeeSurface, Camera.main);
	}

	public Vector3? pickSatanSurface() {
		return pickSurface(satanSurface, Camera.main);
	}


	public Vector3 sampleCubeeSurface() {
		return sampleSurface(cubeeSurface, cubeeWeights);
	}

	public Vector3 sampleSatanSurface() {
		return sampleSurface(satanSurface, satanWeights);
	}
}
