using System;
using System.Text;
using UnityEngine;

namespace Assets.Engine.Scripts.Common.DataTypes
{
    public struct Vector3Int: IEquatable<Vector3Int>
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

        #endregion Public statics

        #region Publics

        public readonly int X;
        public readonly int Y;
        public readonly int Z;

        #endregion Publics

        #region Constructor

        public Vector3Int(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3Int(Vector3 vec)
        {
            X = (int)vec.x;
            Y = (int)vec.y;
            Z = (int)vec.z;
        }

        #endregion Constructor

        #region Operators

        public static implicit operator Vector3(Vector3Int v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static Vector3Int operator+(Vector3Int lhs, Vector3Int rhs)
        {
            return new Vector3Int(lhs.X+rhs.X, lhs.Y+rhs.Y, lhs.Z+rhs.Z);
        }

        public static Vector3Int operator-(Vector3Int lhs, Vector3Int rhs)
        {
            return new Vector3Int(lhs.X-rhs.X, lhs.Y-rhs.Y, lhs.Z-rhs.Z);
        }

        public static Vector3Int operator*(Vector3Int lhs, Vector3Int rhs)
        {
            return new Vector3Int(lhs.X*rhs.X, lhs.Y*rhs.Y, lhs.Z*rhs.Z);
        }

        public static Vector3Int operator*(Vector3Int vec, int i)
        {
            return new Vector3Int(vec.X*i, vec.Y*i, vec.Z*i);
        }

        public static Vector3Int operator*(int i, Vector3Int vec)
        {
            return new Vector3Int(vec.X*i, vec.Y*i, vec.Z*i);
        }

        public static bool operator==(Vector3Int lhs, Vector3Int rhs)
        {
            return lhs.X==rhs.X && lhs.Y==rhs.Y && lhs.Z==rhs.Z;
        }

        public static bool operator!=(Vector3Int lhs, Vector3Int rhs)
        {
            return lhs.X!=rhs.X || lhs.Y!=rhs.Y || lhs.Z!=rhs.Z;
        }

        #endregion Operators

        public static float SqrDistance(Vector3Int a, Vector3Int b)
        {
            return (a.X-b.X)*(a.X-b.X)+(a.Y-b.Y)*(a.Y-b.Y)+(a.Z-b.Z)*(a.Z-b.Z);
        }

        public static float SqrMagnitude(Vector3Int a)
        {
            return a.X*a.X+a.Y*a.Y+a.Z*a.Z;
        }

        #region IEquatable implementation

        public bool Equals(Vector3Int vec)
        {
            return X==vec.X && Y==vec.Y && Z==vec.Z;
        }

        #endregion IEquatable implementation

        #region Object overrides

        public override bool Equals(object other)
        {
            if (!(other is Vector3Int))
                return false;

            Vector3Int vec = (Vector3Int)other;
            return X==vec.X && Y==vec.Y && Z==vec.Z;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode()^Y.GetHashCode()<<2^Z.GetHashCode()>>2;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("<{0}, {1}, {2}>", X, Y, Z);
            return sb.ToString();
        }

        #endregion Object overrides
    }
}