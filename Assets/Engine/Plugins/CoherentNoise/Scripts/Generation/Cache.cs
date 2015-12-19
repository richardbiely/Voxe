namespace Assets.Engine.Plugins.CoherentNoise.Scripts.Generation
{
	/// <summary>
	/// This generator returns its source unchanged. However, it caches last returned value, and does not recalculate it if called several times for the same point.
	/// This is handy if you use same noise generator in different places.
	/// 
	/// Note that displacement, fractal and Voronoi generators call GetValue at different points for their respective source generators.  
	/// This wil trash the Cache and negate any performance benefit, so there's no point in using Cache with these generators.
	/// </summary>
	public class Cache: Generator
	{
		private float m_x;
		private float m_y;
		private float m_z;
		private float m_cached;
		private readonly Generator m_source;

		///<summary>
		///Create new caching generator
		///</summary>
		///<param name="source">Source generator</param>
		public Cache(Generator source)
		{
			m_source = source;
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
			if (x == m_x && y == m_y && z == m_z)
				return m_cached;

		    m_x = x;
		    m_y = y;
		    m_z = z;
		    return m_cached = m_source.GetValue(x, y, z);
		}

		#endregion
	}
}