using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IfcRoomToRvtRoom
{
    [Transaction(TransactionMode.Manual)]
    public class IfcRvtRoom : IExternalCommand
    {
        // Main entry point for the external command.
        public Result Execute(
            ExternalCommandData commandData,    // Provides access to the Revit API.
            ref string message,    // Message to display to the user in case of a failure.
            ElementSet elements)    // Set the elements selected by the user.

        {
            //#####################################################

            // Collect the current document.
            Document doc = commandData.Application.ActiveUIDocument.Document;

            //#####################################################

            // Filtered Element Collectors.

            var levelsCategory = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().ToElements();

            var levelClass = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToList();

            var roomCategory = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().ToElements();

            var rvtLinks = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_RvtLinks).WhereElementIsNotElementType().ToElements();

            var rvtLinkType = new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance)).ToElements();

            var viewPlanClass = new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).WhereElementIsNotElementType().ToElements().ToList();

            //#####################################################

            // Execute Revit Link Dialog box.
            string selectedLinkToCheck = null;

            // Invoke and open a dialog for the user to select a Revit link instance.
            RevitLinkInstance selectedLink = RevitLinkDialog.ShowRevitLinkDialog(doc);

            if (selectedLink != null)
            {
                // Split the name of the selected link at the colon character to remove unwanted characters from the name.
                string[] splitSelectedLink = selectedLink.Name.Split(':');

                // If the name contains at least one part, set it as the selected link to check.
                if (splitSelectedLink.Length > 0)
                {
                    // Collect string at index 0.
                    selectedLinkToCheck = splitSelectedLink[0];

                    // Output a message to the user indicating which model was selected.
                    TaskDialog.Show("Selected Link", $"User Selection = {selectedLinkToCheck}");
                }
            }
            // Check if the user cancelled the operation.
            if (selectedLink == null)
            {
                // Display a message to the user indicating the operation was cancelled.
                TaskDialog.Show("Operation Cancelled", "The user cancelled the operation.");

                // Return a failed result.
                return Result.Failed;
            }
            //#####################################################

            string linkNameToCheck = null;

            try
            {
                if (selectedLinkToCheck.Contains("ifc"))
                {
                    // Method to collect the upper limit parameter to set later.
                    int elevationParameterToSet = CollectParameter.CollectUpperLimitParameter(levelClass);

                    // Iterate revit link types in the project.
                    foreach (RevitLinkInstance rvtlink in rvtLinkType)
                    {
                        // Collect the RevitLinkType element.
                        RevitLinkType linkType = doc.GetElement(rvtlink.GetTypeId()) as RevitLinkType;

                        // Check if link type is not null.
                        if (linkType != null)
                        {
                            // Get the name of the link type.
                            string linkName = linkType.Name;

                            // String array to split the name of the selected link at the colon character to remove unwanted characters from the name.
                            string[] splitLinkName = linkType.Name.Split(':');

                            //  Check if the length of the split name returns more than 0 items. 
                            if (splitLinkName.Length > 0)
                            {
                                // If true, collect the item at index 0 which will be the revit model name without the unwanted characters. 
                                linkNameToCheck = splitLinkName[0];
                            }

                            // Define a bool to check if the link is loaded. 
                            bool isLoaded = RevitLinkType.IsLoaded(doc, linkType.Id);

                            // Check if link is not loaded and contains "ifc" in the name.
                            if (!isLoaded && linkNameToCheck.ToLower().Trim() == selectedLinkToCheck.ToLower().Trim())
                            {
                                // If true, output message to the user. 
                                TaskDialog.Show("Unloaded Link", $"The link {selectedLinkToCheck} is not loaded.");

                                // Throw an exception to indicate an error. 
                                throw new InvalidOperationException($"The link {linkName} is not loaded.");
                            }
                        }
                    }
                    //#####################################################

                    string rvtLinkNameToCheck = null;

                    List<Element> roomElements = new List<Element>();

                    Transform transformLinkedModel = null;

                    // Iterate through revit links in the project.
                    foreach (RevitLinkInstance rvtLink in rvtLinks)
                    {
                        // String array to split unnecessary characters from the revit model names.
                        string[] splitRvtLink = rvtLink.Name.Split(':');

                        //  Check if the length of the split name returns more than 0 items. 
                        if (splitRvtLink.Length > 0)
                        {
                            // If true, collect the item at index 0 which will be the revit model name without the unwanted characters.
                            rvtLinkNameToCheck = splitRvtLink[0];

                            // Check if rvt link name to check contains selected link to check.
                            if (rvtLinkNameToCheck.ToLower().Trim().Contains(selectedLinkToCheck.ToLower().Trim()))
                            {
                                // If true, collect the link instance. 
                                RevitLinkInstance userSelectedLink = rvtLink;

                                // Check if the user link does not equal null. 
                                if (userSelectedLink != null)
                                {
                                    // If true, execute the following code.
                                    // Collect the link document. 
                                    Document userDoc = userSelectedLink.GetLinkDocument();

                                    // Collect the model's transform. 
                                    transformLinkedModel = userSelectedLink.GetTransform();
                                    try
                                    {
                                        // Collect all structural columns elements from the linked document. 
                                        roomElements = (List<Element>)new FilteredElementCollector(userDoc)
                                            .OfCategory(BuiltInCategory.OST_GenericModel)
                                            .WhereElementIsNotElementType().ToElements();

                                        /* --- > Remove if error occurs 1 of 2.
                                        
                                        StringBuilder output = new StringBuilder();

                                        foreach (Element roomElement in roomElements)
                                        {
                                            Parameter roomNumber = roomElement.LookupParameter("LongNameOverride");
                                            output.AppendLine(roomNumber.AsValueString());
                                        }
                                        TaskDialog.Show("Room Element Check", output.ToString());

                                        ---> Remove 2 of 2 */

                                    }
                                    catch (Exception ex)
                                    {
                                        TaskDialog.Show("Error", $"Error: {ex.Message}");
                                        return Result.Failed;
                                    }
                                }
                            }
                        }
                    }
                    // Check if room elements is empty.
                    if (roomElements.Count == 0)
                    {
                        TaskDialog.Show("Room Elements", "Room elements were not found in the Generic Model Category.\n\nModify the code to try another category.");
                        return Result.Failed;
                    }

                    //#####################################################

                    List<Element> newRooms = new List<Element>();

                    string ifcLongName = null;
                    string ifcNumber = null;
                    string ifcLevel = null;

                    Level roomLevel = null;

                    // Start transaction.
                    using (Transaction transaction = new Transaction(doc, "Transfer Ifc Rooms to Revit"))
                    {
                        transaction.Start();

                        if (roomElements != null)
                        {
                            // Iterate room elements.
                            foreach (Element rooms in roomElements)
                            {
                                // Collect room parameters.
                                ParameterSet roomElemParameters = rooms.Parameters;

                                // Iterate room parameters.
                                foreach (Parameter parameter in roomElemParameters)
                                {
                                    // Collect parameter defintion names.
                                    string parameterDefinition = parameter.Definition.Name;

                                    // Check if LongNameOverride is in the parameter definition.
                                    if (parameterDefinition.Equals("LongNameOverride"))
                                    { 
                                        ifcLongName = rooms.LookupParameter("LongNameOverride").AsString();
                                    }
                                    // Check if IfcName is in the parameter definition. This is related to the room number.
                                    if (parameterDefinition.Equals("IfcName"))
                                    {
                                        ifcNumber = rooms.LookupParameter("IfcName").AsString();
                                    }
                                    // Check if LongDecomposes is in the parameter definition.
                                    if (parameterDefinition.Equals("IfcDecomposes"))
                                    {
                                        ifcLevel = rooms.LookupParameter("IfcDecomposes").AsString();
                                    }
                                }

                                // Execute the method to extract the boundary curves from the room elements.
                                List<Element> roomElementList = new List<Element> { rooms };
                                List<Curve> boundaryCurves = ExtractBoundaryCurves.ExtractIfcCurves(roomElementList);

                                if (boundaryCurves != null && boundaryCurves.Count > 0)
                                {
                                    // Iterate level class.
                                    foreach (Level level in levelClass)
                                    {
                                        // Check if ifc level is in the host revit model's name.
                                        if (level.Name.Contains(ifcLevel))
                                        {
                                            // If true, set the level element to the room level variable.
                                            roomLevel = level;

                                            // Exectue the method to collect the host view with the host level.
                                            ViewPlan view = CollectLevelView.CollectViewFromLevel(level.Name, viewPlanClass);

                                            if (view != null)
                                            {
                                                // Create a sketch plane from the level.
                                                // This will ensure that the curves (room separation lines) are associated with the specific level containing the rooms.
                                                SketchPlane sketchPlane = SketchPlane.Create(doc, roomLevel.GetPlaneReference());

                                                if (sketchPlane == null)
                                                {
                                                    TaskDialog.Show("Error", "SketchPlane creation failed.");
                                                    continue;
                                                }

                                                List<Curve> transformedCurves = new List<Curve>();

                                                // Iterate boundary curves.
                                                foreach (Curve boundaryCurve in boundaryCurves)
                                                {
                                                    // Apply a transform of the linked model to each curve.
                                                    Curve transformedCurve = boundaryCurve.CreateTransformed(transformLinkedModel) as Curve;

                                                    if (transformedCurve != null)
                                                    {
                                                        // Add the transformed curve to the list.
                                                        transformedCurves.Add(transformedCurve);
                                                    }
                                                }

                                                // Create a curve array for the new boundary lines.
                                                CurveArray curveArray = new CurveArray();

                                                // Iterate the curves from the boundary curves.
                                                foreach (Curve curve in transformedCurves)
                                                {
                                                    // Add the curves to the list.
                                                    curveArray.Append(curve);
                                                }

                                                // Create the new boundary lines using NewRoomBoundaryLine method from the api.
                                                ModelCurveArray modelCurveArray = doc.Create.NewRoomBoundaryLines(sketchPlane, curveArray, view);

                                                if (modelCurveArray == null)
                                                {
                                                    TaskDialog.Show("Error", "Failed to create room boundary lines.");
                                                    continue;
                                                }

                                                //Collect bounding boxes.
                                                BoundingBoxXYZ boundingBox = rooms.get_BoundingBox(null);

                                                // Find the center of the bounding box.
                                                XYZ bbCenter = new XYZ(
                                                    (boundingBox.Min.X + boundingBox.Max.X) / 2,
                                                    (boundingBox.Min.Y + boundingBox.Max.Y) / 2,
                                                    (boundingBox.Min.Z + boundingBox.Max.Z) / 2
                                                );

                                                // Use transform OfPoint to transfer the center of the elements bounding box from the ifc model's coordinates to the host model.
                                                // This ensures that the coordinates and XYZ values are correctly translated to the host model's coordinate system.
                                                XYZ transformedBbCenter = transformLinkedModel.OfPoint(bbCenter);

                                                // Set the rooms center to the UV point which is a parameter required to execute the new room method.
                                                UV uvCenterPoint = new UV(transformedBbCenter.X, transformedBbCenter.Y);

                                                // Create the new room.
                                                Room newRoom = doc.Create.NewRoom(roomLevel, uvCenterPoint);

                                                if (newRoom == null)
                                                {
                                                    TaskDialog.Show("Error", "Room creation failed.");
                                                    continue;
                                                }

                                                //Set the new room's Name using the ifcLongName parameter from the ifc room.
                                                newRoom.Name = $"IFC - {ifcLongName}";

                                                // Set the new room's number using the ifcNumber parameter from the ifc room. This was re-named for clarity from the "IfcName" parameter.
                                                newRoom.Number = $"IFC - {ifcNumber}";

                                                // Lookup the limit offset parameter of the new room.
                                                Parameter newRoomElevation = newRoom.LookupParameter("Limit Offset");

                                                if (newRoomElevation != null)
                                                {
                                                    // Set the limit offset parameter from the collect_elevation_parameter function called earlier in the code.
                                                    newRoomElevation.Set(elevationParameterToSet);
                                                }
                                                // Add the new room to the rooms list.
                                                newRooms.Add(newRoom);
                                            }
                                        }
                                    }
                                }
                            }
                            // Check if the new rooms count is greater than 0.
                            if (newRooms.Count > 0)
                            {
                                List<string> roomNames = new List<string>();

                                StringBuilder roomOutput = new StringBuilder();

                                // Iterate each room element in the new rooms list.
                                foreach(Element roomElem in newRooms)
                                {
                                    // Lookup the Name parameter.
                                    Parameter nameParameter = roomElem.LookupParameter("Name");

                                    // Add the parameter to the list.
                                    roomNames.Add(nameParameter.AsValueString());
                                }
                                // Sort the room names.
                                roomNames.Sort();

                                // Iterate the room names.
                                foreach(string roomName in roomNames)
                                {
                                    // Append the rooms names to the stringbuilder.
                                    roomOutput.AppendLine(roomName);
                                }

                                TaskDialog.Show("Program Complete", $"{newRooms.Count()} Ifc rooms were processed.\n\n{roomOutput}");
                            }
                            else
                            {
                                TaskDialog.Show("Error", "No rooms were created.");
                            }
                        }

                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                message = "ERROR: " + ex.Message + "\n" + ex.StackTrace;
                return Result.Failed;
            }
            if (selectedLinkToCheck.Contains("rvt"))
            {
                TaskDialog.Show("Invalid Input", $"The selected model {selectedLinkToCheck} is not an IFC model and therefore could not be processed.");
                return Result.Failed;
            }
            //#####################################################

            // Indicate that the command was successful.
            return Result.Succeeded;
        }
    }
}
