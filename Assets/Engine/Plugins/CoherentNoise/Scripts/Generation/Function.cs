using System;

namespace Assets.Engine.Plugins.CoherentNoise.Scripts.Generation
{
	/// <summary>
	/// This generator creates "noise" that is actually a function of coordinates. Use it to create regular patterns that are then perturbed by noise
	/// </summary>
	public class Function: Generator
	{
		private readonly Func<float, float, float, float> m_func;

		/// <summary>
		/// Create new function generator
		/// </summary>
		/// <param name="func">Value function</param>
		public Function(Func<float, float, float, float> func)
		{
			m_func = func;
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
			return m_func(x, y, z);
		}

		#endregion
	}
}