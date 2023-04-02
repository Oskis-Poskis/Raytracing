using OpenTK.Mathematics;
using Raytracing.Common;

public class Camera
{
    // Define image size
    float aspect_ratio;
    float focal_length = 1.0f;
    Vector3 origin;
    Vector3 horizontal;
    Vector3 vertical;
    Vector3 lower_left_corner;

    float viewport_height = 2;
    float viewport_width;

    public Camera(int image_width, int image_height, float f_length)
    {
        aspect_ratio = (float)image_width / (float)image_height;
        viewport_width = aspect_ratio * viewport_height;

        origin = new(0, 0, 0);
        horizontal = new(viewport_width, 0, 0);
        vertical = new(0, -viewport_height, 0);
        lower_left_corner = origin - horizontal / 2 - vertical / 2 - new Vector3(0, 0, focal_length);
    }

    public Ray get_ray(float u, float v)
    {
        return new(origin, lower_left_corner + u * horizontal + v * vertical - origin);
    }
}