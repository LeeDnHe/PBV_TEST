using UnityEngine;

namespace Exoa.Designer
{
    /// <summary>
    /// The Grid class provides functionality for snapping objects to a grid, normalizing positions, and drawing gizmos.
    /// </summary>
    public class Grid : MonoBehaviour
    {
        // The spacing of the grid in each dimension
        public Vector3 space = Vector3.one;

        // Offset for the grid's origin
        public Vector3 offset = Vector3.zero;

        // Flag to determine whether to draw gizmos in the scene
        public bool drawGizmo;

        // Renderer used to calculate bounds for normalization
        public Renderer normalizationRenderer;

        /// <summary>
        /// Gets the material of the MeshRenderer in the grid's children.
        /// </summary>
        public Material Mat
        {
            get { return GetComponentInChildren<MeshRenderer>().material; }
        }

        /// <summary>
        /// Retrieves the bounds of the normalizationRenderer.
        /// </summary>
        /// <returns>A Bounds object representing the size and position of the renderer.</returns>
        public Bounds GetBounds()
        {
            Bounds b = normalizationRenderer.bounds;
            return b;
        }

        /// <summary>
        /// Gets the width of the grid based on the bounds of the normalizationRenderer.
        /// </summary>
        /// <returns>The width of the grid.</returns>
        public float GetWidth()
        {
            return GetBounds().size.x;
        }

        /// <summary>
        /// Snaps a given world position to the nearest grid point.
        /// </summary>
        /// <param name="position">The world position to snap.</param>
        /// <returns>The nearest point on the grid.</returns>
        public Vector3 GetNearestPointOnGrid(Vector3 position)
        {
            position -= transform.position;

            int xCount = Mathf.RoundToInt(position.x / space.x);
            int yCount = Mathf.RoundToInt(position.y / space.y);
            int zCount = Mathf.RoundToInt(position.z / space.z);

            Vector3 result = new Vector3(
                (float)xCount * space.x,
                (float)yCount * space.y,
                (float)zCount * space.z) + offset;

            result += transform.position;

            return result;
        }

        /// <summary>
        /// Converts a world position to a normalized position within the grid bounds.
        /// </summary>
        /// <param name="p">The world position to normalize.</param>
        /// <returns>A Vector2 representing the normalized position.</returns>
        public Vector2 GetNormalizedPosition(Vector3 p)
        {
            Bounds b = GetBounds();

            Vector3 vec = p - b.center + b.extents;
            Vector2 normalized = new Vector2(vec.x / b.size.x, vec.z / b.size.z);
            return normalized;
        }

        /// <summary>
        /// Converts a normalized position back to a world position within the grid bounds.
        /// </summary>
        /// <param name="normalizedPoint">The normalized position to convert.</param>
        /// <returns>A Vector3 representing the world position.</returns>
        public Vector3 GetWorldPosition(Vector2 normalizedPoint)
        {
            Bounds b = GetBounds();

            Vector3 p = new Vector3(normalizedPoint.x * b.size.x, 0, normalizedPoint.y * b.size.z);
            p = p + b.center - b.extents;

            return p;
        }

        /// <summary>
        /// Gets the local position within the grid from a given normalized position.
        /// </summary>
        /// <param name="normalizedPoint">The normalized position to convert.</param>
        /// <returns>A Vector3 representing the local position.</returns>
        public Vector3 GetLocalPosition(Vector2 normalizedPoint, Transform tr)
        {
            Vector3 worldPosition = GetWorldPosition(normalizedPoint);
            return tr.InverseTransformPoint(worldPosition);
        }

        /// <summary>
        /// Draws gizmos in the Unity Editor for visualizing the grid.
        /// </summary>
        void OnDrawGizmos()
        {
            if (!drawGizmo) return;

            Gizmos.color = Color.yellow;

            for (float x = 0; x < 10; x += space.x)
            {
                for (float y = 0; y < 10; y += space.y)
                {
                    var point = GetNearestPointOnGrid(new Vector3(x, y, 0f));
                    Gizmos.DrawSphere(point, 0.1f);
                }
            }
        }
    }
}
