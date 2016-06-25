using UnityEngine;

namespace Engine.Plugins.CoherentNoise.Scripts.Generation.Displacement
{
	/// <summary>
	/// This generator rotates its source around origin.
	/// </summary>
	public class Rotate: Generator
	{
		private readonly Generator m_source;
		private readonly Quaternion m_rotation;

		///<summary>
		/// Create new rotation using a quaternion
		///</summary>
		///<param name="source">Source generator</param>
		///<param name="rotation">Rotation</param>
		public Rotate(Generator source, Quaternion rotation)
		{
			m_source = source;
			m_rotation = rotation;
		}

		///<summary>
		/// Create new rotation using Euler angles
		///</summary>
		///<param name="source">Source generator</param>
		///<param name="angleX">Rotation around X axis</param>
		///<param name="angleY">Rotation around Y axis</param>
		///<param name="angleZ">Rotation around Z axis</param>
		public Rotate(Generator source, float angleX, float angleY, float angleZ):this(source, Quaternion.Euler(angleX,angleY,angleZ))
		{
			
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
			Vector3 v = m_rotation*new Vector3(x,y,z);
			return m_source.GetValue(v);
		}

		#endregion
	}
}