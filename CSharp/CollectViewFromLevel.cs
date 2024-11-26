using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IfcRoomToRvtRoom
{
    public static class CollectLevelView
    {
        public static ViewPlan CollectViewFromLevel(string levelName, List<Element> viewPlanElementsList)
        {
            // Iterate view plan elements.
            foreach(Element viewElement in viewPlanElementsList)
            {
                // Check if the view name is equal to the level names and that the view discipline is equal to architectural.
                if (viewElement.Name.Contains(levelName) && viewElement.get_Parameter(BuiltInParameter.VIEW_DISCIPLINE).AsValueString() == "Architectural")
                {
                    // Cast the element to viewplane.
                    return viewElement as ViewPlan;
                }

                /* ---> Remove if error occurs 1 of 2.
                
                if (viewElement.Name.Contains(levelName))
                {
                    ViewPlan view = viewElement as ViewPlan;
                    if (view != null)
                    {
                        TaskDialog.Show("View Plan Name", $"Found matching view {levelName}, {view.Name}");
                        return view;
                    }
                }
                ---> Remove 2 of 2 */
            }
            TaskDialog.Show("Error", $"No view pland found level: {levelName}");
            return null;
        }
    }
}
