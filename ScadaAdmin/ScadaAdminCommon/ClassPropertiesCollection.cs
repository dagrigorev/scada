using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScadaAdmin.Common
{
    /// <summary>
    /// Список свойств класса
    /// </summary>
    public class ClassPropertiesCollection
    {
        /// <summary>
        /// Коллекция свойств
        /// </summary>
        private List<ClassProp> _properties;

        public int Count => _properties.Count;

        public ClassPropertiesCollection()
        {
            _properties = new List<ClassProp>();
        }

        /// <summary>
        /// Очищает список свойств
        /// </summary>
        public void Clear()
        {
            if(_properties != null)
                _properties.Clear();
        }

        /// <summary>
        /// Добавляет свойство к коллекции
        /// </summary>
        /// <param name="property"></param>
        public void Add(ClassProp property)
        {
            _properties.Add(property);
        }

        /// <summary>
        /// Удаляет элемент с заданной позиции
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            if (_properties.Count > index)
                _properties.RemoveAt(index);
        }
    }
}
