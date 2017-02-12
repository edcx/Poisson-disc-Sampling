using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

/// <summary>
/// Poisson Disc Sampler is implemented based on paper http://www-devel.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf
/// Few points to mention:
/// 1. DummyValue is used to identify not assigned points in the space.
/// 2. By default points bottom left border is (0, 0, 0)
/// 3. If there are too much points to consider in the space it might take too much time to complete.
/// 4. Sampler every frame checks rejectionLimit*neighbourPointsCount points. Modify that coroutine if needed. Or just run it on a different thread
/// </summary>
public class PoissonDiscSampler : MonoBehaviour
{
    public float regionWidth {get; set;}
    public float regionHeight {get; set;}
    public float regionLength {get; set;}
    public float minDistance {get; set;}
    public float rejectionLimit {get; set;}

    public bool isSpherical {get; set;}
    public int TotalPointCount {get; set;}
    public Action SamplingFinished;
    public Action<Vector3> NewPointAdded;



    /// <summary>
    /// DummyValue is a not assigned point in our grid
    /// </summary>
    public static Vector3 DummyValue = Vector3.one * -100;
    private Vector3 center;
    private float cellSize;
   
    private Vector3[] grid;
    private List<Vector3> activeList;

    private int dimensions = 2;
    private int cols;
    private int rows;
    private int lens;

    private float minDistanceSquared;
    

    public static float SquaredDistanceTo(Vector3 v1, Vector3 v2){
		 Vector3 result = v2 - v1;
		 return result.sqrMagnitude;
	 }

    /// <summary>
    /// Returns the current points
    /// </summary>
    /// <returns></returns>
    public Vector3[] Points
    {
        get
        {
            return grid;
        }
    }

    public void StartSampling (int targetDimensions)
	{
        TotalPointCount = 0;
        dimensions = targetDimensions;
        if (dimensions == 2)
            regionLength = 0;
        center = new Vector3(regionWidth / 2f, regionHeight / 2f, regionLength / 2f);
        minDistanceSquared = minDistance * minDistance;

        // ---------- Step 0 ----------
        cellSize = minDistance/Mathf.Sqrt(dimensions);
        cols = Mathf.FloorToInt(regionWidth/cellSize);
        rows = Mathf.FloorToInt(regionHeight/cellSize);
        lens = Mathf.FloorToInt(regionLength/cellSize);
        
        if (dimensions == 2)
            lens = 1;

        grid = new Vector3[ cols * rows * lens];

        for (int i  = 0; i < grid.Length; i++)
	        grid[i] = DummyValue;

        Debug.Log("Grid Length: " + grid.Length);

        // ---------- Step 1 ----------
        Vector3 point = new Vector3(
	        point.x = Random.Range(regionWidth * 0.4f, regionWidth * 0.6f),
            point.y = Random.Range(regionHeight * 0.4f, regionHeight * 0.6f), 
            0);
        if (dimensions == 3)
            point.z = Random.Range(regionLength * 0.4f, regionLength * 0.6f); 

        int xIndex = Mathf.FloorToInt(point.x/regionWidth);
        int yIndex = Mathf.FloorToInt(point.y/regionHeight);
        int zIndex = Mathf.FloorToInt(point.z/regionLength);
        if (dimensions == 2)
            zIndex = 0;
        grid[xIndex + yIndex*cols + zIndex * cols * lens] = point;

        activeList = new List<Vector3>();
        activeList.Add(point);
        NewPointAdded(point);
        TotalPointCount++;
        //Start Step 2
        StartCoroutine(Fill());
	}

/// <summary>
/// Every frame checls rejectionLimit*neighbourPointsCount points. 
/// </summary>
/// <returns></returns>
    IEnumerator Fill()
    {
        // ---------- Step 2 ----------
        while (activeList.Count > 0)
        {
            yield return null;

            int randomIndex = Random.Range(0, activeList.Count);
            var randomPoint = activeList[randomIndex];

            bool found = false;
            for (int k = 0; k < rejectionLimit; k++)
            {
                var length = Random.Range(minDistance, 2 * minDistance);
                var angle = Random.Range(0, 360);
                Vector3 sample = Quaternion.AngleAxis(angle, Vector3.forward) * Vector3.up;
                if (dimensions == 3){
                    angle = Random.Range(0, 360);
                    sample = Quaternion.AngleAxis(angle, Vector3.up) * sample;
                }
                sample = randomPoint + sample.normalized * length;

                int col = Mathf.FloorToInt(sample.x/cellSize);
                int row = Mathf.FloorToInt(sample.y/cellSize);
                int len = Mathf.FloorToInt(sample.z/cellSize);

                if (col < 0 || col >= cols || row < 0 || row >= rows || len < 0 || len >= lens) continue;
                if (isSpherical){
                    if (SquaredDistanceTo(center,sample) > (regionWidth / 2) * (regionWidth / 2))
                        continue;
                }

                bool isOK = true;

                // Check if neighbour points are too close or not
                for (int x = -1; x < 2; x++)
                {
                    for (int y = -1; y < 2; y++)
                    {
                        for (int z = -1; z < 2; z++)
                        {
                            // if indexs are out of bounds try next point
                            if ((col + x) < 0 || (col + x) >= cols || 
                                (row + y) < 0 || (row + y) >= rows ||
                                (len + z) < 0 || (len + z) >= lens) continue;
                            // if there is no point no need to check distance 
                            if (grid[(col + x) + (row + y)*cols + (len + z) * cols * lens] == DummyValue) continue;
                            
                            Vector3 nPoint = grid[(col + x) + (row + y)*cols + (len + z) * cols * lens];
                            var dSqred = SquaredDistanceTo(nPoint, sample);
                            if (dSqred < minDistanceSquared)
                            {
                                isOK = false;
                            }
                        }
                    }
                }
                if (isOK)
                {
                    found = true;
                   
                    grid[col + row * cols + len * cols * lens] = sample;
                    activeList.Add(sample);
                    TotalPointCount++;
                   
                    NewPointAdded(sample);

                    //break;
                }
            }
            if (!found)
                activeList.RemoveAt(randomIndex);
        }
        if (SamplingFinished != null)
            SamplingFinished();     
    }


}
