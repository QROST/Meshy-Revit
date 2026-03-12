// <author>QROST</author>
// <created>20260312</created>

using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using MeshyRevit.Models;
using MeshyRevit.Services;

namespace MeshyRevit.Handlers
{
    public class MeshyPlacementHandler : IExternalEventHandler
    {
        public ParsedMesh Mesh { get; set; }
        public PlacementMode Mode { get; set; } = PlacementMode.DirectShape;

        public event Action<ElementId> OnSuccess;
        public event Action<string> OnError;

        public void Execute(UIApplication app)
        {
            if (Mesh == null)
            {
                OnError?.Invoke("[Meshy Revit] No mesh data available for placement.");
                return;
            }

            try
            {
                ElementId resultId;

                switch (Mode)
                {
                    case PlacementMode.DirectShape:
                        resultId = MeshyImportService.PlaceAsDirectShape(
                            app.ActiveUIDocument.Document, Mesh);
                        break;

                    case PlacementMode.Family:
                        resultId = MeshyImportService.PlaceAsFamily(app, Mesh);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                OnSuccess?.Invoke(resultId);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"[Meshy Revit] {ex.Message}");
            }
            finally
            {
                Mesh = null;
            }
        }

        public string GetName()
        {
            return "MeshyRevit.MeshyPlacementHandler";
        }
    }
}
