using System.Text;
using UnityEngine;

namespace Assets.Engine.Scripts.Common.DataTypes
{
    public struct Vector2Int
    {
        #region Public statics

        public static readonly Vector2Int Zero = new Vector2Int(0, 0);
        public static readonly Vector2Int One = new Vector2Int(1, 1);

        #endregion

        #region Publics

        public readonly int X;
        public readonly int Z;

        #endregion

        #region Constructor

        public Vector2Int(int x, int z)
        {
            X = x;
            Z = z;
        }

        #endregion

        #region Operators

        public static implicit operator Vector2(Vector2Int v)
        {
            return new Vector3(v.X, v.Z);
        }

        public static Vector2Int operator +(Vector2Int lhs, Vector2Int rhs)
        {
            return new Vector2Int(lhs.X + rhs.X, lhs.Z + rhs.Z);
        }

        public static Vector2Int operator -(Vector2Int lhs, Vector2Int rhs)
        {
            return new Vector2Int(lhs.X - rhs.X, lhs.Z - rhs.Z);
        }

        public static Vector2Int operator *(Vector2Int lhs, Vector2Int rhs)
        {
            return new Vector2Int(lhs.X*rhs.X, lhs.Z*rhs.Z);
        }

        public static Vector2Int operator *(Vector2Int vec, int i)
        {
            return new Vector2Int(vec.X * i, vec.Z * i);
        }

        public static Vector2Int operator *(int i, Vector2Int vec)
        {
            return new Vector2Int(vec.X * i, vec.Z * i);
        }

        #endregion

        #region Object overrides

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("<{0}, {1}>", X, Z);
            return sb.ToString();
        }
        
        #endregion
    }
}