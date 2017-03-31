using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RayTracerDemo
{
    public class RayTracer
    {
        private readonly int screenWidth;
        private readonly int screenHeight;
        private const int MaxDepth = 5;

        public Action<int, int, RGB> setPixel;

        public RayTracer(int screenWidth, int screenHeight, Action<int, int, RGB> setPixel)
        {
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
            this.setPixel = setPixel;
        }

        private double TestRay(Ray ray, Scene scene)
        {
            double nearestDist = double.PositiveInfinity;

            foreach (var thing in scene.Things)
            {
                var curDist = thing.IntersectDistance(ray);
                if (curDist < nearestDist)
                {
                    nearestDist = curDist;
                }
            }

            return nearestDist;
        }

        private Color TraceRay(Ray ray, Scene scene, int depth)
        {
            Intersection nearestIntersection = default(Intersection);
            double nearestDist = double.PositiveInfinity;

            foreach (var thing in scene.Things)
            {
                var curDist = thing.IntersectDistance(ray);
                if (curDist < nearestDist)
                {
                    nearestDist = curDist;
                    nearestIntersection = new Intersection(thing, ray, curDist);
                }
            }

            if (nearestDist == double.PositiveInfinity)
            {
                return Color.Background;
            }

            return Shade(nearestIntersection, scene, depth);
        }

        private Color GetNaturalColor(SceneObject thing, Vector pos, Vector norm, Vector rd, Scene scene)
        {
            Color ret = default(Color);
            foreach (Light light in scene.Lights)
            {
                Vector ldis = light.Pos - pos;
                Vector livec = Vector.Norm(ldis);

                double neatIsect = TestRay(new Ray(pos, livec), scene);

                bool isInShadow = !(neatIsect == double.PositiveInfinity || ((neatIsect > Vector.Mag(ldis))));

                if (!isInShadow)
                {
                    double illum = Vector.Dot(livec, norm);
                    if (illum > 0)
                    {
                        ret += thing.Surface.Diffuse(pos) * (light.Color * illum);
                    }

                    double specular = Vector.Dot(livec, rd);
                    if (specular > 0)
                    {
                        ret += thing.Surface.Specular(pos) * (light.Color * Math.Pow(specular, thing.Surface.Roughness));
                    }
                }
            }
            return ret;
        }

        private Color GetReflectionColor(SceneObject thing, Vector pos, Vector norm, Vector rd, Scene scene, int depth)
        {
            return TraceRay(new Ray(pos, rd), scene, depth + 1) * thing.Surface.Reflect(pos);
        }

        private Color Shade(Intersection isect, Scene scene, int depth)
        {
            var d = isect.Ray.Direction;
            var pos = d * isect.Dist + isect.Ray.Start;
            var normal = isect.Thing.Normal(pos);
            var reflectDir = d - normal * (Vector.Dot(normal, d) * 2);

            Color ret = Color.DefaultColor;
            ret += GetNaturalColor(isect.Thing, pos, normal, reflectDir, scene);

            if (depth >= MaxDepth)
            {
                return ret + new Color(.5, .5, .5);
            }

            return ret + GetReflectionColor(isect.Thing, pos + (reflectDir * .001), normal, reflectDir, scene, depth);
        }

        private double RecenterX(double x)
        {
            return (x - (screenWidth / 2.0)) / (2.0 * screenWidth);
        }

        private double RecenterY(double y)
        {
            return -(y - (screenHeight / 2.0)) / (2.0 * screenHeight);
        }

        private Vector GetPoint(double x, double y, Camera camera)
        {
            return Vector.Norm(camera.Forward + camera.Right * RecenterX(x) + camera.Up * RecenterY(y));
        }

        internal void Render(Scene scene)
        {
            void DrawLine(int y)
            {
                for (int x = 0; x < screenWidth; x++)
                {
                    var ray = new Ray(scene.Camera.Pos, GetPoint(x, y, scene.Camera));
                    Color color = TraceRay(ray, scene, 0);
                    setPixel(x, y, color.ToDrawingColor());
                }
            }

            for (int y = 0; y < screenHeight; y++) DrawLine(y);

            // Parallel.For(0, screenHeight, (y) => DrawLine(y));
        }

        internal Scene DefaultScene(double angle) =>
            new Scene()
            {
                Things = new SceneObject[] {
                                new Plane(
                                    norm: new Vector(0,1,0),
                                    offset: 0,
                                    surface: Surfaces.CheckerBoard
                                ),
                                new Sphere(
                                    center: new Vector(0,1,0),
                                    radius: 1,
                                    surface: Surfaces.Shiny
                                ),
                                new Sphere(
                                    center: new Vector(-1,.5,1.5),
                                    radius: .5,
                                    surface: Surfaces.Shiny
                                )
                    //,
                    //            new Sphere(
                    //                center: new Vector(1, .6,-1.5),
                    //                radius: .6,
                    //                surface: Surfaces.Shiny
                    //            ),
                    //            new Sphere(
                    //                center: new Vector(-1, .7,-1.5),
                    //                radius: .7,
                    //                surface: Surfaces.Shiny
                    //            ),
                    //            new Sphere(
                    //                center: new Vector(1, .4, 1.5),
                    //                radius: .4,
                    //                surface: Surfaces.Shiny
                    //            ),
                    //            new Sphere(
                    //                center: new Vector(1.5, .5, 0.5),
                    //                radius: .5,
                    //                surface: Surfaces.Shiny
                    //            )
                },
                Lights = new Light[] {
                                new Light(
                                    pos: new Vector(-2,2.5,0),
                                    color: new Color(.49,.07,.07)
                                ),
                                new Light(
                                    pos: new Vector(1.5,2.5,1.5),
                                    color: new Color(.07,.07,.49)
                                ),
                                new Light(
                                    pos: new Vector(1.5,2.5,-1.5),
                                    color: new Color(.07,.49,.071)
                                ),
                                new Light(
                                    pos: new Vector(0,3.5,0),
                                    color: new Color(.21,.21,.35)
                                )},

                Camera = Camera.Create(
                    new Vector(Math.Sin(angle / 180 * Math.PI) * 6, 2, Math.Cos(angle / 180 * Math.PI) * 6),
                    new Vector(0, 0, 0))
            };
    }

    static class Surfaces
    {
        // Only works with X-Z plane.
        public static readonly Surface CheckerBoard = new CheckerBoard();
        public static readonly Surface Shiny = new Shiny();
    }

    struct Vector
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;

        public Vector(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Vector(string str)
        {
            string[] nums = str.Split(',');
            if (nums.Length != 3) throw new ArgumentException();
            X = double.Parse(nums[0]);
            Y = double.Parse(nums[1]);
            Z = double.Parse(nums[2]);
        }

        public static Vector operator *(Vector v, double n) => new Vector(v.X * n, v.Y * n, v.Z * n);

        public static Vector operator -(Vector v1, Vector v2) => new Vector(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);

        public static Vector operator +(Vector v1, Vector v2) => new Vector(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);

        public static double Dot(Vector v1, Vector v2) => (v1.X * v2.X) + (v1.Y * v2.Y) + (v1.Z * v2.Z);

        public static double Mag(Vector v) => Math.Sqrt(Dot(v, v));

        public static Vector Cross(Vector v1, Vector v2)
        {
            return new Vector(((v1.Y * v2.Z) - (v1.Z * v2.Y)),
                              ((v1.Z * v2.X) - (v1.X * v2.Z)),
                              ((v1.X * v2.Y) - (v1.Y * v2.X)));
        }

        public static Vector Norm(Vector v)
        {
            double mag = Mag(v);

            return mag > 0 ?
                v * (1 / mag) :
                default(Vector);
        }

        public static bool Equals(Vector v1, Vector v2)
        {
            return (v1.X == v2.X) && (v1.Y == v2.Y) && (v1.Z == v2.Z);
        }
    }

    public struct Color
    {
        public static readonly Color Background = default(Color);
        public static readonly Color DefaultColor = default(Color);

        public readonly double R;
        public readonly double G;
        public readonly double B;

        public Color(double r, double g, double b) { R = r; G = g; B = b; }
        public Color(string str)
        {
            string[] nums = str.Split(',');
            if (nums.Length != 3) throw new ArgumentException();
            R = double.Parse(nums[0]);
            G = double.Parse(nums[1]);
            B = double.Parse(nums[2]);
        }

        public static Color operator *(Color v, double n) => new Color(n * v.R, n * v.G, n * v.B);

        public static Color operator *(Color v1, Color v2) => new Color(v1.R * v2.R, v1.G * v2.G, v1.B * v2.B);

        public static Color operator +(Color v1, Color v2) => new Color(v1.R + v2.R, v1.G + v2.G, v1.B + v2.B);

        public static Color operator -(Color v1, Color v2) => new Color(v1.R - v2.R, v1.G - v2.G, v1.B - v2.B);

        public double Legalize(double d)
        {
            return d > 1 ? 1 : d;
        }

        internal RGB ToDrawingColor()
        {
            return new RGB((byte)(Legalize(R) * 255), (byte)(Legalize(G) * 255), (byte)(Legalize(B) * 255));
        }
    }

    struct Ray
    {
        public readonly Vector Start;
        public readonly Vector Direction;

        public Ray(Vector start, Vector dir)
        {
            this.Start = start;
            this.Direction = dir;
        }
    }

    struct Intersection
    {
        public readonly SceneObject Thing;
        public readonly Ray Ray;
        public readonly double Dist;

        public Intersection(SceneObject thing, Ray ray, double dist)
        {
            this.Thing = thing;
            this.Ray = ray;
            this.Dist = dist;
        }
    }

    abstract class Surface
    {
        public readonly double Roughness;

        public Surface(double roughness)
        {
            this.Roughness = roughness;
        }

        public abstract Color Diffuse(Vector pos);
        public abstract Color Specular(Vector pos);
        public abstract double Reflect(Vector pos);
    }

    internal class CheckerBoard : Surface
    {
        public CheckerBoard() : base(roughness: 150) { }

        public override Color Diffuse(Vector pos) => (((int)(pos.Z) + (int)(pos.X)) & 1) != 0
                            ? new Color(1, 1, 1)
                            : default(Color);

        public override Color Specular(Vector pos) => new Color(1, 1, 1);

        public override double Reflect(Vector pos) => (((int)(pos.Z) + (int)(pos.X)) & 1) != 0
                            ? .1
                            : .7;
    }

    internal class Shiny : Surface
    {
        public Shiny() : base(roughness: 50) { }

        public override Color Diffuse(Vector pos) => new Color(1, 1, 1);
        public override Color Specular(Vector pos) => new Color(.5, .5, .5);
        public override double Reflect(Vector pos) => .6;
    }

    class Camera
    {
        public readonly Vector Pos;
        public readonly Vector Forward;
        public readonly Vector Up;
        public readonly Vector Right;

        public Camera(Vector pos, Vector forward, Vector up, Vector right)
        {
            this.Pos = pos;
            this.Forward = forward;
            this.Up = up;
            this.Right = right;
        }

        public static Camera Create(Vector pos, Vector lookAt)
        {
            Vector forward = Vector.Norm(lookAt - pos);
            Vector down = new Vector(0, -1, 0);
            Vector right = Vector.Norm(Vector.Cross(forward, down)) * 1.5;
            Vector up = Vector.Norm(Vector.Cross(forward, right)) * 1.5;

            return new Camera(pos, forward, up, right);
        }
    }

    class Light
    {
        public readonly Vector Pos;
        public readonly Color Color;

        public Light(Vector pos, Color color)
        {
            this.Pos = pos;
            this.Color = color;
        }
    }

    abstract class SceneObject
    {
        public readonly Surface Surface;

        public SceneObject(Surface surface)
        {
            this.Surface = surface;
        }

        public abstract double IntersectDistance(Ray ray);
        public abstract Vector Normal(Vector pos);
    }

    class Sphere : SceneObject
    {
        private readonly Vector Center;
        private readonly double RadiusSq;

        public Sphere(Vector center, double radius, Surface surface)
            : base(surface)
        {
            this.Center = center;
            this.RadiusSq = radius * radius;
        }

        public override double IntersectDistance(Ray ray)
        {
            Vector eo = Center - ray.Start;
            double v = Vector.Dot(eo, ray.Direction);

            if (v > 0)
            {
                double disc = RadiusSq - (Vector.Dot(eo, eo) - (v * v));
                if (disc > 0)
                {
                    return v - Math.Sqrt(disc);
                }
            }

            return double.PositiveInfinity;
        }

        public override Vector Normal(Vector pos)
        {
            return Vector.Norm(pos - Center);
        }
    }

    class Plane : SceneObject
    {
        private readonly Vector Norm;
        private readonly double Offset;

        public Plane(Vector norm, double offset, Surface surface)
            : base(surface)
        {
            this.Norm = norm;
            this.Offset = offset;
        }

        public override double IntersectDistance(Ray ray)
        {
            double denom = Vector.Dot(Norm, ray.Direction);

            if (denom > 0)
            {
                return double.PositiveInfinity;
            }

            var dist = (Vector.Dot(Norm, ray.Start) + Offset) / (-denom);
            return dist;
        }

        public override Vector Normal(Vector pos)
        {
            return Norm;
        }
    }

    class Scene
    {
        public SceneObject[] Things;
        public Light[] Lights;
        public Camera Camera;
    }

    public struct RGB
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;

        public RGB(byte R, byte G, byte B)
        {
            this.R = R;
            this.G = G;
            this.B = B;
        }
    }
}