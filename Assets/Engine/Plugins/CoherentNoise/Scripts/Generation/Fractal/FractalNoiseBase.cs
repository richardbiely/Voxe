using UnityEngine;

namespace Engine.Plugins.CoherentNoise.Scripts.Generation.Fractal
{
	///<summary>
	/// base class for fractal noise generators. Fractal generators use a source noise, that is sampled at several frequencies. 
	/// These sampled values are then combined into a result using some algorithm. 
	///</summary>
	public abstract class FractalNoiseBase : Generator
	{
		private static readonly Quaternion s_rotation = Quaternion.Euler(30, 30, 30);

		private readonly Generator m_noise;
		private float m_frequency;
		private float m_lacunarity;
		private int m_octaveCount;

		/// <summary>
		/// Creates a new fractal noise using default source: gradient noise seeded by supplied seed value
		/// </summary>
		/// <param name="seed">seed value</param>
		protected FractalNoiseBase(int seed)
		{
			m_noise = new GradientNoise(seed);
            m_lacunarity = 2.17f;
            m_octaveCount = 6;
            m_frequency = 1;
		}

		/// <summary>
		/// Creates a new fractal noise, supplying your own source generator
		/// </summary>
		/// <param name="source">source noise</param>
		protected FractalNoiseBase(Generator source)
		{
			m_noise = source;
            m_lacunarity = 2.17f;
            m_octaveCount = 6;
            m_frequency = 1;
		}

		///<summary>
		/// Frequency coefficient. Sampling frequency is multiplied by lacunarity value with each octave.
		/// Default value is 2, so that every octave doubles sampling frequency
		///</summary>
		public float Lacunarity
		{
			get { return m_lacunarity; }
			set
			{
				m_lacunarity = value;
				OnParamsChanged();
			}
		}

		/// <summary>
		/// Number of octaves to sample. Default is 6.
		/// </summary>
		public int OctaveCount
		{
			get { return m_octaveCount; }
			set
			{
				m_octaveCount = value;
				OnParamsChanged();
			}
		}

		/// <summary>
		/// Initial frequency.
		/// </summary>
		public float Frequency
		{
			get { return m_frequency; }
			set
			{
				m_frequency = value;
				OnParamsChanged();
			}
		}

		/// <summary>
		///  Returns noise value at given point. 
		///  </summary>
		/// <param name="x">X coordinate</param>
		/// <param name="y">Y coordinate</param>
		/// <param name="z">Z coordinate</param><returns>Noise value</returns>
		public override float GetValue(float x, float y, float z)
		{
			float value = 0;

		    x *= Frequency;
			y *= Frequency;
			z *= Frequency;

			for (int curOctave = 0; curOctave < OctaveCount; curOctave++)
			{
				// Get the coherent-noise value from the input value and add it to the
				// final result.
				float signal = m_noise.GetValue(x, y, z);
				// дефолтный перлин - складывает все значения с уменьшающимся весом
				value = CombineOctave(curOctave, signal, value);

				// Prepare the next octave.
				// scale coords to increase frequency, then rotate to break up lattice pattern
				var rotated = s_rotation*(new Vector3(x, y, z) * Lacunarity);
				x = rotated.x;
				y = rotated.y;
				z = rotated.z;
			}

			return value;
		}

		/// <summary>
		/// Returns new resulting noise value after source noise is sampled. 
		/// </summary>
		/// <param name="curOctave">Octave at which source is sampled (this always starts with 0</param>
		/// <param name="signal">Sampled value</param>
		/// <param name="value">Resulting value from previous step</param>
		/// <returns>Resulting value adjusted for this sample</returns>
		protected abstract float CombineOctave(int curOctave, float signal, float value);

		/// <summary>
		/// This method is called whenever any generator's parameter is changed (i.e. Lacunarity, Frequency or OctaveCount). Override it to precalculate any values used in generation.
		/// </summary>
		protected virtual void OnParamsChanged()
		{
		}
	}
}