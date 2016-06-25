using UnityEngine;

namespace Engine.Plugins.CoherentNoise.Scripts.Generation.Combination
{
	/// <summary>
	/// This generator blends two noises together, using third as a blend weight. Note that blend weight's value is clamped to [0,1] range
	/// </summary>
	public class Blend : Generator
	{
		private readonly Generator m_a;
		private readonly Generator m_b;
		private readonly Generator m_weight;

		///<summary>
		/// Create new blend generator
		///</summary>
		///<param name="a">First generator to blend (this is returned if weight==0)</param>
		///<param name="b">Second generator to blend (this is returned if weight==1)</param>
		///<param name="weight">Blend weight source</param>
		public Blend(Generator a, Generator b, Generator weight)
		{
			m_a = a;
			m_weight = weight;
			m_b = b;
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
			var w = Mathf.Clamp01(m_weight.GetValue(x, y, z));
			return m_a.GetValue(x, y, z) * (1 - w) + m_b.GetValue(x, y, z) * w;
		}

		#endregion
	}
}