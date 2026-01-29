namespace ETicaret.Helpers
{
    public class Debouncer : IDisposable
    {
        private System.Timers.Timer? _timer;
        private readonly int _delay;

        public Debouncer(int delayMs = 500)
        {
            _delay = delayMs;
        }

        public void Debounce(Action action)
        {
            _timer?.Stop();
            _timer?.Dispose();

            _timer = new System.Timers.Timer(_delay);
            _timer.Elapsed += (s, e) =>
            {
                _timer?.Dispose();
                action();
            };
            _timer.AutoReset = false;
            _timer.Start();
        }

        public void Dispose() => _timer?.Dispose();
    }
}
