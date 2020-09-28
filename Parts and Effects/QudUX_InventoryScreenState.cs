using System;
using System.Collections.Generic;

namespace XRL.World.Parts
{
    [Serializable]
    public class QudUX_InventoryScreenState : IPart
    {
        public List<string> InventoryCategory = new List<string>();
        public List<bool> InventoryCategory_Expanded = new List<bool>();
        public List<string> CategoriesInOtherTab = new List<string>();

        public bool GetExpandState(string category)
        {
            int index = InventoryCategory.IndexOf(category);
            if (index < 0)
            {
                InventoryCategory.Add(category);
                InventoryCategory_Expanded.Add(true);
                return true;
            }
            return InventoryCategory_Expanded[index];
        }

        public void SetExpandState(string category, bool isExpanded)
        {
            int index = InventoryCategory.IndexOf(category);
            if (index < 0)
            {
                InventoryCategory.Add(category);
                InventoryCategory_Expanded.Add(isExpanded);
            }
            else
            {
                InventoryCategory_Expanded[index] = isExpanded;
            }
        }

        public bool ToggleExpandState(string category)
        {
            int index = InventoryCategory.IndexOf(category);
            if (index >= 0)
            {
                InventoryCategory_Expanded[index] = !InventoryCategory_Expanded[index];
                return true;
            }
            return false;
        }

        public bool AddCategoryToOtherTab(string category)
        {
            if (CategoriesInOtherTab.Contains(category))
            {
                return false;
            }
            CategoriesInOtherTab.Add(category);
            return true;
        }

        public bool RemoveCategoryFromOtherTab (string category)
        {
            if (!CategoriesInOtherTab.Contains(category))
            {
                return false;
            }
            CategoriesInOtherTab.Remove(category);
            return true;
        }
    }
}
