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
        public float AngularVelocity { get; set; }

        // Input received from the client
        public float MoveX { get; set; }  
        public float MoveZ { get; set; }  
        public float RotateY { get; set; }
        
        private const float MaxSpeed = 10f;
        private const float MaxAngularSpeed = 10f;
        private const float Acceleration = 20f;
        private const float AngularAcceleration = 20f;

        public void Update(float deltaTime)
        {
            VelocityX = MathHelper.Lerp(VelocityX, MoveX * MaxSpeed, Acceleration * deltaTime);
            VelocityZ = MathHelper.Lerp(VelocityZ, MoveZ * MaxSpeed, Acceleration * deltaTime);
            AngularVelocity = MathHelper.Lerp(AngularVelocity, RotateY * MaxAngularSpeed, AngularAcceleration * deltaTime);

            X += VelocityX * deltaTime;
            Z += VelocityZ * deltaTime;
            
            RotationY += AngularVelocity * deltaTime;
            RotationY %= 360f;

            // We clear the input after processing, in preparation for the next frame
            MoveX = 0;
            MoveZ = 0;
            RotateY = 0;
        }
    }
}
