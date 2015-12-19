using System;
using UnityEngine;

namespace Assets.Engine.Plugins.CoherentNoise.Scripts.Generation.Displacement
{
	/// <summary>
	/// This generator perturbs its source, using a user-supplied function to obtain displacement values. In other words, <see cref="Perturb"/> nonuniformly displaces each value of
	/// its source.
	/// </summary>
	public class Perturb: Generator
	{
		private readonly Generator m_source;
        private readonly Func<Vector3, Vector3> m_displacementSource;

		///<summary>
		/// Create new perturb generator
		///</summary>
		///<param name="source">Source generator</param>
		///<param name="displacementSource">Displacement generator</param>
        public Perturb(Generator source, Func<Vector3, Vector3> displacementSource)
		{
			m_source = source;
			m_displacementSource = displacementSource;
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
			Vector3 displacement = m_displacementSource(new Vector3(x, y, z));
			return m_source.GetValue(x + displacement.x, y + displacement.y, z + displacement.z);
		}

		#endregion
	}
}