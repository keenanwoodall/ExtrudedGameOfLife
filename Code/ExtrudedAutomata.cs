using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Automata
{
	public class Frame
	{
		public readonly int width, height;
		public readonly bool empty;

		public bool[,] grid;

		public Frame (int width, int height)
		{
			this.width = width;
			this.height = height;

			grid = new bool[width, height];
		}

		public Frame (Frame previous, int min, int max)
		{
			if (previous == null)
				throw new System.NullReferenceException ("Previous frame cannot be null.");

			width = previous.width;
			height = previous.height;

			empty = true;

			grid = new bool[width, height];


			for (int x = 1; x < width - 1; x++)
			{
				for (int y = 1; y < height - 1; y++)
				{
					var count = GetNeighborCount (previous, x, y);
					var value = false;
					if (grid[x, y])
						value = count >= min && count <= max;
					else
						value = count == max;
					if (value)
						empty = false;

					grid[x, y] = value;
				}
			}
		}

		public static int GetNeighborCount (Frame frame, int x, int y)
		{
			var count = 0;

			var grid = frame.grid;

			var xPlus = (x + 1) % frame.width;
			var xMinus = x - 1;
			if (xMinus < 0)
				xMinus = frame.width;
			var yPlus = (y + 1) % frame.height;
			var yMinus = y - 1;
			if (yMinus < 0)
				yMinus = frame.height;

			if (grid[xPlus, y] == true)
				count++;
			if (grid[xMinus, y] == true)
				count++;
			if (grid[x, yPlus] == true)
				count++;
			if (grid[x, yMinus] == true)
				count++;
			if (grid[xPlus, yPlus] == true)
				count++;
			if (grid[xMinus, yMinus] == true)
				count++;
			if (grid[xMinus, yPlus] == true)
				count++;
			if (grid[xPlus, yMinus] == true)
				count++;

			return count;
		}
	}

	public class ExtrudedAutomata : MonoBehaviour
	{
		public enum Placement { Noise, Random }

		[Header ("Initialization")]
		public int Width = 50;
		public int Height = 50;
		public Placement PlacementMode = ExtrudedAutomata.Placement.Noise;
		public float Frequency = 1f;
		[Range (0f, 1f)]
		public float Minimum = 0.5f;

		[Header ("Animation")]
		public float Delay = 0.5f;
		[Range (0, 8)]
		public int MinNeighbors = 2;
		[Range (0, 8)]
		public int MaxNeighbors = 3;
		[Range (1, 50)]
		public int MaxFrames = 20;
		public bool Scroll = true;
		public Vector3 Spacing = Vector3.one;
		public Mesh Mesh;
		public Material TopMaterial;
		public Material BodyMaterial;

		private List<Frame> frames;
		private Coroutine generateFramesRoutine;

		private void OnEnable ()
		{
			Initialize ();
			generateFramesRoutine = StartCoroutine (GenerateFramesRoutine ());
		}

		private void OnDisable ()
		{
			if (generateFramesRoutine != null)
				StopCoroutine (generateFramesRoutine);
			frames = new List<Frame> ();
		}

		public void Initialize ()
		{
			frames = new List<Frame> ();

			var firstFrame = new Frame (Width, Height);

			var offset = Random.Range (-1000, 1000);

			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					var value = 0f;
					if (PlacementMode == Placement.Noise) value = Mathf.PerlinNoise (x * Frequency + offset, y * Frequency);
					else value = Random.value;

					firstFrame.grid[x, y] = value > Minimum;
				}
			}

			frames.Add (firstFrame);
		}

		private IEnumerator GenerateFramesRoutine ()
		{
			while (true)
			{
				if (frames.Count > MaxFrames)
				{
					if (Scroll)
					{
						while (frames.Count > MaxFrames)
							frames.RemoveAt (0);
					}
					else
					{
						yield return null;
						continue;
					}
				}

				//frames[0] = new Frame (frames[frames.Count - 1]);
				var newFrame = new Frame (frames[frames.Count - 1], MinNeighbors, MaxNeighbors);

				if (newFrame.empty)
				{
					StopCoroutine (generateFramesRoutine);
					yield break;
				}

				frames.Add (newFrame);


				yield return new WaitForSeconds (Delay);
			}
		}

		private void Update ()
		{
			var transformMatrix = Matrix4x4.TRS (transform.position, transform.rotation, transform.lossyScale);

			for (int i = 0; i < frames.Count; i++)
			{
				var material = i == frames.Count - 1 ? TopMaterial : BodyMaterial;
				for (int x = 0; x < frames[i].width; x++)
				{
					for (int y = 0; y < frames[i].height; y++)
					{
						if (frames[i].grid[x, y] == true)
						{
							var matrix = transformMatrix * Matrix4x4.Translate (new Vector3 (x * Spacing.x, i * Spacing.y, y * Spacing.z));
							Graphics.DrawMesh (Mesh, matrix, material, 0);
						}
					}
				}
			}
		}
	}
}