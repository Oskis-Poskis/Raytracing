using System;
using System.Numerics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

using Raytracing.Common;
using Raytracing.Geometry;

namespace Raytracing
{
    class Program
    {
        static Vector3 ray_color(Ray r, int depth)
        {
            if (depth <= 0)
            {
                return new(0, 0, 0);
            }

            Sphere sphere1 = new Sphere(new Vector3(0, 0, -1), 0.5f);
            Sphere sphere2 = new Sphere(new Vector3(0, -100.5f, -1), 100f);

            HitRecord record1 = new HitRecord();
            HitRecord record2 = new HitRecord();
            
            bool hit1 = sphere1.Hit(r, 0.0001f, float.PositiveInfinity, out record1);
            bool hit2 = sphere2.Hit(r, 0.0001f, float.PositiveInfinity, out record2);

            if (hit1 || hit2)
            {
                Vector3 N = hit1 ? record1.normal : record2.normal;
                Vector3 target = hit1 ?
                    record1.point + record1.normal + RandomUtility.RandomInHemisphere(record1.normal) :
                    record2.point + record2.normal + RandomUtility.RandomInHemisphere(record2.normal);
                return 0.5f * ray_color(new Ray(hit1 ? record1.point : record2.point, target - (hit1 ? record1.point : record2.point)), depth - 1);
            }

            Vector3 unit_direction = Vector3.Normalize(r.Direction);
            float t = 0.5f * (unit_direction.Y + 1.0f);

            return (1.0f - t) * new Vector3(1.0f, 1.0f, 1.0f) + t * new Vector3(0.5f, 0.7f, 1.0f);
        }

        static void Main(string[] args)
        {
            int samples_per_pixel = 64;
            const int max_depth = 50;

            int image_width = 512;
            int image_height = 512;
            Camera camera = new(image_width, image_height, 1.0f);

            Bitmap bmp = new Bitmap(image_width, image_height);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            for (int x = 0; x < image_width; x++)
            {
                for (int y = 0; y < image_height; y++)
                {   
                    Vector3 pixelcolor = new(0, 0, 0);
                    for (int s = 0; s < samples_per_pixel; s++)
                    {
                        float u = ((float)x + RandomUtility.RandomFloat(0, 1)) / (image_width - 1);
                        float v = ((float)y + RandomUtility.RandomFloat(0, 1)) / (image_height - 1);
                       
                        pixelcolor += ray_color(camera.get_ray(u, v), max_depth);
                    }

                    bmp.SetPixel(x, y, ColorUtility.vec3color(pixelcolor, samples_per_pixel));
                }
            }

            stopwatch.Stop();

            // Save the bitmap as a PNG file
            bmp.Save("output.png", ImageFormat.Png);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Render time: {0:hh\\:mm\\:ss}", stopwatch.Elapsed.ToString());
            Console.ResetColor();
        }
    }
}