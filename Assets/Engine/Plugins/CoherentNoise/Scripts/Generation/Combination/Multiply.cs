namespace Assets.Engine.Plugins.CoherentNoise.Scripts.Generation.Combination
{
	/// <summary>
	/// Generator that multiplies two noise values
	/// </summary>
	public class Multiply: Generator
	{
		private readonly Generator m_a;
		private readonly Generator m_v;

		///<summary>
		/// Create new generator
		///</summary>
		///<param name="a">First generator to multiply</param>
		///<param name="b">Second generator to multiply</param>
		public Multiply(Generator a, Generator b)
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
			return m_a.GetValue(x, y, z) * m_v.GetValue(x, y, z);
		}

		#endregion
	}
}