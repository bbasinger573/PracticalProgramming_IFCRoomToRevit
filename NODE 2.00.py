#Author = 'Billy Basinger'

# Load the Python Standard and DesignScript Libraries
import sys
import clr
clr.AddReference('ProtoGeometry')
from Autodesk.DesignScript.Geometry import *

clr.AddReference('RevitAPI')
import Autodesk
from Autodesk.Revit.DB import *

clr.AddReference('RevitServices')
import RevitServices

from RevitServices.Persistence import DocumentManager

clr.AddReference('RevitServices')
from RevitServices.Persistence import DocumentManager
from RevitServices.Transactions import TransactionManager

from System.Collections.Generic import List

import traceback

# The inputs to this node will be stored as a list in the IN variables.

#####################################################

doc = DocumentManager.Instance.CurrentDBDocument

input = UnwrapElement(IN[0])

#####################################################

levels_category = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Levels).WhereElementIsNotElementType().ToElements()

level_class = FilteredElementCollector(doc).OfClass(Level)

room_category = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().ToElements()

rvt_links = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_RvtLinks).WhereElementIsNotElementType().ToElements()

rvt_link_type = FilteredElementCollector(doc).OfClass(RevitLinkType)

view_plan_class = FilteredElementCollector(doc).OfClass(ViewPlan).WhereElementIsNotElementType().ToElements()

#####################################################

# Function to extract boundary curves from direct shapes.
def extract_ifc_curves(direct_shapes):
    boundary_curves = []
    for direct_shape in direct_shapes:
        # Collect the geometry of the direct shape.
        # Use Options to extract the geometry.  
        geometry_element = direct_shape.get_Geometry(Options())
        # Iterate each geometry element.
        for geometry_object in geometry_element:
            # Check if the geometry object is a geometry instance.
            if isinstance(geometry_object, GeometryInstance):
                # If true, collect the symbole geometry of the geometry instance.
                # Use GetSymbol Geometry to access geometric details, like faces.
                symbol_geometry = geometry_object.GetSymbolGeometry()
                # Iterate each symbol geometry.
                for geom_instance in symbol_geometry:
                    # Check if the symbol geometry is a solid.
                    if isinstance(geom_instance, Solid):
                        # If true, iterate the faces of the solid geometry.
                        for face in geom_instance.Faces:
                            # Collect the edges of the faces as curve loops.
                            loops = face.GetEdgesAsCurveLoops()
                            # Iterate each loop.
                            for loop in loops:
                                # Iterate each curve in the curve loop.
                                for curve in loop:
                                    # Add the curve to the curve loop.
                                    boundary_curves.append(curve)
                    else:
                        # Return the message below if the geometry instance is not a solid.
                        # The error will return the type of unhandled instance.
                        return f"Unhandled geometry instance type: {type(geom_instance)}"
            else:
                # Return the message below if the geometry is not a geometry instance.
                # The error will return the type of unhandled object.
                return f"Unhandled geometry object type: {type(geometry_object)}"
    
    if not boundary_curves:
        return "No boundary curves extracted."
    # Return the list of boundary curves.
    return boundary_curves

#####################################################

# Function to collect a view associated with a level.
def collect_view_from_level(level_name, view_plan):
    # Iterate view plan class.
    for view in view_plan:
        # Check if the view name is equal to the level names and that the view discipline is equal to architectural.
        if level_name in view.Name and view.get_Parameter(BuiltInParameter.VIEW_DISCIPLINE).AsValueString() == "Architectural":
            # If true, return the view element.
            return view
    return None

#####################################################

