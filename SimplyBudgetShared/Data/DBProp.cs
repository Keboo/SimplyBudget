
using System.Collections.Generic;

namespace SimplyBudgetShared.Data
{
    internal class DBProp<T>
    {
        private T _originalValue;
        private T _value;
        private bool _modified;

        public DBProp(T initialValue)
        {
            _originalValue = _value = initialValue;
        }

        public bool Modified
        {
            get { return _modified; }
        }

        public T OriginalValue
        {
            get { return _originalValue; }
        }

        public T Value
        {
            get { return _value; }
            set
            {
                _modified = EqualityComparer<T>.Default.Equals(_originalValue, value) == false;
                _value = value;
            }
        }

        public void Saved()
        {
            _modified = false;
            _originalValue = _value;
        }

        public static implicit operator T(DBProp<T> property)
        {
            if (property == null)
                return default(T);
            return property.Value;
        }
    }
}
