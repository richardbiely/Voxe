using UnityEngine;

namespace Assets.Engine.Plugins.CoherentNoise.Scripts.Generation.Fractal
{
	/// <summary>
	/// A variation of Perlin noise, this generator creates billowy shapes useful for cloud generation. It uses the same formula as Perlin noise, but adds 
	/// absolute values of signal
	/// </summary>
	public class BillowNoise : FractalNoiseBase
	{
		private float m_curPersistence;

		///<summary>
		/// Create new billow generator using seed (seed is used to create a <see cref="GradientNoise"/> source)
		///</summary>
		///<param name="seed">seed value</param>
		public BillowNoise(int seed)
			: base(seed)
		{
			Persistence = 0.5f;
		}
		///<summary>
		/// Create new billow generator with user-supplied source. Usually one would use this with <see cref="ValueNoise"/> or gradient noise with less dimensions, but 
		/// some weird effects may be achieved with other generators.
		///</summary>
		///<param name="source">noise source</param>
		public BillowNoise(Generator source)
			: base(source)
		{
			Persistence = 0.5f;
		}

		#region Overrides of FractalNoiseBase

		/// <summary>
		/// Returns new resulting noise value after source noise is sampled. 
		/// </summary>
		/// <param name="curOctave">Octave at which source is sampled (this always starts with 0</param>
		/// <param name="signal">Sampled value</param>
		/// <param name="value">Resulting value from previous step</param>
		/// <returns>Resulting value adjusted for this sample</returns>
		protected override float CombineOctave(int curOctave, float signal, float value)
		{
			if (curOctave == 0)
				m_curPersistence = 1;
			value = value + (2 * Mathf.Abs(signal) - 1) * m_curPersistence;
			m_curPersistence *= Persistence;
			return value;
		}

		#endregion

		/// <summary>
		/// Persistence value determines how fast signal diminishes with frequency. i-th octave signal will be multiplied by presistence to the i-th power.
		/// Note that persistence values >1 are possible, but will not produce interesting noise (lower frequencies will just drown out)
		/// 
		/// Default value is 0.5
		/// </summary>
		public float Persistence { get; set; }
	}
}