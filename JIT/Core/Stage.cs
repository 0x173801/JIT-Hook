
namespace jit_winform.Core
{
    public abstract class Stage
    {
        public abstract string Name { get; }
        public abstract void Execute(FileContext context);
    }
}
