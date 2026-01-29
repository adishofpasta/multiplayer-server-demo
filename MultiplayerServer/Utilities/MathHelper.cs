namespace MultiplayerServer.Utilities
{
    public static class MathHelper
    {
        public static float Lerp(float a, float b, float t)
        {
            t = MathF.Max(0, MathF.Min(1, t));
            return a + (b - a) * t;
        }

        public static float Clamp(float value, float min, float max)
        {
            return MathF.Max(min, MathF.Min(max, value));
        }
    }
}