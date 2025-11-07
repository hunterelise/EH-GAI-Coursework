using UnityEngine;

public abstract class SteeringBehaviour : MonoBehaviour
{
	/// <summary>
	/// Shows debug lines in scene view to help debug issues with creating steering behaviours.
	/// NOTE: [field: SerializeField] exposes a C# property to Unity's inspector which is useful to toggle at runtime
	/// </summary>
	[field: SerializeField]
	public bool ShowDebugLines { get; set; } = false;

	protected Vector3 desiredVelocity;
	protected Vector3 steeringVelocity;

	/// <summary>
	/// Do steering behaviour code here. At the end of this the desiredVelocity and steeringVelocity variables should be set
	/// </summary>
	/// <param name="steeringAgent">The agent this component is acting on</param>
	/// <returns>The steeringVelocity should always be returned here</returns>
	public abstract Vector3 UpdateBehaviour(SteeringAgent steeringAgent);

	protected virtual void Start()
	{
		// Annoyingly this is needed for the enabled bool to work in Unity. A MonoBehaviour must now have one of the following
		// to activate this: Start(), Update(), FixedUpdate(), LateUpdate(), OnGUI(), OnDisable(), OnEnabled()
	}

	/// <summary>
	/// Draws helpful debug info to the screen
	/// </summary>
	/// <param name="steeringAgent">The steering agent that the debug information is for</param>
	public virtual void DebugDraw(SteeringAgent steeringAgent)
	{
		var currentPoint = transform.position + steeringAgent.CurrentVelocity;

		DebugDrawLine("DebugDesiredVelocity " + GetType().Name, transform.position, transform.position + desiredVelocity, Color.red);
		DebugDrawLine("DebugCurrentVelocity " + GetType().Name, transform.position, currentPoint, Color.white);
		DebugDrawLine("DebugSteeringVelocity " + GetType().Name, currentPoint, currentPoint + (desiredVelocity - steeringAgent.CurrentVelocity), Color.blue);

		var lineRenderers = GetComponentsInChildren<LineRenderer>(true);
		foreach (var lineRenderer in lineRenderers)
		{
			if (lineRenderer.name.Contains("Debug") && lineRenderer.name.Contains(GetType().Name))
			{
				lineRenderer.gameObject.SetActive(ShowDebugLines && enabled);
			}
		}
	}

	/// <summary>
	/// Draws a line at runtime using a LineRenderer. If the LineRenderer with the name specified does not exist it will be created automatically
	/// </summary>
	/// <param name="name">Name of the LineRenderer to draw</param>
	/// <param name="startPosition">Start point of the line to draw</param>
	/// <param name="endPosition">End point of the line to draw</param>
	/// <param name="colour">Colour of the line</param>
	public virtual void DebugDrawLine(string name, Vector3 startPosition, Vector3 endPosition, Color colour)
	{
		var lineRenderer = GetDebugLineRenderer(name);
		lineRenderer.startColor = colour;
		lineRenderer.endColor = colour;
		lineRenderer.SetPosition(0, startPosition);
		lineRenderer.SetPosition(1, endPosition);

		// Also draw to the scene view
		Debug.DrawLine(startPosition, endPosition, colour);
	}

	/// <summary>
	/// Draws a circle using a LineRenderer for debugging purposes centred at position with a defined radius 
	/// </summary>
	/// <param name="position">Position of centre of circle</param>
	/// <param name="radius">Radius of the circle</param>
	/// <param name="lineCount">Number of lines used to draw the circle (More lines = smoother circle)</param>
	public virtual void DebugDrawCircle(string name, Vector3 position, float radius, Color colour, int lineCount = 24)
	{
		var lineRenderer = GetDebugLineRenderer(name);
		lineRenderer.startColor = colour;
		lineRenderer.endColor = colour;

		lineRenderer.positionCount = lineCount + 1;
		for (int lineIndex = 0; lineIndex < lineCount; ++lineIndex)
		{
			float firstAngle = ((float)lineIndex / (float)lineCount) * (2.0f * Mathf.PI);
			float secondAngle = ((float)(lineIndex + 1) / (float)lineCount) * (2.0f * Mathf.PI);
			Vector3 firstPoint = new Vector3(Mathf.Cos(firstAngle), Mathf.Sin(firstAngle), 0.0f) * radius;
			Vector3 secondPoint = new Vector3(Mathf.Cos(secondAngle), Mathf.Sin(secondAngle), 0.0f) * radius;
			Debug.DrawLine(firstPoint + position, secondPoint + position, colour);

			lineRenderer.SetPosition(lineIndex, firstPoint + position);
		}

		lineRenderer.SetPosition(lineCount, (new Vector3(Mathf.Cos(0.0f), Mathf.Sin(0.0f), 0.0f) * radius) + position);
	}

	/// <summary>
	/// Returns the LineRender with the specified name
	/// </summary>
	/// <param name="name">Name of the LineRenderer you want to get</param>
	/// <returns>LineRenderer with the specified name or null if not found</returns>
	protected virtual LineRenderer GetDebugLineRenderer(string name)
	{
		var debugTransform = transform.Find(name);
		LineRenderer lineRenderer;
		if (debugTransform == null)
		{
			var debugCircle = new GameObject();
			debugCircle.name = name;
			debugCircle.transform.SetParent(transform, false);
			lineRenderer = debugCircle.AddComponent<LineRenderer>();
			lineRenderer.material = Instantiate(Resources.Load<Material>("DebugLineRendererMaterial"));

			lineRenderer.startWidth = 0.1f;
			lineRenderer.endWidth = 0.1f;
		}
		else
		{
			lineRenderer = debugTransform.gameObject.GetComponent<LineRenderer>();
		}

		return lineRenderer;
	}
}