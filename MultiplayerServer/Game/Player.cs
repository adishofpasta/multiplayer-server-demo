using MultiplayerServer.Utilities;

namespace MultiplayerServer.Game
{
    public class Player(int id)
    {
        public int Id { get; } = id;
        
        // Position
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        
        // Rotation
        public float RotationY { get; set; }
        
        // Velocity
        public float VelocityX { get; set; }
        public float VelocityY { get; set; }
        public float VelocityZ { get; set; }
        
        // Input
        public float MoveX { get; set; }  
        public float MoveZ { get; set; }  
        public float RotateY { get; set; }
        
        private const float MaxSpeed = 10f;
        private const float Acceleration = 20f;
        
        public void Update(float deltaTime)
        {
            VelocityX = MathHelper.Lerp(VelocityX, MoveX * MaxSpeed, Acceleration * deltaTime);
            VelocityZ = MathHelper.Lerp(VelocityZ, MoveZ * MaxSpeed, Acceleration * deltaTime);
            
            X += VelocityX * deltaTime;
            Z += VelocityZ * deltaTime;
            
            RotationY += RotateY * 30f * deltaTime;
            RotationY %= 360f;
            
            MoveX = 0;
            MoveZ = 0;
            RotateY = 0;
        }
    }
}
