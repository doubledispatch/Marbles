using OpenTK;
using static Marbles.Utils;

namespace Marbles
{
    class Camera
    {
        public Vector3 FocusPoint { get; set; }
        public float Yaw { get; set; }
        public float Pitch { get; set; }
        public float Zoom { get; set; }
        public bool FirstPerson { get; set; }

        public Camera() { }

        public Camera(Vector3 focusPoint, float yaw, float pitch, float zoom, bool firstPerson)
        {
            FocusPoint = focusPoint;
            Yaw = yaw;
            Pitch = pitch;
            Zoom = zoom;
            FirstPerson = firstPerson;
        }

        public Camera Copy => new Camera(FocusPoint, Yaw, Pitch, Zoom, FirstPerson);

        public Vector3 Position => FirstPerson ? FocusPoint : FocusPoint - Forward * Zoom;

        public Vector3 Forward =>
            new Vector3(
                Cos(Pitch) * Sin(Yaw),
                Cos(Pitch) * Cos(Yaw),
                Sin(Pitch)
            ).Normalized();

        public Vector3 Right => Vector3.Cross(Forward, Vector3.UnitZ).Normalized();

        public Vector3 Up => -Vector3.Cross(Forward, Right).Normalized();
    }
}
