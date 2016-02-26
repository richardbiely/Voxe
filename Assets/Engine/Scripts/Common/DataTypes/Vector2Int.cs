using System;
using System.Text;
using UnityEngine;

namespace Assets.Engine.Scripts.Common.DataTypes
{
    public struct Vector2Int: IEquatable<Vector2Int>
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

        public static Vector2Int operator+(Vector2Int lhs, Vector2Int rhs)
        {
            return new Vector2Int(lhs.X+rhs.X, lhs.Z+rhs.Z);
        }

        public static Vector2Int operator-(Vector2Int lhs, Vector2Int rhs)
        {
            return new Vector2Int(lhs.X-rhs.X, lhs.Z-rhs.Z);
        }

        public static Vector2Int operator*(Vector2Int lhs, Vector2Int rhs)
        {
            return new Vector2Int(lhs.X*rhs.X, lhs.Z*rhs.Z);
        }

        public static Vector2Int operator*(Vector2Int vec, int i)
        {
            return new Vector2Int(vec.X*i, vec.Z*i);
        }

        public static Vector2Int operator*(int i, Vector2Int vec)
        {
            return new Vector2Int(vec.X*i, vec.Z*i);
        }

        public static bool operator ==(Vector2Int lhs, Vector2Int rhs)
        {
            return lhs.X == rhs.X && lhs.Z == rhs.Z;
        }

        public static bool operator !=(Vector2Int lhs, Vector2Int rhs)
        {
            return lhs.X != rhs.X || lhs.Z != rhs.Z;
        }

        #endregion

        #region IEquatable implementation

        public bool Equals(Vector2Int vec)
        {
            return X==vec.X && Z==vec.Z;
        }

        #endregion

        #region Object overrides

        public override bool Equals(object other)
        {
            if (!(other is Vector2Int))
                return false;

            Vector2Int vec = (Vector2Int)other;
            return X==vec.X && Z==vec.Z;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode()^Z.GetHashCode()<<2;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("<{0}, {1}>", X, Z);
            return sb.ToString();
        }

        #endregion
    }
}