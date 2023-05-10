using Prism.Mvvm;

namespace FindUnusedResources
{
    public class Resource : BindableBase
    {
        public Resource(string name, int count)
        {
            Name = name;
            Count = count;
        }

        public Resource(string name) : this(name, 0)
        { }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private int _count;
        public int Count
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }
    }
}