using System.Collections.Generic;
using XRL.World;

namespace QudUX.Utilities
{
    class ImposterUtilities
    {
        public static List<GameObject> DisableImposters(Zone z, int x1, int y1, int x2, int y2)
        {
            List<GameObject> disabledObjectsWithImposters = new List<GameObject>();
            for (int i = x1; i <= x2; i++)
            {
                for (int j = y1; j <= y2; j++)
                {
                    Cell cell = z.GetCell(i, j);
                    for (int k = 0; k < cell.Objects.Count; k++)
                    {
                        GameObject thing = cell.Objects[k];
                        if (thing.HasIntProperty("HasImposter") && !thing.HasPropertyOrTag("Non"))
                        {
                            disabledObjectsWithImposters.Add(thing);
                            thing.SetIntProperty("Non", 1);
                        }
                    }
                }
            }
            return disabledObjectsWithImposters;
        }

        public static void RestoreImposters(List<GameObject> objectsWithImposters)
        {
            if (objectsWithImposters != null)
            {
                foreach (GameObject thing in objectsWithImposters)
                {
                    thing.RemoveIntProperty("Non");
                }
            }
        }
    }
}
