using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Scada.Scheme
{
    public struct ComponentCounter {
        public string ComponentTypeName { get; set; }
        public int Count { get; set; }
        public int MaxCount { get; set; }

        public void IncrementCounter() {
            Count++;
        }

        public void SetLimit(int maxCount)
        {
            this.MaxCount = maxCount;
        }
    }

    public class ComponentFilter
    {
        private List<ComponentCounter> componentsList;

        public ComponentFilter()
        {
            componentsList = new List<ComponentCounter>();
        }

        public void Reset()
        {
            componentsList.Clear();
        }

        public void AddNewComponent(string componentTypeName, int maxCount = 1000, bool updateCounter = false)
        {
            var existingItemIndex = componentsList.FindIndex(comp => comp.ComponentTypeName.ToLower().Equals(componentTypeName.ToLower()));

            if (existingItemIndex == -1)
            {
                componentsList.Add(new ComponentCounter() {
                    ComponentTypeName = componentTypeName,
                    Count = 0,
                    MaxCount = maxCount
                });
                existingItemIndex = componentsList.Count - 1;
            }
            if(updateCounter)
                componentsList[existingItemIndex].IncrementCounter();
        }

        public void Remove(string componentTypeName)
        {
            var existingItemIndex = componentsList.FindIndex(comp => comp.ComponentTypeName.ToLower().Equals(componentTypeName.ToLower()));
            if (existingItemIndex != -1)
                componentsList.RemoveAt(existingItemIndex);
        }

        [Obsolete]
        public void SetLimit(int maxCount, string componentTypeName)
        {
            var existingItemIndex = componentsList.FindIndex(comp => comp.ComponentTypeName.ToLower().Equals(componentTypeName.ToLower()));
            if (existingItemIndex != -1)
            {
                componentsList[existingItemIndex].SetLimit(maxCount);
            }
        }

        public int GetLimit(string componentTypeName)
        {
            var existingItemIndex = componentsList.FindIndex(comp => comp.ComponentTypeName.ToLower().Equals(componentTypeName.ToLower()));
            if (existingItemIndex != -1)
            {
                return componentsList[existingItemIndex].Count;
            }
            return -1;
        }

        public bool IsCountGreater(string componentTypeName)
        {
            var existingItemIndex = componentsList.FindIndex(comp => comp.ComponentTypeName.ToLower().Equals(componentTypeName.ToLower()));
            if (existingItemIndex != -1)
            {
                var result = componentsList[existingItemIndex].Count > componentsList[existingItemIndex].MaxCount;
                if (result)
                    ShowError();
                return result;
            }
            return false;
        }

        private void ShowError()
        {
            MessageBox.Show("Limit exception", "Error", MessageBoxButtons.OK, MessageBoxIcon.None,
                MessageBoxDefaultButton.Button1, (MessageBoxOptions)0x40000);  // MB_TOPMOST
        }

        public void IncrementCount(string componentTypeName)
        {
            var existingItemIndex = componentsList.FindIndex(comp => comp.ComponentTypeName.ToLower().Equals(componentTypeName.ToLower()));
            if (existingItemIndex != -1)
            {
                var tempRef = componentsList[existingItemIndex];
                tempRef.Count++;
                componentsList[existingItemIndex] = tempRef;
            }
        }
    }
}
