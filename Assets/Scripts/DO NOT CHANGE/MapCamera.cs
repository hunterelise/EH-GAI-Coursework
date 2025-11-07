using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class MapCamera : MonoBehaviour
{
	private const float ZoomAmount = 2.5f;

	private float cameraDefaultOrthographicSize;

	private Vector3 startTranformPosition;
	private Vector3 startScreenPosition;
	private Vector3 startWorldPosition;
	private bool previousMouseButton = false;

	private void Start()
	{
		cameraDefaultOrthographicSize = Camera.main.orthographicSize;
	}

	private void Update()
	{
		var camera = Camera.main;
		var currentMouseButton = Input.GetMouseButton(0);

		if (previousMouseButton == false && currentMouseButton)
		{
			// On first click store start position to be used in later frames for positioning
			startScreenPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.0f);
			startWorldPosition = camera.ScreenToWorldPoint(startScreenPosition);
			startTranformPosition = transform.position;
		}
		else if (currentMouseButton)
		{
			// Reset Camera to starting position and then calculate the difference in movement from the start
			transform.position = startTranformPosition;
			var newScreenPosition = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0.0f);
			var newWorldPosition = camera.ScreenToWorldPoint(newScreenPosition);
			var worldPositionDelta = startWorldPosition - newWorldPosition;

			// Apply difference from start
			transform.position = startTranformPosition + worldPositionDelta;
		}

		previousMouseButton = currentMouseButton;

		// Zooming
		if (Input.mouseScrollDelta.y > 0.0f && camera.orthographicSize > ZoomAmount)
		{
			camera.orthographicSize -= ZoomAmount;
		}
		else if (Input.mouseScrollDelta.y < 0.0f && camera.orthographicSize <= cameraDefaultOrthographicSize - ZoomAmount)
		{
			camera.orthographicSize += ZoomAmount;
		}
	}
}
