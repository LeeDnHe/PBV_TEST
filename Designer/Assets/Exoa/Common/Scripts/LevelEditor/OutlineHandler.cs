using Exoa.Effects;
using System.Collections.Generic;
using UnityEngine;

namespace Exoa.Designer
{
    /// <summary>
    /// The OutlineHandler class is responsible for managing the outlines and materials of the child renderers.
    /// It allows objects to switch between 'ghost' (transparent) and normal states and controls the visibility of outlines.
    /// </summary>
    public class OutlineHandler : MonoBehaviour
    {
        // Indicates whether the object is currently in ghost mode
        [HideInInspector]
        public bool isGhost = false;

        // Dictionary to store the original materials of the renderers by their names
        protected Dictionary<string, Material> materials;

        // List to store Outlinable components for controlling outlines
        protected List<Outlinable> outlines;

        // Array to store MeshRenderers of the child objects
        protected MeshRenderer[] renderers;

        // Flag to indicate whether the outlines are currently being shown
        protected bool showingOutlines;

        /// <summary>
        /// Initializes the OutlineHandler by setting up the materials and outlines for the child renderers.
        /// This method is called when the script instance is being loaded.
        /// </summary>
        virtual protected void Start()
        {
            // Initialize lists and dictionary
            outlines = new List<Outlinable>();
            materials = new Dictionary<string, Material>();
            renderers = GetComponentsInChildren<MeshRenderer>();

            // Iterate through each renderer to set up outlines and store original materials
            foreach (MeshRenderer r in renderers)
            {
                // Skip the "Collider" game object
                if (r.gameObject.name == "Collider")
                    continue;

                // Add the renderer's material to the dictionary if not already added
                if (materials.ContainsKey(r.name) == false)
                    materials.Add(r.name, r.material);

                // Get or add the Outlinable component to the renderer's game object
                Outlinable outline = r.GetComponent<Outlinable>();
                if (outline == null)
                {
                    outline = r.gameObject.AddComponent<Outlinable>();
                    outline.AddAllChildRenderersToRenderingList(RenderersAddingMode.MeshRenderer);
                }
                // Set the outline's enabled state based on the current showingOutlines flag
                outline.enabled = showingOutlines;
                outlines.Add(outline);
            }
        }

        /// <summary>
        /// Cleans up the resources used by the OutlineHandler.
        /// This method is called when the MonoBehaviour will be destroyed.
        /// </summary>
        virtual protected void OnDestroy()
        {
            // Release references to the renderers, outlines, and materials
            renderers = null;
            outlines = null;
            materials = null;
        }

        /// <summary>
        /// Restores the original materials to the child renderers, removing any ghost effects.
        /// </summary>
        public void Unghost()
        {
            if (renderers != null)
            {
                // Iterate through each renderer to restore its original material
                foreach (MeshRenderer r in renderers)
                {
                    Material mat = null;
                    materials.TryGetValue(r.name, out mat);
                    if (mat != null) r.material = mat;
                }
            }
        }

        /// <summary>
        /// Applies a transparent material to the child renderers, making them appear ghost-like.
        /// </summary>
        /// <param name="transparentMaterial">The material to apply to make the renderers transparent.</param>
        /// <param name="alpha">The alpha value (transparency level) to set on the material's color.</param>
        public void Ghost(Material transparentMaterial, float alpha)
        {
            if (renderers != null)
            {
                // Iterate through each renderer to apply the transparent material
                foreach (MeshRenderer r in renderers)
                {
                    r.material = transparentMaterial;
                    r.material.color = new Color(1, 1, 1, alpha);
                }
            }
        }

        /// <summary>
        /// Toggles the visibility of the outlines for the child renderers.
        /// </summary>
        /// <param name="show">A boolean indicating whether to show or hide the outlines.</param>
        /// <param name="colorIndex">An optional index to set the outline color (default is 0).</param>
        public void ShowOutline(bool show, int colorIndex = 0)
        {
            // Set the flag indicating whether outlines should be shown
            showingOutlines = show;

            if (outlines != null)
            {
                // Iterate through each outline to enable or disable it
                foreach (Outlinable r in outlines)
                {
                    r.enabled = show;
                }
            }
        }
    }
}
