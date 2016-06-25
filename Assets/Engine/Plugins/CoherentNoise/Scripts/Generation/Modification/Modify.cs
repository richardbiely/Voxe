using System;

namespace Engine.Plugins.CoherentNoise.Scripts.Generation.Modification
{
	/// <summary>
	/// This generator takes a source generator and applies a function to its output.
	/// </summary>
	public class Modify: Generator
	{
		private readonly Func<float, float> m_modifier;
		private readonly Generator m_source;

		///<summary>
		/// Create new generator
		///</summary>
		///<param name="source">Source generator</param>
		///<param name="modifier">Modifier function to apply</param>
		public Modify(Generator source, Func<float, float> modifier)
		{
			m_source = source;
			m_modifier = modifier;
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
			return m_modifier(m_source.GetValue(x, y, z));
		}

		#endregion
	}
}