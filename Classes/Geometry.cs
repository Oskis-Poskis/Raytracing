using OpenTK.Mathematics;
using Raytracing.Common;

namespace Raytracing.Geometry
{
    public struct HitRecord
    {
        public Vector3 point;
        public Vector3 normal;
        public float t;

        public bool front_face;

        public void set_face_normal(Ray r, Vector3 outward_normal)
        {
            front_face = Vector3.Dot(r.Direction, outward_normal) < 0;
            normal = front_face ? outward_normal : -outward_normal;
        }
    }

    public interface IHittable
    {
        public bool Hit(Ray r, float t_min, float t_max, out HitRecord hit_record);
    }

    public class Sphere : IHittable
    {
        public Vector3 center;
        public float radius;

        public Sphere(Vector3 C, float r)
        {
            this.center = C;
            this.radius = r;
        }

        public bool Hit(Ray r, float t_min, float t_max, out HitRecord rec)
        {
            rec = new HitRecord();

            Vector3 oc = r.Origin - center;
            float a = r.Direction.LengthSquared;
            float half_b = Vector3.Dot(oc, r.Direction);
            float c = oc.LengthSquared - radius * radius;
            float discriminant = half_b * half_b - a * c;

            if (discriminant < 0)
            {
                return false;
            }

            float sqrtd = (float)Math.Sqrt(discriminant);

            float root = (-half_b - sqrtd) / a;
            if (root < t_min || t_max < root)
            {
                root = (-half_b + sqrtd) / a;
                if (root < t_min || t_max < root)
                {
                    return false;
                }
            }

            rec.t = root;
            rec.point = r.At(rec.t);
            Vector3 outward_normal = (rec.point - center) / radius;
            rec.set_face_normal(r, outward_normal);

            return true;
        }
    }
}