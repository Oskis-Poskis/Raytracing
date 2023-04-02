using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Collections.Concurrent;

using Raytracing.Common;
using Raytracing.Geometry;
using Raytracing.UserInterface;

using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;

namespace Raytracing
{
    class Renderer : GameWindow
    {
        public Renderer(int width, int height, string title)
            : base(GameWindowSettings.Default, new  NativeWindowSettings()
            {
                Title = title,
                Size = new OpenTK.Mathematics.Vector2i(width, height),
                WindowBorder = WindowBorder.Resizable,
                StartVisible = false,
                StartFocused = true,
                WindowState = WindowState.Normal,
                API = ContextAPI.OpenGL,
                Profile = ContextProfile.Core,
                APIVersion = new  Version(3, 3),
                Flags = ContextFlags.Debug
            })
        {
            CenterWindow();
            viewportSize = Size;   
        }

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

        private ImGuiController ImGuiController;
        static Vector2i viewportSize;
        static int textureId;
        static int samples_per_pixel = 8;
        static int max_depth = 50;
        static string renderTime = "";
        
        protected override void OnLoad()
        {
            MakeCurrent();
            IsVisible = true;
            VSync = VSyncMode.On;

            GL.GenTextures(1, out textureId);
            GL.BindTexture(TextureTarget.Texture2D, textureId);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

            ImGuiController = new ImGuiController(viewportSize.X, viewportSize.Y);
            UI.LoadTheme();
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.ClearColor(new  Color4(1, 1, 1, 1));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            ImGuiController.Update(this, (float)args.Time);
            ImGui.DockSpaceOverViewport();

            ImGui.Begin("Settings");
            if (ImGui.Button("Render scene")) Render();
            ImGui.Separator();
            ImGui.SliderInt("Samples", ref samples_per_pixel, 1, 128);
            ImGui.Separator();
            ImGui.Text("Time: " + renderTime);
            ImGui.Text("Width: " + viewportSize.X.ToString());
            ImGui.Text("Height: " + viewportSize.Y.ToString());
            ImGui.End();

            ImGui.Begin("Scene");
            viewportSize = new(
                Convert.ToInt32(MathHelper.Abs(ImGui.GetWindowContentRegionMin().X - ImGui.GetWindowContentRegionMax().X)),
                Convert.ToInt32(MathHelper.Abs(ImGui.GetWindowContentRegionMin().Y - ImGui.GetWindowContentRegionMax().Y)));
            ImGui.Image((IntPtr)textureId, new System.Numerics.Vector2(viewportSize.X, viewportSize.Y));
            ImGui.End();
            ImGuiController.Render();
            
            SwapBuffers();
        }

        public static void Render()
        {
            int image_width = viewportSize.X;
            int image_height = viewportSize.Y;

            Bitmap bmp = new Bitmap(image_width, image_height);
            Camera camera = new(image_width, image_height, 1.0f);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Parallel.For(0, image_height, tile_y =>

            Vector3[,] pixels = new Vector3[image_height, image_width];
            for (int tile_y = 0; tile_y < image_height; tile_y++)
            {
                for (int tile_x = 0; tile_x < image_width; tile_x++)
                {
                    int x_start = tile_x;
                    int y_start = tile_y;
                    int x_end = Math.Min(x_start + 1, image_width);
                    int y_end = Math.Min(y_start + 1, image_height);

                    Vector3 tile_color = new Vector3(0, 0, 0);
                    for (int y = y_start; y < y_end; y++)
                    {
                        for (int x = x_start; x < x_end; x++)
                        {
                            Vector3 pixelcolor = new Vector3(0, 0, 0);
                            for (int s = 0; s < samples_per_pixel; s++)
                            {
                                float u = ((float)x + RandomUtility.RandomFloat(0, 1)) / (image_width - 1);
                                float v = ((float)y + RandomUtility.RandomFloat(0, 1)) / (image_height - 1);

                                pixelcolor += ray_color(camera.get_ray(u, v), max_depth);
                            }

                            tile_color = pixelcolor;
                        }
                    }

                    pixels[tile_y, tile_x] = tile_color;
                    Vector3 pixel_color = pixels[tile_y, tile_x] / samples_per_pixel;
                    bmp.SetPixel(tile_x, tile_y, ColorUtility.vec3color(pixel_color, 1));
                }
            };

            stopwatch.Stop();

            // bmp.Save("output.png", ImageFormat.Png);
            renderTime = stopwatch.Elapsed.ToString();

            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0);
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, e.Width, e.Height);
            ImGuiController.WindowResized(e.Width, e.Height);
        }
    }
}