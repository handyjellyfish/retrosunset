using System;
using System.Collections;
using System.Collections.Generic;
using HandyJellyfish.Audio;
using UnityEngine;

namespace RetroSunset
{
    [ExecuteAlways]
    public class ReactiveMountains : MonoBehaviour
    {
        [SerializeField] AudioProcessor processor;
        [SerializeField] int sampleSize;

        [SerializeField] int animateEvery;
        [SerializeField] AnimationCurve animationCurve;

        [SerializeField] bool invert = false;

        [SerializeField] float speed;
        [SerializeField] float resetPoint;

        [SerializeField] DynamicGrid[] rightGrids;
        [SerializeField] DynamicGrid[] leftGrids;

        float[] heightMap;
        Coroutine heightAnimation;

        int gridSize;
        
        void Start()
        {
            InitializeGrids();
            processor.OnSample.AddListener(SamplesReceived);
        }

        void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                InitializeGrids();
                return;
            }
#endif
            MoveGrids(leftGrids, -1);
            MoveGrids(rightGrids, 1);
        }

        void InitializeGrids()
        {
            processor.SampleBands = sampleSize;
            gridSize = sampleSize + 1;

            var mapSize = gridSize * gridSize;
            heightMap = new float[mapSize];

            for (var i = 0; i < mapSize; i++)
                heightMap[i] = 0;

            SetHeightMap(heightMap, gridSize);
        }

        void MoveGrids(DynamicGrid[] grids, int direction)
        {
            var moveTowards = new Vector3(direction * (resetPoint + grids[0].Bounds.max.x), 0, 0);

            var overshotIx = -1;

            for (var i = 0; i < grids.Length; i++)
            {
                var grid = grids[i];
                var step = speed * Time.deltaTime;
                grid.transform.localPosition = Vector3.MoveTowards(grid.transform.localPosition, moveTowards, step);

                if (Vector3.Distance(grid.transform.localPosition, moveTowards) < 0.001f)
                    overshotIx = i;
            }

            if (overshotIx >= 0)
            {
                var lastGrid = grids[overshotIx > 0 ? overshotIx - 1 : grids.Length - 1];
                var pos = lastGrid.transform.localPosition;

                grids[overshotIx].transform.localPosition = new Vector3(pos.x - direction * lastGrid.Bounds.size.x, 0, 0);
            }
        }

        void SamplesReceived(float[] sampleData)
        {
            if (sampleData.Length < sampleSize || heightAnimation != null)
                return;
            
            float minValue = float.MaxValue;

            for (int i = 0; i < sampleSize; i++)
            {
                if (sampleData[i] < minValue)
                    minValue = sampleData[i];
                
                // if (i == 0 || i == samples - 1)
                //     continue;

                // Debug.DrawLine(new Vector3(i - 1, sampleData[i], 0), new Vector3(i, sampleData[i + 1], 0), Color.red);
                // Debug.DrawLine(new Vector3(i - 1, Mathf.Log(sampleData[i - 1]) + 10, 2), new Vector3(i, Mathf.Log(sampleData[i]) + 10, 2), Color.cyan);
                // Debug.DrawLine(new Vector3(Mathf.Log(i - 1), sampleData[i - 1] - 10, 1), new Vector3(Mathf.Log(i), sampleData[i] - 10, 1), Color.green);
                // Debug.DrawLine(new Vector3(Mathf.Log(i - 1), Mathf.Log(sampleData[i - 1]), 3), new Vector3(Mathf.Log(i), Mathf.Log(sampleData[i]), 3), Color.blue);
            }
        

            var animationTime = processor.SampleTime * animateEvery;
            
            var newHeights = new float[heightMap.Length];
            
            // Copy all data, removing last row
            for (var i = 0; i < newHeights.Length; i++)
            {
                var row = i / gridSize;

                if (row == 0) {
                    newHeights[i] = 0; // first row is 0 height to join the grid to the road
                }
                else if (row == 1) {
                    var ix = i - gridSize == sampleSize ? 0 : i - gridSize; // last column joins to next grid
                    ix = invert ? sampleSize - 1 - ix : ix;
                    
                    newHeights[i] = Mathf.Log(sampleData[ix] + 1) * 2; // log(x+1) ensures no negative values
                }
                else {
                    newHeights[i] = heightMap[i-gridSize];
                }
            }
            
            heightAnimation = StartCoroutine(AnimateHeights(newHeights, animationTime));
        }

        IEnumerator AnimateHeights(float[] newHeights, float animationTime)
        {
            var time = 0f;
            var heights = new float[newHeights.Length];

            while (time < animationTime)
            {
                for (var i = 0; i < heightMap.Length; i++)
                {
                    heights[i] = heightMap[i] + (animationCurve.Evaluate(time/animationTime) * (newHeights[i] - heightMap[i])); 
                }

                SetHeightMap(heights);
                yield return null;
                time += Time.deltaTime;
            }

            SetHeightMap(newHeights);
            heightMap = newHeights;
            heightAnimation = null;
        }

        void SetHeightMap(float[] heightMap, int? gridSize = null)
        {
            foreach(var lGrid in leftGrids)
                    lGrid.SetHeightMap(heightMap, gridSize, gridSize);

            foreach(var rGrid in rightGrids)
                rGrid.SetHeightMap(heightMap, gridSize, gridSize);
        }
    }
}