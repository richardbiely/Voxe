using UnityEngine;

namespace Assets.Engine.Plugins.CoherentNoise.Scripts.Generation.Modification
{
	///<summary>
	/// This generator modifies source noise by applying a curve transorm to it. Curves can be edited using Unity editor's CurveFields, or created procedurally.
	///</summary>
	public class Curve : Generator
	{
		private readonly Generator m_source;
		private readonly AnimationCurve m_curve;

		///<summary>
		/// Create a new curve generator
		///</summary>
		///<param name="source">Source generator</param>
		///<param name="curve">Curve to use</param>
		public Curve(Generator source, AnimationCurve curve)
		{
			m_source = source;
			m_curve = curve;
		}

		#region Overrides of NoiseGen

		/// <summary>
		///  Returns noise value at given point. 
		///  </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
		/// <param name="z">Z coordinate</param><returns>Noise value</returns>
		public override float GetValue(float x, float y, float z)
		{
			float v = m_source.GetValue(x, y, z);
			return m_curve.Evaluate(v);
		}

		#endregion
	}
}