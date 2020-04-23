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

        [SerializeField] bool invert = false;

        [SerializeField] float speed;
        [SerializeField] float resetPoint;

        [SerializeField] DynamicGrid[] rightGrids;
        [SerializeField] DynamicGrid[] leftGrids;

        List<float> heightMap;
        int gridSize;

        void Start()
        {
            InitializeGrids();
            processor.OnSample.AddListener(GenerateRow);
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
            gridSize = processor.SampleBands + 1;

            var mapSize = gridSize * gridSize;
            heightMap = new List<float>(mapSize);

            for (var i = 0; i < mapSize; i++)
                heightMap.Add(0);

            foreach (var grid in rightGrids)
                grid.SetHeightMap(heightMap, gridSize, gridSize);
            
            foreach (var grid in leftGrids)
                grid.SetHeightMap(heightMap, gridSize, gridSize);
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

        void GenerateRow(float[] sampleData)
        {
            if (sampleData.Length < sampleSize || rightGrids[0].Animating)
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
        
            // remove the last row of data
            var ix = heightMap.Count - 1 - gridSize;
            heightMap.RemoveRange(ix, gridSize);
            
            // add the new row of data from the samples we received.
            for (var i = 0; i < sampleSize; i++)
            {
                ix = invert ? sampleSize - (i + 1) : i;
                // first row is 0 height to join to road math.Log(x + 1) => ensure no negative values
                heightMap.Insert(gridSize + i, Mathf.Log(sampleData[ix] + 1) * 2);
            }
            
            // insert the first column in the last column to match the next grid
            ix = invert ? sampleSize - 1 : 0;
            heightMap.Insert(gridSize + sampleSize, Mathf.Log(sampleData[ix] + 1) * 2);

            // send height maps to the grids
            foreach(var grid in rightGrids)
                grid.SetHeightMap(heightMap);

            foreach(var grid in leftGrids)
                grid.SetHeightMap(heightMap);
        }
    }
}