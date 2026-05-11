using System;
using System.Collections.Generic;

namespace PuzzleBattle
{
    internal sealed class SimplePool<T> where T : class
    {
        private readonly Stack<T> _items = new Stack<T>();
        private readonly Func<T> _factory;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;

        public SimplePool(Func<T> factory, Action<T> onGet = null, Action<T> onRelease = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onGet = onGet;
            _onRelease = onRelease;
        }

        public int Count => _items.Count;

        public T Get()
        {
            T item = _items.Count > 0 ? _items.Pop() : _factory();
            _onGet?.Invoke(item);
            return item;
        }

        public void Release(T item)
        {
            if (item == null)
            {
                return;
            }

            _onRelease?.Invoke(item);
            _items.Push(item);
        }

        public void Clear(Action<T> onDispose = null)
        {
            if (onDispose == null)
            {
                _items.Clear();
                return;
            }

            while (_items.Count > 0)
            {
                onDispose(_items.Pop());
            }
        }
    }
}
