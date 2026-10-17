using System.Collections.Generic;
using UnityEngine;

using EchoEdge.Domain.Battle;

namespace EchoEdge.Presenter.Battle
{
    /// <summary>
    /// エナジーウォール用の斜面メッシュを生成するクラス。
    /// 1マス(1x1x1、中心が原点)の対角線に沿った薄い板を作る。
    /// 一閃の反射は Raycast の hit.normal を使うため、板の側面の法線がそのまま斜めの反射面になる。
    /// </summary>
    public static class SlopeWallMeshBuilder
    {
        /// <summary>
        /// 斜面の板の厚さ
        /// </summary>
        public const float Thickness = 0.12f;

        /// <summary>
        /// 斜面の板の高さ（元の壁のキューブと同じ）
        /// </summary>
        private const float Height = 1.0f;

        private static readonly Dictionary<EnergyWallDirection, Mesh> Cache = new();

        /// <summary>
        /// 指定した向きの斜面メッシュを取得する。同じ向きのメッシュは使い回す。
        /// </summary>
        public static Mesh Get(EnergyWallDirection direction)
        {
            if (Cache.TryGetValue(direction, out var cached) && cached != null)
            {
                return cached;
            }

            var mesh = Build(direction);
            Cache[direction] = mesh;
            return mesh;
        }

        private static Mesh Build(EnergyWallDirection direction)
        {
            // 板の長さ方向（xz平面上の対角線）と、それに垂直な厚さ方向
            Vector3 along = direction == EnergyWallDirection.Slash
                ? new Vector3(1f, 0f, 1f).normalized
                : new Vector3(1f, 0f, -1f).normalized;
            Vector3 across = Vector3.Cross(Vector3.up, along).normalized;

            // 端が隣のマスにはみ出さないよう、厚さの分だけ短くする
            float halfLength = (Mathf.Sqrt(2f) - Thickness) * 0.5f;
            float halfThickness = Thickness * 0.5f;
            float halfHeight = Height * 0.5f;

            Vector3 a = along * halfLength;
            Vector3 c = across * halfThickness;
            Vector3 h = Vector3.up * halfHeight;

            Vector3[] corners =
            {
                -a - c - h, a - c - h, a + c - h, -a + c - h, // 下面
                -a - c + h, a - c + h, a + c + h, -a + c + h, // 上面
            };

            var vertices = new List<Vector3>(24);
            var normals = new List<Vector3>(24);
            var uvs = new List<Vector2>(24);
            var triangles = new List<int>(36);

            // 面ごとに頂点を分けてフラットシェーディングにする
            AddFace(corners[4], corners[5], corners[6], corners[7], Vector3.up);
            AddFace(corners[0], corners[1], corners[2], corners[3], Vector3.down);
            AddFace(corners[0], corners[1], corners[5], corners[4], -across);
            AddFace(corners[2], corners[3], corners[7], corners[6], across);
            AddFace(corners[1], corners[2], corners[6], corners[5], along);
            AddFace(corners[3], corners[0], corners[4], corners[7], -along);

            var mesh = new Mesh
            {
                name = $"EnergyWallSlope_{direction}"
            };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;

            void AddFace(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal)
            {
                int start = vertices.Count;
                vertices.Add(p0);
                vertices.Add(p1);
                vertices.Add(p2);
                vertices.Add(p3);
                for (int i = 0; i < 4; i++) normals.Add(normal);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));

                // Unity は Cross(b - a, c - a) の向きが表面になる。法線と揃うよう巻き順を決める。
                Vector3 faceNormal = Vector3.Cross(p1 - p0, p2 - p0);
                if (Vector3.Dot(faceNormal, normal) > 0f)
                {
                    triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                    triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
                }
                else
                {
                    triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
                    triangles.Add(start); triangles.Add(start + 3); triangles.Add(start + 2);
                }
            }
        }
    }
}
