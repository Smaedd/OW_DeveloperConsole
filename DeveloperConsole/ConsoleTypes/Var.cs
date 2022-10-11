using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DeveloperConsole.ConsoleTypes
{
    internal class Var
    {
        private FieldInfo _field;
        private PropertyInfo _property;

        public readonly Type Type;
        public string Info { get; set; } = null;

        public Var(FieldInfo field)
        {
            _field = field;
            _property = null;

            Type = field.FieldType;
        }

        public Var(PropertyInfo property)
        {
            _field = null;
            _property = property;

            Type = property.PropertyType;
        }

        public void SetValue(object val)
        {
            if (_field != null)
            {
                _field.SetValue(null, val);
            }
            else // property
            {
                _property.SetValue(null, val);
            }
        }

        public object GetValue()
        {
            if (_field != null)
            {
                return _field.GetValue(null);
            }
            else // property
            {
                return _property.GetValue(null);
            }
        }

        public object[] GetCustomAttributes(Type type, bool inherit = false)
        {
            if (_field != null)
            {
                return _field.GetCustomAttributes(type, inherit);
            }
            else // property
            {
                return _property.GetCustomAttributes(type, inherit);
            }
        }
    }
}
