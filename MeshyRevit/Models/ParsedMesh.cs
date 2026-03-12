// <author>QROST</author>
// <created>20260312</created>

using System.Collections.Generic;

namespace MeshyRevit.Models
{
    public class ParsedMesh
    {
        public List<MeshVertex> Vertices { get; set; } = new List<MeshVertex>();
        public List<MeshFace> Faces { get; set; } = new List<MeshFace>();
        public string Name { get; set; }

        public int VertexCount => Vertices.Count;
        public int FaceCount => Faces.Count;
    }

    public struct MeshVertex
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public MeshVertex(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public struct MeshFace
    {
        public int V0 { get; set; }
        public int V1 { get; set; }
        public int V2 { get; set; }

        public MeshFace(int v0, int v1, int v2)
        {
            V0 = v0;
            V1 = v1;
            V2 = v2;
        }
    }
}
