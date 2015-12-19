using UnityEngine;

namespace Assets.Engine.Plugins.CoherentNoise.Scripts.Generation.Displacement
{
	///<summary>
	/// This generator scales its source by given vector.
	///</summary>
	public class Scale : Generator
	{
		private readonly Generator m_source;
		private readonly float m_x;
		private readonly float m_y;
		private readonly float m_z;

		///<summary>
		/// Create new scaling
		///</summary>
		///<param name="source">Source generator</param>
		///<param name="v">Scale value</param>
		public Scale(Generator source, Vector3 v)
			: this(source, v.x, v.y, v.z)
		{

		}
		///<summary>
		/// Create new scaling
		///</summary>
		///<param name="source">Source generator</param>
		///<param name="x">Scale amount along X axis</param>
		///<param name="y">Scale amount along Y axis</param>
		///<param name="z">Scale amount along Z axis</param>
		public Scale(Generator source, float x, float y, float z)
		{
			m_source = source;
			m_z = z;
			m_y = y;
			m_x = x;
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
			return m_source.GetValue(x * m_x, y * m_y, z * m_z);
		}

		#endregion
	}
}