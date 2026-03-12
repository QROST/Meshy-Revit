// <author>QROST</author>
// <created>20260312</created>

using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using MeshyRevit.Services;
using MeshyRevit.UI;

namespace MeshyRevit
{
    [Transaction(TransactionMode.Manual)]
    public class MeshyCommand : IExternalCommand
    {
        private static MeshyGeneratorWindow _window;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiApp = commandData.Application;

            try
            {
                if (_window != null && _window.IsLoaded)
                {
                    _window.Activate();
                    return Result.Succeeded;
                }

                if (!MeshySettingsService.HasApiKey())
                {
                    var settingsWindow = new MeshySettingsWindow();
                    settingsWindow.ShowDialog();

                    if (!MeshySettingsService.HasApiKey())
                    {
                        message = "[Meshy Revit] A valid Meshy API key is required to use this tool.";
                        return Result.Cancelled;
                    }
                }

                _window = new MeshyGeneratorWindow(uiApp);
                _window.Closed += (s, args) => _window = null;
                _window.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
