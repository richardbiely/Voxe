using UnityEngine;

namespace Assets.Engine.Plugins.CoherentNoise.Scripts.Generation.Combination
{
	/// <summary>
	/// This generator returns minimum value of its two source generators
	/// </summary>
	public class Min : Generator
	{
		private readonly Generator m_a;
		private readonly Generator m_v;

		///<summary>
		/// Create new generator
		///</summary>
		///<param name="a">First generator</param>
		///<param name="b">Second generator</param>
		public Min(Generator a, Generator b)
		{
			m_a = a;
			m_v = b;
		}

		#region Implementation of Noise

		/// <summary>
		/// Returns noise value at given point. 
		/// </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
		/// <param name="z">Z coordinate</param>
		/// <returns>Noise value</returns>
		public override float GetValue(float x, float y, float z)
		{
			return Mathf.Min(m_a.GetValue(x, y, z), m_v.GetValue(x, y, z));
		}

		#endregion
	}
}