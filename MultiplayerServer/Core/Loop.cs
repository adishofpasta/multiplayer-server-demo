using MultiplayerServer.Game;

namespace MultiplayerServer.Core
{
    public class Loop(World world)
    {
        private readonly World _world = world;
        private bool _running;
        private Thread? _thread;

        public event Action? OnTick;

        public void Start()
        {
            _running = true;
            _thread = new Thread(Run);
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;
        }

        private void Run()
        {
            const int tick = 30;
            const float delta = 1f / tick;

            while (_running)
            {
                Tick(delta);
                Thread.Sleep(1000 / tick);
            }
        }

        private void Tick(float deltaTime)
        {
            foreach (var player in _world.Players.Values)
            {
                player.Update(deltaTime);
            }

            OnTick?.Invoke();
        }
    }
}
