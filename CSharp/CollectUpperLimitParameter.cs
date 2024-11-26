using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IfcRoomToRvtRoom
{
    public static class CollectParameter
    {
        public static int CollectUpperLimitParameter(List<Level> levelsClassList)
        {
            List<int> elevationsList = new List<int>();

            // Iterate level class.
            foreach (Level levelElement in levelsClassList)
            {
                // Collect level parameters.
                ParameterSet parameters = levelElement.Parameters;

                // Iterate parameters.
                foreach (Parameter param in parameters)
                {
                    // Collect parameter definition names.
                    string parameterDefinition = param.Definition.Name;

                    // Check if elevation is in the parameter definition names.
                    if (parameterDefinition.Equals("Elevation"))
                    {

                        // If true, look up the parameter in the level element.
                        Parameter elevationParameter = levelElement.LookupParameter("Elevation");
                        {
                            // Check if elevation parameter is not null.
                            if (elevationParameter != null)
                            {

                                // Collect the value of the elevation parameter.
                                string elevation = elevationParameter.AsValueString();

                                // Split the value to isolate the first number.
                                int elevationValue = int.Parse(elevation.Split('\'')[0]);

                                // Add the elevation value as an integer.
                                elevationsList.Add(elevationValue);
                            }
                        }
                    }
                }

            }
            // Sort the list in ascending order.
            var sortedElevationsList = elevationsList.OrderBy(e => e).ToList();

            // Set the variable to 0.
            int upperLimit = 0;

            // Outer loop - iterate the elements in the sorted elevations list.
            for (int i = 0; i < sortedElevationsList.Count; i++)
            {
                // i starts at index 0 and goes up to the last index in the list.
                int elevation1 = sortedElevationsList[i];

                // Inner loop - iterate over the elements in the sorted elevations list starting after index 0.
                for (int j = i + 1; j < sortedElevationsList.Count; j++)
                {
                    // j starts at i + 1 to ensure that j only considers elements after i to compare all items.
                    int elevation2 = sortedElevationsList[j];

                    // Check if i - j equals j.
                    if (Math.Abs(elevation1 - elevation2) == elevation2)
                    {
                        // If true, set the integer of elevation2 to the upper limit variable.
                        // The loop will then break once the Math condition is satisfied and return one value.  
                        upperLimit = elevation2;
                        break;
                    }
                }
                if (upperLimit != 0) break;
            }
            // Return the upper limit.
            return upperLimit;
        }
    }
}
