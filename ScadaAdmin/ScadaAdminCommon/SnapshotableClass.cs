using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ScadaAdmin.Common
{
    /// <summary>
    /// Абстрактный класс для определения возможности создания снимков состояния
    /// </summary>
    abstract class SnapshotableClass
    {
        /// <summary>
        /// Список состояний класса
        /// </summary>
        public List<ClassSnapshot> _snapshots;

        public SnapshotableClass()
        {
            _snapshots = new List<ClassSnapshot>();
        }

        /// <summary>
        /// Создает снимок состояния
        /// </summary>
        private void DoSnapshot(string className)
        {
            var subclassTypes = Assembly
               .GetAssembly(typeof(SnapshotableClass))
               .GetTypes()
               .Where(t => t.IsSubclassOf(typeof(SnapshotableClass)));
            // TODO: Do snapshot by specified subclass name
        }

        /// <summary>
        /// Сохраняет текущее состояние класса
        /// </summary>
        /// <param name="className"></param>
        public void SaveState(string className = "")
        {
            DoSnapshot(className);
        }

        /// <summary>
        /// Восстанавливает последнее сохраненное состояние
        /// </summary>
        public void RestoreState()
        {
            // TODO: Restore state by Snapshot type
        }
    }
}
