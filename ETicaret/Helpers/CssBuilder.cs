namespace ETicaret.Helpers
{
    public class CssBuilder
    {
        private readonly List<string> _classes = new();

        public CssBuilder(string baseClass)
        {
            _classes.Add(baseClass);
        }

        public CssBuilder AddClass(string className)
        {
            _classes.Add(className);
            return this;
        }

        public CssBuilder AddClass(string className, bool condition)
        {
            if (condition) _classes.Add(className);
            return this;
        }

        public string Build() => string.Join(" ", _classes);

        public override string ToString() => Build();
    }
}