def collect_upper_limit_parameter(levels):
    elevations_list = []
    # Iterate level class:
    for level in levels:
        # Collect level parameters.
        parameters = level.Parameters
        # Iterate parameters.
        for param in parameters:
            # Collect parameter definition names.
            parameter_definition = param.Definition.Name
            # Check if Elevation is in the parameter definition names.
            if "Elevation" in parameter_definition:
                # If true, look up the parameter in the level.
                elevation = level.LookupParameter("Elevation").AsValueString()
                # Split the value to isolate the first number.
                elevation = elevation.split("'")[0]
                # Append the integer value of elevation.
                elevations_list.append(int(elevation))
                # Sorte the list in ascending order. 
                sorted_elevations_list = sorted(elevations_list)

    # Outer loop - iterate the elements in the sorted elevations list.
    for i in range(len(sorted_elevations_list)):
        # i starts at index 0 and goes up to the last index in the list.
        elevation1 = sorted_elevations_list[i]
        # Inner loop - iterate over the elements in the sorted elevations list starting after index 0.
        for j in range(i + 1, len(sorted_elevations_list)):
            # j starts at i + 1 to ensure that j only considers elements after i to compare all items.
            elevation2 = sorted_elevations_list[j]
            # Check if i - j equals j.
            if abs(elevation1 - elevation2) == elevation2:
                # If true, set the integer of elevation2 to the upper limit variable. 
                # The structure of the indents at this location will produce a single number in the output of the upper limit variable. 
                upper_limit = elevation2
                break

    return upper_limit
   
#####################################################

