using System;

namespace Shooter.Scripts.Shared
{
    public class GamePool<T> where T : class
    {
        private readonly T[]     _pool;
        private readonly Func<T> _creator;
        private          int     _count;

        public GamePool(Func<T> creator) : this(creator, 8)
        {
        }

        public GamePool(Func<T> creator, int capacity)
        {
            _pool = new T[capacity];
            _creator = creator;
        }

        public T Get()
        {
            if (_count > 0)
            {
                _count--;
                var result = _pool[_count];
                _pool[_count] = default;
                return result;
            }

            return _creator();
        }

        public bool Put(T gameObject)
        {
            if (_count == _pool.Length)
                return false;

            _pool[_count] = gameObject;
            _count++;
            return true;
        }
    }
}