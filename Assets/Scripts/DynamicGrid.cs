using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RetroSunset
{
    [RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer))]
    [ExecuteAlways]
    public class DynamicGrid : MonoBehaviour
    {
        [SerializeField]float sizeX;
        [SerializeField]float sizeY;
        
        [SerializeField]int pointsX;
        [SerializeField]int pointsY;

        [SerializeField]bool use32bitMesh;
        
        [Range(0,2)]public float animationTime;

        Coroutine heightAnimation;
        
        Vector3[] vertices = null;
        int[] triangles = null;
        Vector2[] uv = null;

        Mesh mesh = null;
        
        bool invalid = false;
        
        public Bounds Bounds => mesh.bounds;
        
        public void SetHeightMap(IList<float> map, int? width = null, int? height = null, bool sizeToPoints = true)
        {
            if (width != null && height != null)
            {
                pointsY = width.Value;
                pointsX = height.Value;

                if (sizeToPoints)
                {
                    sizeX = pointsX;
                    sizeY = pointsY;
                }
            }
            
            if (animationTime > 0)
            {
                if (heightAnimation != null)
                    StopCoroutine(heightAnimation);

                heightAnimation = StartCoroutine(AnimateHeights(map));
            }
            else
            {
                UpdateMesh(map);
            }
        }

        void Start()
        {
            UpdateMesh();
        }

        void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateMesh();
#endif
        }

        void UpdateMesh(IList<float> heightMap = null)
        {
            if (!ValidateValues())
                return;

            if (mesh == null)
            {
                mesh = new Mesh();
                mesh.MarkDynamic();
                mesh.name = "Grid";

                GetComponent<MeshFilter>().mesh = mesh;
            }

            var totalVertices = pointsX * pointsY;
            var sizeChanged = !Equal(mesh.bounds, sizeX, sizeY);

            if (heightMap == null && vertices?.Length == totalVertices && !sizeChanged)
                return;

            var meshUpdated = invalid; // previously invalid - mesh was cleared.
            invalid = false;

            mesh.indexFormat = use32bitMesh ? UnityEngine.Rendering.IndexFormat.UInt32 :
                                              UnityEngine.Rendering.IndexFormat.UInt16;

            if (vertices?.Length != totalVertices)
            {
                vertices = new Vector3[totalVertices];
                uv = new Vector2[totalVertices];
                triangles = new int[(pointsX -1)*(pointsY-1)*6];

                meshUpdated = true;
                mesh.Clear();
            }

            var verticesUpdated = UpdateVertices(meshUpdated, heightMap);
            
            if (verticesUpdated)
            {
                mesh.vertices = vertices;
            
                if (meshUpdated)
                {
                    UpdateTriangles();
                    mesh.triangles = triangles;
                    mesh.uv = uv;
                }

                mesh.RecalculateNormals();
            
                if (sizeChanged)
                    mesh.RecalculateBounds();
            }
        }

        bool ValidateValues()
        {
            if (pointsX <= 1 || pointsY <= 1 || sizeX <= 0 || sizeY <= 0)
            {
                if (!invalid)
                {
                    invalid = true;
                    mesh.Clear(false);
                    Debug.LogError("Invalid dimensions");
                }
                return false;
            }

            if (!use32bitMesh && pointsX * pointsY > ushort.MaxValue)
            {
                if (!invalid)
                {
                    invalid = true;
                    mesh?.Clear(false);
                    Debug.LogError("16 bit mesh - vertex count must not exceed " + ushort.MaxValue);
                }
                return false;
            }

            return true;
        }

        bool UpdateVertices(bool meshUpdated, IList<float> heightMap)
        {
            var verticesUpdated = meshUpdated;

            var xStep = sizeX / (float)(pointsX - 1);
            var yStep = sizeY / (float)(pointsY - 1);

            var offsetX = sizeX / 2f;
            var offsetY = sizeY / 2f;

            if (heightMap != null && heightMap.Count != vertices.Length)
                heightMap = null;
                
            for (var j = 0; j < pointsY; j++)
            {
                var y = j * yStep - offsetY;

                for (var i = 0; i < pointsX; i++)
                {
                    var ix = j * pointsX + i;

                    var x = i * xStep - offsetX;
                    var h = heightMap == null ? 0 : heightMap[ix];
                    var v = new Vector3(x, h, y);

                    if (v != vertices[ix])
                    {
                        vertices[ix] = v;
                        verticesUpdated = true;
                    }
                    
                    if (meshUpdated)
                    {
                        uv[i].x = i / (float)pointsX;
                        uv[i].y = j / (float)pointsY;
                    }
                }
            }

            return verticesUpdated;
        }

        void UpdateTriangles()
        {
            for (int ti = 0, vi = 0, y = 0; y < pointsY - 1; y++, vi++)
            {
                for (int x = 0; x < pointsX - 1; x++, ti += 6, vi++)
                {
                    triangles[ti] = vi;
                    triangles[ti + 1] = triangles[ti + 4] = vi + pointsX;
                    triangles[ti + 2] = triangles[ti + 3] = vi + 1;
                    triangles[ti + 5] = vi + pointsX + 1;

                    // Debug.Log(vertices[vi].z + " " + triangles[ti] + ":" + triangles[ti + 1] + ":" + triangles[ti + 2] + " & " + triangles[ti + 3] + ":" + triangles[ti + 4] + ":" + triangles[ti + 5]);
                }
            }
        }

        IEnumerator AnimateHeights(IList<float> heightMap)
        {
            var time = 0f;
            var curve = new AnimationCurve[heightMap.Count];
            var heights = new float[heightMap.Count];

            for (var i = 0; i < heightMap.Count; i++)
            {
                var oldheight = heightMap.Count == vertices.Length ? vertices[i].y : 0;
                curve[i] = AnimationCurve.Linear(0f, oldheight, animationTime, heightMap[i]);
            }

            while (time < animationTime)
            {
                for (var i = 0; i < heightMap.Count; i++)
                {
                    heights[i] = curve[i].Evaluate(time); 
                }

                UpdateMesh(heights);
                time += Time.deltaTime;
                yield return null;
            }

            heightAnimation = null;
        }

        private static bool Equal(Bounds bounds, float width, float height)
        {
            return Mathf.Abs(bounds.size.x - width) < 0.001f &&
                   Mathf.Abs(bounds.size.z - height) < 0.001f;
        }
    }
}
