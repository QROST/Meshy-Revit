// <author>QROST</author>
// <created>20260312</created>

using System;
using System.Reflection;
using Autodesk.Revit.UI;
using MeshyRevit.Handlers;

namespace MeshyRevit
{
    public class App : IExternalApplication
    {
        public static MeshyPlacementHandler PlacementHandler { get; private set; }
        public static ExternalEvent PlacementEvent { get; private set; }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                PlacementHandler = new MeshyPlacementHandler();
                PlacementEvent = ExternalEvent.Create(PlacementHandler);

                CreateRibbonPanel(application);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Meshy Revit - Startup Error",
                    $"Failed to initialize Meshy Revit plugin:\n\n{ex.Message}");
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private void CreateRibbonPanel(UIControlledApplication application)
        {
            var panel = application.CreateRibbonPanel("Meshy");
            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            var buttonData = new PushButtonData(
                "MeshyCommand",
                "Meshy 3D\nGenerator",
                assemblyPath,
                "MeshyRevit.MeshyCommand");

            buttonData.ToolTip = "Generate 3D models from text or images using Meshy AI";
            buttonData.LongDescription =
                "Opens the Meshy 3D Generator window where you can create 3D models from text prompts, " +
                "images, or multiple images. Generated models can be placed as DirectShapes or loaded as Families.";

            panel.AddItem(buttonData);
        }
    }
}
