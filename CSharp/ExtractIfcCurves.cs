using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IfcRoomToRvtRoom
{
    public static class ExtractBoundaryCurves
    {
        public static List<Curve> ExtractIfcCurves(List<Element> directShapeElementList)
        {
            List<Curve> boundaryCurves = new List<Curve>();

            foreach (Element directShape in directShapeElementList)
            {
                // Create an options object for geometry extraction.
                Options options = new Options();

                // Collect the geometry of the direct shape.
                GeometryElement geometryElement = directShape.get_Geometry(options);

                // Iterate each geometry element.
                foreach (GeometryObject geometryObject in geometryElement)
                {
                    try
                    {

                        // Check if the geometry object is a geometry instance.
                        if (geometryObject is GeometryInstance geometryInstance)
                        {
                            // If true, collect the symbole geometry of the geometry instance.
                            // Use GetSymbol Geometry to access geometric details, like faces.
                            GeometryElement symbolGeometry = geometryInstance.GetSymbolGeometry();

                            // Iterate each symbol geometry.
                            foreach (GeometryObject geomInstance in symbolGeometry)
                            {

                                // Check if the symbold geometry is a solid.
                                if (geomInstance is Solid solid)
                                {
                                    // If true, iterate the faces of the solid geometry.
                                    foreach (Face face in solid.Faces)
                                    {
                                        // Collect the edges of the faces as curve loops.
                                        IList<CurveLoop> loops = face.GetEdgesAsCurveLoops();

                                        // Iterate each loop.
                                        foreach (CurveLoop loop in loops)
                                        {
                                            // Iterate each curve in the curve loop.
                                            foreach (Curve curve in loop)
                                            {
                                                // Add the curves to the list.
                                                boundaryCurves.Add(curve);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // Return the message below if the geometry instance is not a solid.
                                    // The error will return the type of unhandled instance.
                                    throw new InvalidOperationException($"Unhandled geometry instance type: {geomInstance.GetType()}");
                                }
                            }
                        }
                        else
                        {
                            // Return the message below if the geometry is not a geometry instance.
                            // The error will return the type on unhandled object.
                            throw new InvalidOperationException($"Unhandled geometry object type: {geometryObject.GetType()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Error", $"Error processing direct shape: {ex.Message}");
                    }
                }
            }
            if (boundaryCurves.Count == 0)
            {
                throw new InvalidOperationException("No boundary curves were extracted.");
            }
            // Return the list of boundary curves.
            return boundaryCurves;
        }
    }
}
