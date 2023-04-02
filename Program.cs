using System;
using System.Numerics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

using Raytracing.Common;
using Raytracing.Geometry;
using System.Collections.Concurrent;

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
                    record1.point + record1.normal + RandomUtility.RandomInUnitSphere() :
                    record2.point + record2.normal + RandomUtility.RandomInUnitSphere();
                return 0.5f * ray_color(new Ray(hit1 ? record1.point : record2.point, target - (hit1 ? record1.point : record2.point)), depth - 1);
            }

            Vector3 unit_direction = Vector3.Normalize(r.Direction);
            float t = 0.5f * (unit_direction.Y + 1.0f);

            return (1.0f - t) * new Vector3(1.0f, 1.0f, 1.0f) + t * new Vector3(0.5f, 0.7f, 1.0f);
        }

        static void Main(string[] args)
        {
            int samples_per_pixel = 32;
            const int max_depth = 50;

            int image_width = 1024;
            int image_height = 1024;
            Camera camera = new(image_width, image_height, 1.0f);
            Bitmap bmp = new Bitmap(image_width, image_height);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            ConcurrentDictionary<(int, int), Vector3> tile_colors = new ConcurrentDictionary<(int, int), Vector3>();
            Parallel.For(0, image_width, tile_y =>
            {
                Parallel.For(0, image_height, tile_x =>
                {
                    int x_start = tile_x;
                    int y_start = tile_y;
                    int x_end = Math.Min(x_start + 1, image_width);
                    int y_end = Math.Min(y_start + 1, image_height);

                    for (int y = y_start; y < y_end; y++)
                    {
                        for (int x = x_start; x < x_end; x++)
                        {
                            Vector3 pixelcolor = new(0, 0, 0);
                            for (int s = 0; s < samples_per_pixel; s++)
                            {
                                float u = ((float)x + RandomUtility.RandomFloat(0, 1)) / (image_width - 1);
                                float v = ((float)y + RandomUtility.RandomFloat(0, 1)) / (image_height - 1);

                                pixelcolor += ray_color(camera.get_ray(u, v), max_depth);
                            }

                            tile_colors.AddOrUpdate((tile_x, tile_y), pixelcolor, (key, value) => value + pixelcolor);
                        }
                    }
                });
            });

            // Combine the colors from each tile into the final image
            for (int y = 0; y < image_height; y++)
            {
                for (int x = 0; x < image_width; x++)
                {
                    int tile_x = x;
                    int tile_y = y;
                    Vector3 tile_color = tile_colors[(tile_x, tile_y)];
                    int tile_pixels = Math.Min(1, (image_width - tile_x) * (image_height - tile_y));
                    Vector3 pixel_color = tile_color / (samples_per_pixel * tile_pixels);
                    bmp.SetPixel(x, y, ColorUtility.vec3color(pixel_color, 1));
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