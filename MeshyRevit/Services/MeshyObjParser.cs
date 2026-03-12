// <author>QROST</author>
// <created>20260312</created>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MeshyRevit.Models;

namespace MeshyRevit.Services
{
    public static class MeshyObjParser
    {
        private const double _metersToFeet = 3.280839895013123;

        public static ParsedMesh Parse(string objContent, string name = "Meshy_Model")
        {
            var mesh = new ParsedMesh { Name = name };
            var lines = objContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();

                if (line.StartsWith("v "))
                    ParseVertex(line, mesh.Vertices);
                else if (line.StartsWith("f "))
                    ParseFace(line, mesh.Faces, mesh.Vertices.Count);
            }

            return mesh;
        }

        public static ParsedMesh ParseFile(string filePath, string name = null)
        {
            string content = File.ReadAllText(filePath);
            return Parse(content, name ?? Path.GetFileNameWithoutExtension(filePath));
        }

        private static void ParseVertex(string line, List<MeshVertex> vertices)
        {
            string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) return;

            if (double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double y) &&
                double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
            {
                vertices.Add(new MeshVertex(x * _metersToFeet, y * _metersToFeet, z * _metersToFeet));
            }
        }

        private static void ParseFace(string line, List<MeshFace> faces, int vertexCount)
        {
            string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) return;

            var indices = new List<int>();
            for (int i = 1; i < parts.Length; i++)
            {
                string indexPart = parts[i].Split('/')[0];
                if (int.TryParse(indexPart, out int idx))
                {
                    int resolved = idx > 0 ? idx - 1 : vertexCount + idx;
                    indices.Add(resolved);
                }
            }

            for (int i = 1; i < indices.Count - 1; i++)
            {
                faces.Add(new MeshFace(indices[0], indices[i], indices[i + 1]));
            }
        }
    }
}