try:
    if "ifc" in input:
        # Execute function to collect the upper limit parameter to set later.
        elevation_parameter_to_set = collect_upper_limit_parameter(level_class)
                
        # Iterate revit link types.
        for linktype in rvt_link_type:
            # Check if link is not None and is an instance.
            if linktype is not None and isinstance(linktype, RevitLinkType):
                # Check if there are linked models that are not loaded.
                if not RevitLinkType.IsLoaded(doc, linktype.Id):
                    # Get ids of link types that are not loaded.
                    unloaded_links = linktype.Id
                    # Get the link elements.
                    link_elem = doc.GetElement(unloaded_links)
                    # Check if ifc is in link elements name. 
                    if "ifc" in link_elem.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_NAME).AsString().lower():
                        # If IFC is in model, but not loaded, output message to user.
                        raise RuntimeError("The selected IFC link is not loaded. ", link_elem.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_NAME).AsString())

        #####################################################
                      
        # Iterate through revit links.
        for link in rvt_links:
            # Check if the user selected model is is in the names of the revit links in the model. 
            # Use split to remove some unnecessary componenets of the link names. 
            if "ifc" in link.Name:
                # Get Link.
                user_link = link
                # Get document from link.
                user_doc = user_link.GetLinkDocument()
                # Get the linked model's transform. 
                transform_linked_model = user_link.GetTransform()
                # Check for room elements in the linked document. Rooms from the ifc model are present in the model under the generic model category.
                room_elements = FilteredElementCollector(user_doc).OfCategory(BuiltInCategory.OST_GenericModel).WhereElementIsNotElementType().ToElements()
                # Check room elements.
                if not room_elements:
                    # If not room elements, output a message to the user.
                    raise RuntimeError("Room elements were not found in the Generic Model Category.\n\nModify the code to try another category.")
                
        #####################################################

        TransactionManager.Instance.EnsureInTransaction(doc)
        
        new_rooms = []
        output_message = None

        if room_elements:
            # Iterate room elements:
            for rooms in room_elements:
                # Collect room parameters.
                room_elem_parameters = rooms.Parameters
                # Iterate room parameters.
                for parameters in room_elem_parameters:
                    # Collect parameter definition names.
                    parameter_definitions = parameters.Definition.Name
                    # Check if LongNameOverride is in the parameter deifnitions.
                    if "LongNameOverride" in parameter_definitions:
                        ifcLongName = rooms.LookupParameter("LongNameOverride").AsString()
                    # Check if IfcName is in the parameter deifnitions. This is related to the room number.
                    if "IfcName" in parameter_definitions:
                        ifcNumber = rooms.LookupParameter("IfcName").AsString()
                    # Check if LongDecomposes is in the parameter deifnitions.
                    if "IfcDecomposes" in parameter_definitions:
                        ifcLevel = rooms.LookupParameter("IfcDecomposes").AsString()
                
                # Execute the function to extract the boundary curves from the room elements.
                boundary_curves = extract_ifc_curves([rooms])
                
                if boundary_curves:
                    # Iterate level class.
                    for level in level_class:
                        # Check if ifc level is in host revit model's name. 
                        if ifcLevel in level.Name:
                            # If true, set the level element to the room level variable.
                            room_level = level
                            # Execute the function to collect the host view that contains the host level's name.
                            view = collect_view_from_level(level.Name, view_plan_class)
                            
                            if view:
                                # Create a sketch plane from the level.
                                # This will ensure that the curves (room separation lines) are associated with the specific level containing the rooms.
                                sketch_plane = SketchPlane.Create(doc, room_level.GetPlaneReference())
                                
                                transformed_curves = []
                                # Iterate boundary curves.
                                for boundary_curve in boundary_curves:
                                    # Apply a transform of the linked model to each curve.
                                    transformed_curve = boundary_curve.CreateTransformed(transform_linked_model)
                                    # Add the transformed curves to the list.
                                    transformed_curves.append(transformed_curve)

                                # Create a CurveArray for the new room boundary lines
                                curve_array = CurveArray()
                                # Iterate the curves from boundary curves.
                                for curve in transformed_curves:
                                    # Append the curves to the list.
                                    curve_array.Append(curve)
                                    
                                # Create the new boundary lines using NewRoomBoundaryLines method from the API.
                                model_curve_array = doc.Create.NewRoomBoundaryLines(sketch_plane, curve_array, view)
                                
                                # Collect the room bounding boxe
                                bounding_box = rooms.get_BoundingBox(None)
                                # Find the center of the bounding box
                                bb_center = XYZ((bounding_box.Min.X + bounding_box.Max.X) / 2, (bounding_box.Min.Y + bounding_box.Max.Y) / 2, bounding_box.Min.Z)
                                # Use transform OfPoint to transfer the center of the elements bounding box from the ifc model's coordinates to the host model.
                                # This ensures that the coordinates and XYZ values are correctly translated to the host model's coordinate system.
                                transformed_bb_center = transform_linked_model.OfPoint(bb_center)
                                # Set the rooms center to the UV point which is a parameter required to execute the new room method.
                                uv_center_point = UV(transformed_bb_center.X, transformed_bb_center.Y)

                                # Create the new room.
                                new_room = doc.Create.NewRoom(room_level, uv_center_point)
                                # Set the new room's Name using the ifcLongName parameter from the ifc room.
                                new_room.Name = f"IFC - {ifcLongName}"
                                # Set the new room's number using the ifcNumber parameter from the ifc room. This was re-named for clarity from the "IfcName" parameter.
                                new_room.Number = f"IFC - {ifcNumber}"
                                # Lookup the limit offset parameter of the new room.
                                new_room_elevation = new_room.LookupParameter("Limit Offset")
                                # Set the limit offset parameter from the collect_elevation_parameter function called earlier in the code.
                                new_room_elevation.Set(elevation_parameter_to_set)
                                # Append the new room to the new rooms list.
                                new_rooms.append(new_room)
                                # If successful, add the message below to the output message variable.
                                output_message = f"{(str(len(new_rooms)))} Ifc rooms were processed."
                
        TransactionManager.Instance.TransactionTaskDone()

        #####################################################

except:
    # If an error occurs, output the message below. 
    OUT = "ERROR: See Below for more information.\n\n" + "\n\n".join(traceback.format_exc().splitlines())
    raise RuntimeError(OUT)

#####################################################

# Assign your output to the OUT variable.
if "ifc" not in input and "rvt" not in input:
    OUT = input
    
elif "rvt" in input:
    OUT = f"\n\nThe selected model {input} is not an Ifc model and therefore could not be processed.\n\n"
    raise RuntimeError(OUT)

else:
    OUT = output_message, new_rooms
