using System.Text;
using UnityEngine;

namespace Assets.Engine.Scripts.Common.DataTypes
{
    public struct Vector3Int
    {
        #region Public statics

        public static readonly Vector3Int Zero = new Vector3Int(0, 0, 0);
        public static readonly Vector3Int One = new Vector3Int(1, 1, 1);
        public static readonly Vector3Int Up = new Vector3Int(0, 1, 0);
        public static readonly Vector3Int Down = new Vector3Int(0, -1, 0);
        public static readonly Vector3Int Forward = new Vector3Int(0, 0, 1);
        public static readonly Vector3Int Back = new Vector3Int(0, 0, -1);
        public static readonly Vector3Int Right = new Vector3Int(1, 0, 0);
        public static readonly Vector3Int Left = new Vector3Int(-1, 0, 0);

        #endregion

        #region Publics

        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        #endregion

        #region Constructor

        public Vector3Int(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        #endregion

        #region Operators

        public static implicit operator Vector3(Vector3Int v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static Vector3Int operator +(Vector3Int lhs, Vector3Int rhs)
        {
            return new Vector3Int(lhs.X + rhs.X, lhs.Y + rhs.Y, lhs.Z + rhs.Z);
        }

        public static Vector3Int operator -(Vector3Int lhs, Vector3Int rhs)
        {
            return new Vector3Int(lhs.X - rhs.X, lhs.Y - rhs.Y, lhs.Z - rhs.Z);
        }

        public static Vector3Int operator *(Vector3Int lhs, Vector3Int rhs)
        {
            return new Vector3Int(lhs.X*rhs.X, lhs.Y*rhs.Y, lhs.Z*rhs.Z);
        }

        public static Vector3Int operator *(Vector3Int vec, int i)
        {
            return new Vector3Int(vec.X * i, vec.Y * i, vec.Z * i);
        }

        public static Vector3Int operator *(int i, Vector3Int vec)
        {
            return new Vector3Int(vec.X * i, vec.Y * i, vec.Z * i);
        }

        #endregion

        public static float SqrDistance(Vector3Int a, Vector3Int b)
        {
            return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z);
        }

        public static float SqrMagnitude(Vector3Int a)
        {
            return (a.X * a.X) + (a.Y * a.Y) + (a.Z * a.Z);
        }

        #region Object overrides

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("<{0}, {1}, {2}>", X, Y, Z);
            return sb.ToString();
        }

        #endregion
    }
}