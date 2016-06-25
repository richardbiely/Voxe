using System;
using UnityEngine;

namespace Engine.Plugins.CoherentNoise.Scripts.Generation.Patterns
{
	///<summary>
	/// This generator does the opposite of texture generation. It takes a texture and returns its red channel as a noise value.
	/// Use it to incorporate hand-created patterns in your generation.
	///</summary>
	public class TexturePattern : Generator
	{
		private readonly Color[] m_colors;
		private readonly int m_width;
		private readonly int m_height;
		private readonly TextureWrapMode m_wrapMode;

		///<summary>
		/// Create new texture generator
		///</summary>
		///<param name="texture">Texture to use. It must be readable. The texture is read in constructor, so any later changes to it will not affect this generator</param>
		///<param name="wrapMode">Wrapping mode</param>
		public TexturePattern(Texture2D texture, TextureWrapMode wrapMode)
		{
			m_colors = texture.GetPixels();
			m_width = texture.width;
			m_height = texture.height;

			m_wrapMode = wrapMode;
		}

		#region Overrides of Noise

		/// <summary>
		///  Returns noise value at given point. 
		///  </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
		/// <param name="z">Z coordinate</param><returns>Noise value</returns>
		public override float GetValue(float x, float y, float z)
		{
			int ix = Mathf.FloorToInt(x * m_width);
			int iy = Mathf.FloorToInt(y * m_height);
			ix = Wrap(ix, m_width);
			iy = Wrap(iy, m_height);
			var c = m_colors[iy*m_width + ix];
			return c.r*2 - 1;
		}

		private int Wrap(int i, int size)
		{
			switch (m_wrapMode)
			{
				case TextureWrapMode.Repeat:
					return i >= 0 ? i%size : (i%size+size);
				case TextureWrapMode.Clamp:
					return i < 0 ? 0 : i > size ? size - 1 : i;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion
	}
}