using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScadaAdmin.Common
{
    /// <summary>
    /// Снимок состояния класса
    /// </summary>
    class ClassSnapshot
    {
        /// <summary>
        /// Состояния свойств класса
        /// </summary>
        private ClassPropertiesCollection _props;

        /// <summary>
        /// Тип класса сохраняемого состояния
        /// </summary>
        public Type ObjectType { get; set; }

        /// <summary>
        /// Описание последнего действия
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Время сохранения состояния
        /// </summary>
        public DateTime SnapshotDate { get; set; }

        public ClassSnapshot()
        {
            _props = new ClassPropertiesCollection();
            SnapshotDate = DateTime.Now;
            Description = "";
        }

        /// <summary>
        /// Добавляет свойство к коллекции
        /// </summary>
        /// <param name="property"></param>
        public void Add(ClassProp property)
        {
            _props.Add(property);
        }

        /// <summary>
        /// Удаляет элемент с заданной позиции
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            if (_props.Count > index)
                _props.RemoveAt(index);
        }
    }
}
