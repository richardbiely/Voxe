using UnityEngine;

namespace Assets.Engine.Scripts.Atlas
{
    /// <summary>
    /// Utility class for retrieving texture rectangles
    /// </summary>
    public static class TextureAtlas
    {
        #region Constants
	
        private const int LogNumImages = 4;
	
        /// <summary>
        /// How many images are packed in the atlas horizontally/vertically.
        /// </summary>
        public const int NumImages = 1 << LogNumImages; //16
	
        private const int MaskNumImages = NumImages - 1;
	
        // the width/height of a texture rectangle
        private const float RectSize = 1f / NumImages;
	
        #endregion
	
        #region Static Methods
	
        /// <summary>
        /// Gets the rectangle for the given texture ID.
        /// </summary>
        public static Rect GetRectangle (int textureEntry)
        {
            int x = textureEntry & MaskNumImages;
            int y = textureEntry >> LogNumImages;
		
            // we add a small offset to the rectangle (0.001) to avoid texture aliasing
            // otherwise, every so often neighboring textures would be sampled which results in cracks in the blocks
            return new Rect ((x * RectSize) + 0.001f, (y * RectSize) + 0.001f, RectSize - 0.002f, RectSize - 0.002f);
        }
	
        #endregion
    }
}