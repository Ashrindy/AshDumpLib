namespace AshDumpLib.Helpers;

public static class MathA
{
    public static uint SwapBytes(uint x)
    {
        x = (x >> 16) | (x << 16);
        return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
    }

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

        public override string ToString()
        {
            return $"#{r.ToString("X2")}{g.ToString("X2")}{b.ToString("X2")}{a.ToString("X2")}";
        }
    }

    public struct ColorF
    {
        public float r;
        public float g;
        public float b;
        public float a;
    }

    public struct Crop
    {
        public float Left;
        public float Top;
        public float Right;
        public float Bottom;
    }
}
