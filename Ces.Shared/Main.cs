using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ces.Shared
{
    public class CesObjectState: INotifyPropertyChanged
    {
        public string Id { get; init; } = "";

        public Dictionary<string, object> Properties { get; } = new();

        public event Action<string, object>? SetChanged;
        public event Action? GetChanged;

        public void Set(string key, object value)
        {
            Properties[key] = value;
            SetChanged?.Invoke(key, value);
            OnPropertyChanged(key);
        }
        public T GetOrDefault<T>(string key, T defaultValue = default!)
        {
            if (Properties.TryGetValue(key, out var obj) && obj is T t)
                return t;

            return defaultValue;
        }
        public bool TryGet<T>(string key, out T value)
        {
            if (Properties.TryGetValue(key, out var obj) && obj is T typed)
            {
                value = typed;
                return true;
            }

            value = default!;
            return false;
        }
        public T Get<T>(string key)
        {
           GetChanged?.Invoke();
           return (T)Properties[key]; 
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    public abstract class CesBindable
    {
        protected Dictionary<string, Func<object>> _initialValues = new();
        protected Dictionary<string, Action<CesObjectState>> _bindings = new();

        protected void Bind<T>(
            string key,
            Func<T> getter,
            Action<T> setter
        )
        {
            _initialValues[key] = () => getter();
            _bindings[key] = state =>
            {
                setter(state.GetOrDefault(key, getter()));
            };
        }
        public void Reallocate(CesObjectState state)
        {
            foreach (var key in _bindings.Keys)
            {
                if (_initialValues.TryGetValue(key, out var get))
                {
                    state.Set(key, get());
                }
            }
        }
        public void Attach(CesObjectState state)
        {
            
            state.SetChanged += (key, value) =>
            {
                if (_bindings.TryGetValue(key, out var bind))
                {
                    bind(state);
                }
            };
            state.GetChanged +=() => {
                Reallocate(state); 
            };
            
            Reallocate(state);
            Apply(state);
        }

        public void Apply(CesObjectState state)
        {
            foreach (var bind in _bindings.Values)
                bind(state);
        }
    }


    public class CesShared
    {
        public Dictionary<string, CesObjectState> Objects { get; } = new();
        public Dictionary<string, object> Globals { get; } = new();
        public CesObjectState Create(string id)
        {
            var state = new CesObjectState { Id = id };
            Objects[id] = state;
            return state;
        }

        public CesObjectState Get(string id)
            => Objects[id];
    }

}
