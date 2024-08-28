﻿using System.Numerics;

namespace AshDumpLib.HedgehogEngine;

public static class MathA
{
    public struct Matrix3x4
    {
        public float[,] matrix;

        public Matrix3x4(float[,] initialValues)
        {
            if (initialValues.GetLength(0) != 3 || initialValues.GetLength(1) != 4)
            {
                throw new ArgumentException("Initial values must be a 3x4 matrix.");
            }
            matrix = initialValues;
        }
    }

    public struct Color8
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;
    }

    public struct ColorF
    {
        public float r;
        public float g;
        public float b;
        public float a;
    }
}

public static class Helpers
{
    public static Vector3 ToEulerAngles(Quaternion q)
    {
        Vector3 angles = new();

        double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
        double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        angles.X = (float)Math.Atan2(sinr_cosp, cosr_cosp);

        double sinp = 2 * (q.W * q.Y - q.Z * q.X);
        if (Math.Abs(sinp) >= 1)
        {
            angles.Y = (float)Math.CopySign(Math.PI / 2, sinp);
        }
        else
        {
            angles.Y = (float)Math.Asin(sinp);
        }

        double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
        double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        angles.Z = (float)Math.Atan2(siny_cosp, cosy_cosp);

        return angles;
    }

    public static Quaternion ToQuaternion(Vector3 v)
    {

        float cy = (float)Math.Cos(v.Z * 0.5);
        float sy = (float)Math.Sin(v.Z * 0.5);
        float cp = (float)Math.Cos(v.Y * 0.5);
        float sp = (float)Math.Sin(v.Y * 0.5);
        float cr = (float)Math.Cos(v.X * 0.5);
        float sr = (float)Math.Sin(v.X * 0.5);

        return new Quaternion
        {
            W = (cr * cp * cy + sr * sp * sy),
            X = (sr * cp * cy - cr * sp * sy),
            Y = (cr * sp * cy + sr * cp * sy),
            Z = (cr * cp * sy - sr * sp * cy)
        };

    }
}
