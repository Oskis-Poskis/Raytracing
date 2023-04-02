using OpenTK.Mathematics;
using System.Drawing;

namespace Raytracing.Common
{
    public class Ray
    {
        public Vector3 Origin { get; set; }
        public Vector3 Direction { get; set; }

        public Ray(Vector3 origin, Vector3 direction)
        {
            Origin = origin;
            Direction = direction;
        }

        public Vector3 At(float t)
        {
            return Origin + t * Direction;
        }
    }

    public class ColorUtility
    {
        public static Color vec3color(Vector3 col, int samples_per_pixel)
        {
            Vector3 color = col;

            float scale = 1.0f / samples_per_pixel;
            color.X = MathF.Sqrt(scale * color.X);
            color.Y = MathF.Sqrt(scale * color.Y);
            color.Z = MathF.Sqrt(scale * color.Z);

            int r_int = (int)(256 * Math.Clamp(color.X, 0.0f, 0.999f));
            int g_int = (int)(256 * Math.Clamp(color.Y, 0.0f, 0.999f));
            int b_int = (int)(256 * Math.Clamp(color.Z, 0.0f, 0.999f));

            return Color.FromArgb(255, r_int, g_int, b_int);
        }
    }

    public class MathUtility
    {
        public static Vector3 unit_vector(Vector3 v)
        {
            return v / v.Length;
        }
    }

    public static class RandomUtility
    {
        private static Random rng = new Random();

        public static float RandomFloat(float min, float max)
        {
            return (float)(min + rng.NextDouble() * (max - min));
        }

        public static Vector3 random_vector()
        {
            return new Vector3(RandomUtility.RandomFloat(0, 1), RandomUtility.RandomFloat(0, 1), RandomUtility.RandomFloat(0, 1));
        }

        public static Vector3 random_vector(float min, float max)
        {
            return new Vector3(RandomUtility.RandomFloat(min, max) , RandomUtility.RandomFloat(min, max), RandomUtility.RandomFloat(min, max));
        }

        public static Vector3 RandomInUnitSphere()
        {
            while (true)
            {
                Vector3 p = new Vector3(RandomUtility.RandomFloat(-1, 1), RandomUtility.RandomFloat(-1, 1), RandomUtility.RandomFloat(-1, 1));
                if (p.LengthSquared >= 1) continue;
                return p;
            }
        }

        public static Vector3 RandomInHemisphere(Vector3 normal)
        {
            Vector3 in_unit_sphere = RandomInUnitSphere();
            if (Vector3.Dot(in_unit_sphere, normal) > 0.0f)
            {
                return in_unit_sphere;
            }
            else
            {
                return -in_unit_sphere;
            }
        }
    }
}