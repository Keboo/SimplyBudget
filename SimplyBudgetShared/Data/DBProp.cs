
using System.Collections.Generic;

namespace SimplyBudgetShared.Data
{
    internal class DBProp<T>
    {
        private T _value;

        public DBProp(T initialValue)
        {
            OriginalValue = _value = initialValue;
        }

        public bool Modified { get; private set; }

        public T OriginalValue { get; private set; }

        public T Value
        {
            get => _value;
            set
            {
                Modified = EqualityComparer<T>.Default.Equals(OriginalValue, value) == false;
                _value = value;
            }
        }

        public void Saved()
        {
            Modified = false;
            OriginalValue = _value;
        }

        public static implicit operator T(DBProp<T> property)
        {
            if (property is null)
                return default(T);
            return property.Value;
        }
    }
}
