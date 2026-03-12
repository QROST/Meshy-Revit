# Meshy Revit Plugin

A Revit plugin that enables AI-powered 3D model generation from [Meshy](https://www.meshy.ai/) directly inside Autodesk Revit. Generate 3D meshes from text prompts or reference images and place them into your Revit project as DirectShape elements or loadable Family instances (.rfa).

## Features

* **Text to 3D**: Generate 3D models from text descriptions (two-stage preview + refine workflow)
* **Image to 3D**: Create 3D models from a single reference image (.jpg, .png)
* **Multi-Image to 3D**: Provide 1-4 images of the same object from different angles for higher-quality results
* **DirectShape Placement**: Place generated meshes directly into the active Revit document as Generic Model elements
* **Family Creation**: Save generated meshes as loadable Revit families (.rfa) and place as FamilyInstance elements
* **Multi-Version Support**: Targets Revit 2024, 2025, and 2026
* **Configurable Generation**: Control topology, polycount, AI model version, and PBR map generation

## Requirements

* Autodesk Revit 2024, 2025, or 2026
* [Meshy account](https://www.meshy.ai/) with an API key ([get one here](https://www.meshy.ai/settings/api))
* Visual Studio 2022 (for building from source)

## Installation

### Option 1: Build from Source (Recommended)

1. Clone this repository:
   ```
   git clone https://github.com/qrost/meshy-revit-plugin.git
   ```
2. Open `Meshy-Revit.sln` in Visual Studio 2022
3. Select the build configuration for your Revit version (see [Build Configurations](#build-configurations))
4. Build the solution -- the post-build event automatically deploys to the Revit Addins folder

### Option 2: Manual Install

1. Download the release for your Revit version
2. Copy `MeshyRevit.addin` to `%APPDATA%\Autodesk\REVIT\Addins\{version}\`
3. Copy `MeshyRevit.dll` and dependencies to `%APPDATA%\Autodesk\REVIT\Addins\{version}\MeshyRevit\`
4. Restart Revit

## Usage

### Setting Up the API Key

1. In Revit, go to the **Meshy** ribbon tab
2. Click **Meshy 3D Generator**
3. On first launch, click **Settings** and enter your Meshy API key (`msy_...`)
4. The key is stored locally at `%APPDATA%\MeshyRevit\settings.json`

### Generating a 3D Model

1. Open the **Meshy 3D Generator** window from the ribbon
2. Choose a tab: **Text-to-3D**, **Image-to-3D**, or **Multi-Image-to-3D**
3. Enter your prompt or select image(s)
4. Configure generation options:
   - **AI Model**: Meshy-6 (latest) or Meshy-5
   - **Topology**: Triangle or Quad mesh
   - **Target Polycount**: 100 -- 300,000
   - **PBR Maps**: Enable metallic, roughness, and normal map generation
5. Select output format: **DirectShape** or **Family (.rfa)**
6. Click **Generate** -- the plugin submits the task, shows real-time progress, and places the result in Revit

## How It Works

The plugin follows an async-safe pattern required by the Revit API:

1. User input is collected in a **modeless WPF window** (Revit remains interactive)
2. API calls and model download run on a **background thread**
3. The downloaded OBJ file is parsed into vertices and triangle faces (with meter-to-feet conversion)
4. Geometry creation is marshaled to the Revit main thread via `IExternalEventHandler` + `ExternalEvent`
5. Mesh triangles are built using `TessellatedShapeBuilder` and placed as a DirectShape or within a new Family document

## Build Configurations

The project uses build configurations to target multiple Revit versions:

| Configuration | Revit Version | .NET Target |
|--------------|---------------|-------------|
| `Debug-R2024` / `Release-R2024` | Revit 2024 | .NET Framework 4.8 |
| `Debug-R2025` / `Release-R2025` | Revit 2025 | .NET 8.0 |
| `Debug-R2026` / `Release-R2026` | Revit 2026 | .NET 8.0 |

```bash
# Build for a specific Revit version
msbuild Meshy-Revit.sln /p:Configuration=Release-R2024
msbuild Meshy-Revit.sln /p:Configuration=Release-R2025
msbuild Meshy-Revit.sln /p:Configuration=Release-R2026
```

## Troubleshooting

**Plugin does not appear in Revit:**
* Verify the `.addin` file is in `%APPDATA%\Autodesk\REVIT\Addins\{version}\`
* Check that the DLL path in the `.addin` file matches the actual file location
* Look at the Revit journal file for load errors

**API key rejected:**
* Ensure your key starts with `msy_` and has not been revoked
* Check your internet connection
* Verify credits are available on your Meshy account

**Mesh placement fails:**
* Ensure a project document (not a family document) is open
* Check the Revit status bar and journal for error details
* Very high polycount meshes may need the target reduced

**Build errors for a Revit version:**
* Confirm the matching `Autodesk.Revit.SDK` NuGet package version is available
* For Revit 2024: requires .NET Framework 4.8 SDK
* For Revit 2025/2026: requires .NET 8 SDK

## API Reference

This plugin uses the [Meshy REST API](https://docs.meshy.ai/):

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/openapi/v2/text-to-3d` | POST | Create text-to-3D task (preview or refine) |
| `/openapi/v2/text-to-3d/:id` | GET | Retrieve task status and model URLs |
| `/openapi/v1/image-to-3d` | POST | Create image-to-3D task |
| `/openapi/v1/image-to-3d/:id` | GET | Retrieve task status and model URLs |
| `/openapi/v1/multi-image-to-3d` | POST | Create multi-image-to-3D task |
| `/openapi/v1/multi-image-to-3d/:id` | GET | Retrieve task status and model URLs |

Authentication: `Authorization: Bearer msy_...` header on all requests.

## Contributing

Contributions are welcome. Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/YourFeature`)
3. Commit your changes (`git commit -m 'Add YourFeature'`)
4. Push to the branch (`git push origin feature/YourFeature`)
5. Open a Pull Request

## Changelog

### Version 0.1.0 (Current)

* Initial release
* Text-to-3D with preview + refine workflow
* Image-to-3D single image generation
* Multi-Image-to-3D (1-4 images)
* DirectShape and Family (.rfa) output modes
* Multi-version support for Revit 2024, 2025, 2026
* Configurable topology, polycount, AI model, and PBR options
* Modeless WPF UI with real-time progress tracking
* API key persistence to user AppData

## License

Proprietary. All rights reserved by QROST.

## Support

* Meshy Documentation: [docs.meshy.ai](https://docs.meshy.ai/)
* Issues: Report via the repository issue tracker

---

Made by QROST
