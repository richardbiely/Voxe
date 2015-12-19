namespace Assets.Engine.Plugins.CoherentNoise.Scripts.Generation.Modification
{
	/// <summary>
	/// This generator binarizes its source noise, returning only value 0 and 1. A constant treshold value is user for binarization. I.e. result will be 0 where source value is less than treshold,
	/// and 1 elsewhere.
	/// </summary>
	public class Binarize:Generator
	{
		private readonly Generator m_source;
		private readonly float m_treshold;

		///<summary>
		/// Create new binarize generator
		///</summary>
		///<param name="source">Source generator</param>
		///<param name="treshold">Treshold value</param>
		public Binarize(Generator source, float treshold)
		{
			m_source = source;
			m_treshold = treshold;
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
			return m_source.GetValue(x, y, z) > m_treshold ? 1 : 0;
		}

		#endregion
	}
}