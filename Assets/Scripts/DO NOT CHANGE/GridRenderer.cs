using UnityEngine;

public sealed class GridRenderer : MonoBehaviour
{
	private const float positionZ = 1.0f;

	private int gridWidth;
	private int gridHeight;
	private float gridWidthInWorld;
	private float gridHeightInWorld;
	private float lineThickness;

	private Mesh mesh;
	private MeshFilter meshFilter;
	private MeshRenderer meshRenderer;


	public void Initialise(int gridWidth, int gridHeight, float gridWidthInWorld, float gridHeightInWorld, float lineThickness = 1.0f)
	{
		this.gridWidth = gridWidth;
		this.gridHeight = gridHeight;
		this.gridWidthInWorld = gridWidthInWorld;
		this.gridHeightInWorld = gridHeightInWorld;
		this.lineThickness = lineThickness;

		bool hasMesh = mesh != null;
		if(!hasMesh)
		{
			mesh = new Mesh();
		}
		
		CreateMesh();

		if (!hasMesh)
		{
			meshFilter = gameObject.AddComponent<MeshFilter>();
			meshFilter.mesh = mesh;

			meshRenderer = gameObject.AddComponent<MeshRenderer>();
			meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			meshRenderer.receiveShadows = false;
			meshRenderer.sharedMaterial = Resources.Load<Material>("GridMaterial");
		}
	}

	private void CreateMesh()
	{
		int verticesCount = (4 * (gridHeight + 1)) + (4 * (gridWidth + 1));
		int trianglesCount = (6 * (gridHeight + 1)) + (6 * (gridWidth + 1));

		Vector3[] vertices = new Vector3[verticesCount];
		Vector2[] uvs = new Vector2[verticesCount];
		int[] triangles = new int[trianglesCount];

		int vertexIndex = 0;


		for (int x = 0; x <= gridWidth; ++x)
		{
			float startX = ((float)x) * gridWidthInWorld / ((float)gridWidth);
			vertices[vertexIndex] = new Vector3(startX, 0.0f, positionZ);
			vertices[vertexIndex + 1] = new Vector3(startX, gridHeightInWorld, positionZ);
			vertices[vertexIndex + 2] = new Vector3(startX + lineThickness, gridHeightInWorld, positionZ);
			vertices[vertexIndex + 3] = new Vector3(startX + lineThickness, 0.0f, positionZ);
			vertexIndex += 4;
		}

		for (int y = 0; y <= gridHeight; ++y)
		{
			float startY = ((float)y) * gridHeightInWorld / ((float)gridHeight);
			vertices[vertexIndex] = new Vector3(0.0f, startY, positionZ);
			vertices[vertexIndex + 1] = new Vector3(0.0f, startY + lineThickness, positionZ);
			vertices[vertexIndex + 2] = new Vector3(gridWidthInWorld, startY + lineThickness, positionZ);
			vertices[vertexIndex + 3] = new Vector3(gridWidthInWorld, startY, positionZ);
			vertexIndex += 4;
		}

		vertexIndex = 0;
		for (int triangleIndex = 0; triangleIndex < trianglesCount; triangleIndex += 6)
		{
			triangles[triangleIndex] = vertexIndex;
			triangles[triangleIndex + 1] = vertexIndex + 1;
			triangles[triangleIndex + 2] = vertexIndex + 2;
			triangles[triangleIndex + 3] = vertexIndex + 2;
			triangles[triangleIndex + 4] = vertexIndex + 3;
			triangles[triangleIndex + 5] = vertexIndex + 0;
			vertexIndex += 4;
		}

		mesh.vertices = vertices;
		mesh.uv = uvs;
		mesh.triangles = triangles;
		mesh.UploadMeshData(true);
    }
}
