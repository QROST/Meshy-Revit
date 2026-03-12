// <author>QROST</author>
// <created>20260312</created>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using MeshyRevit.Models;

namespace MeshyRevit.Services
{
    public static class MeshyImportService
    {
        public static ElementId PlaceAsDirectShape(Document doc, ParsedMesh mesh)
        {
            var geomObjects = BuildTessellatedGeometry(mesh);
            if (geomObjects == null || geomObjects.Count == 0)
                throw new InvalidOperationException("[Meshy Revit] Failed to build tessellated geometry from mesh.");

            using (var trans = new Transaction(doc, "Meshy: Place DirectShape"))
            {
                trans.Start();

                var categoryId = new ElementId(BuiltInCategory.OST_GenericModel);
                var ds = DirectShape.CreateElement(doc, categoryId);
                ds.SetShape(geomObjects);
                ds.Name = mesh.Name ?? "Meshy_Model";

                trans.Commit();
                return ds.Id;
            }
        }

        public static ElementId PlaceAsFamily(UIApplication uiApp, ParsedMesh mesh)
        {
            var doc = uiApp.ActiveUIDocument.Document;
            var app = uiApp.Application;

            string templatePath = GetFamilyTemplatePath(app);
            string tempRfaPath = Path.Combine(Path.GetTempPath(), $"Meshy_{Guid.NewGuid():N}.rfa");

            Document familyDoc = app.NewFamilyDocument(templatePath);

            try
            {
                using (var trans = new Transaction(familyDoc, "Meshy: Build Family Geometry"))
                {
                    trans.Start();

                    var geomObjects = BuildTessellatedGeometry(mesh);
                    if (geomObjects == null || geomObjects.Count == 0)
                        throw new InvalidOperationException("[Meshy Revit] Failed to build tessellated geometry from mesh.");

                    var categoryId = new ElementId(BuiltInCategory.OST_GenericModel);
                    var ds = DirectShape.CreateElement(familyDoc, categoryId);
                    ds.SetShape(geomObjects);

                    if (familyDoc.FamilyManager.Types.Size == 0)
                        familyDoc.FamilyManager.NewType(mesh.Name ?? "Meshy_Type");
                    else
                        familyDoc.FamilyManager.RenameCurrentType(mesh.Name ?? "Meshy_Type");

                    trans.Commit();
                }

                familyDoc.SaveAs(tempRfaPath);
            }
            finally
            {
                familyDoc.Close(false);
            }

            using (var trans = new Transaction(doc, "Meshy: Load Family"))
            {
                trans.Start();

                if (!doc.LoadFamily(tempRfaPath, new MeshyFamilyLoadOptions(), out Family family))
                    throw new InvalidOperationException("[Meshy Revit] Failed to load family into project.");

                var symbolId = family.GetFamilySymbolIds().FirstOrDefault();
                if (symbolId == null || symbolId == ElementId.InvalidElementId)
                    throw new InvalidOperationException("[Meshy Revit] Family has no symbol.");

                var symbol = doc.GetElement(symbolId) as FamilySymbol;
                if (!symbol.IsActive)
                    symbol.Activate();

                var instance = doc.Create.NewFamilyInstance(
                    XYZ.Zero,
                    symbol,
                    Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

                trans.Commit();

                try { File.Delete(tempRfaPath); } catch { }

                return instance.Id;
            }
        }

        private static IList<GeometryObject> BuildTessellatedGeometry(ParsedMesh mesh)
        {
            var builder = new TessellatedShapeBuilder();
            builder.Target = TessellatedShapeBuilderTarget.AnyGeometry;
            builder.Fallback = TessellatedShapeBuilderFallback.Mesh;
            builder.OpenConnectedFaceSet(false);

            var xyzVertices = mesh.Vertices
                .Select(v => new XYZ(v.X, v.Y, v.Z))
                .ToList();

            int addedFaces = 0;
            foreach (var face in mesh.Faces)
            {
                if (face.V0 < 0 || face.V0 >= xyzVertices.Count ||
                    face.V1 < 0 || face.V1 >= xyzVertices.Count ||
                    face.V2 < 0 || face.V2 >= xyzVertices.Count)
                    continue;

                var p0 = xyzVertices[face.V0];
                var p1 = xyzVertices[face.V1];
                var p2 = xyzVertices[face.V2];

                if (p0.IsAlmostEqualTo(p1) || p1.IsAlmostEqualTo(p2) || p0.IsAlmostEqualTo(p2))
                    continue;

                var tessFace = new TessellatedFace(
                    new List<XYZ> { p0, p1, p2 },
                    ElementId.InvalidElementId);

                if (builder.DoesFaceHaveEnoughLoopsAndVertices(tessFace))
                {
                    builder.AddFace(tessFace);
                    addedFaces++;
                }
            }

            if (addedFaces == 0)
                return new List<GeometryObject>();

            builder.CloseConnectedFaceSet();
            builder.Build();

            var result = builder.GetBuildResult();
            return result.GetGeometricalObjects();
        }

        private static string GetFamilyTemplatePath(Autodesk.Revit.ApplicationServices.Application app)
        {
            string familyTemplatePath = app.FamilyTemplatePath;
            string templateFile = Path.Combine(familyTemplatePath, "Metric Generic Model.rft");

            if (File.Exists(templateFile))
                return templateFile;

            templateFile = Path.Combine(familyTemplatePath, "Generic Model.rft");
            if (File.Exists(templateFile))
                return templateFile;

            var rftFiles = Directory.GetFiles(familyTemplatePath, "*.rft");
            if (rftFiles.Length > 0)
                return rftFiles[0];

            throw new FileNotFoundException(
                $"[Meshy Revit] No family template (.rft) found in {familyTemplatePath}");
        }
    }

    internal class MeshyFamilyLoadOptions : IFamilyLoadOptions
    {
        public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
        {
            overwriteParameterValues = true;
            return true;
        }

        public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse,
            out FamilySource source, out bool overwriteParameterValues)
        {
            source = FamilySource.Family;
            overwriteParameterValues = true;
            return true;
        }
    }
}
