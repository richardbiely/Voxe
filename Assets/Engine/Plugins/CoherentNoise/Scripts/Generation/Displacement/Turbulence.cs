using Assets.Engine.Plugins.CoherentNoise.Scripts.Generation.Fractal;
using UnityEngine;

namespace Assets.Engine.Plugins.CoherentNoise.Scripts.Generation.Displacement
{
    /// <summary>
    ///     Turbulence is a case of Perturb generator, that uses 3 Perlin noise generators as displacement source.
    /// </summary>
    public class Turbulence : Generator
    {
        private readonly int m_seed;
        private readonly Generator m_source;
        private Generator m_displacementX;
        private Generator m_displacementY;
        private Generator m_displacementZ;
        private float m_frequency;
        private int m_octaveCount;

        /// <summary>
        ///     Create new perturb generator
        /// </summary>
        /// <param name="source">Source generator</param>
        /// <param name="seed">Seed value for perturbation noise</param>
        public Turbulence(Generator source, int seed)
        {
            m_source = source;
            m_seed = seed;
            Power = 1;
            Frequency = 1;
            OctaveCount = 6;
        }

        /// <summary>
        ///     Turbulence power, in other words, amount by which source will be perturbed.
        ///     Default value is 1.
        /// </summary>
        public float Power { get; set; }

        /// <summary>
        ///     Frequency of perturbation noise.
        ///     Default value is 1.
        /// </summary>
        public float Frequency
        {
            get { return m_frequency; }
            set
            {
                m_frequency = value;
                CreateDisplacementSource();
            }
        }

        /// <summary>
        ///     Octave count of perturbation noise
        ///     Default value is 6
        /// </summary>
        public int OctaveCount
        {
            get { return m_octaveCount; }
            set
            {
                m_octaveCount = value;
                CreateDisplacementSource();
            }
        }

        #region Overrides of Noise

        /// <summary>
        ///     Returns noise value at given point.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        /// <returns>Noise value</returns>
        public override float GetValue(float x, float y, float z)
        {
            Vector3 displacement =
                new Vector3(m_displacementX.GetValue(x, y, z), m_displacementY.GetValue(x, y, z),
                    m_displacementZ.GetValue(x, y, z))*Power;
            return m_source.GetValue(x + displacement.x, y + displacement.y, z + displacement.z);
        }

        #endregion

        private void CreateDisplacementSource()
        {
            m_displacementX = new PinkNoise(m_seed) {Frequency = Frequency, OctaveCount = OctaveCount};
            m_displacementY = new PinkNoise(m_seed + 1) {Frequency = Frequency, OctaveCount = OctaveCount};
            m_displacementZ = new PinkNoise(m_seed + 2) {Frequency = Frequency, OctaveCount = OctaveCount};
        }
    }
}