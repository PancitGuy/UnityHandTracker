using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HandTracking: MonoBehaviour
{
    public UDPReceive udpReceive;
    public GameObject[] leftHandPoints;
    public GameObject[] rightHandPoints;
    public Vector3[] lastLeftPositions;
    public Vector3[] lastRightPositions;

    public Vector3 leftHandOffset = new Vector3(-2f, 0f, 0f);
    public Vector3 rightHandOffset = new Vector3(2f, 0f, 0f);
    public Vector3 globalCenterPoint = new Vector3(-8f, -3f, 0);

    public Vector3 globalOffSet = Vector3.zero;

    public bool flipX = false;
    public bool flipY = false;
    public bool flipZ = false;

    public float scaleFactor = 100f;



    public bool enableSmooth = true;
    public float smoothFactor = 15f;


    private Vector3 leftHandWrist;
    private Vector3 rightHandWrist;


    public float depthMultiplier = 5f;
    private bool initialized = false;

    public float zMultiplier = 0.05f;
    public float zOffset = 0f;
    public float zBase = 1.5f;


    private float smoothDepthLeft = 0f;
    private float smoothSizeLeft = 0f;
    private float baseHandSizeLeft = -1f;
    private float smoothDepthRight = 0f;
    private float smoothSizeRight = 0f;
    private float baseHandSizeRight = -1f;

    private Vector3 centerLeft;
    private Vector3 centerRight;

    private Vector3 leftCalib;
    private Vector3 rightCalib;
    private bool calibrateCenter = false;


    private Queue<float> leftHandQueue = new Queue<float>();
    private Queue<float> rightHandQueue = new Queue<float>();
    public int handQueueSize = 10;

    private Queue <float> leftZQueue = new Queue<float>();
    private Queue <float> rightZQueue = new Queue<float>();

    public Vector3 xyOffset = Vector3.zero;
    void Start()
    {
        lastLeftPositions = new Vector3[leftHandPoints.Length];
        lastRightPositions = new Vector3[rightHandPoints.Length];
    }

    void Update()
    {
        string data = udpReceive.receivedData;

        if (string.IsNullOrEmpty(data))
        {   
            InterpolateHands();
            return;
        }

        data = data.Remove(0, 1);
        data = data.Remove(data.Length - 1, 1);
        print(data);

        string[] points = data.Split(',');
        print(points[0]);

        if (points.Length < 42 * 3)
        {
            return;
        }

        leftHandWrist = GetWristPosition(points, 0);
        rightHandWrist = GetWristPosition(points, 21 * 3);

        centerLeft = calibrateCenter ? leftCalib : Vector3.zero;
        centerRight = calibrateCenter ? rightCalib : Vector3.zero;

        if (!initialized){
            initialized = true;
        }
        

        UpdateHand(leftHandPoints, points, 0, leftHandOffset, ref leftHandWrist, ref smoothSizeLeft, ref baseHandSizeLeft, 
        ref smoothDepthLeft, centerLeft, true, leftHandQueue,
        leftZQueue, ref lastLeftPositions);

        UpdateHand(rightHandPoints, points, 21 * 3 , rightHandOffset, ref rightHandWrist, ref smoothSizeRight, 
        ref baseHandSizeRight, ref smoothDepthRight, centerRight, false, rightHandQueue,
        rightZQueue, ref lastRightPositions);
    }

    Vector3 GetWristPosition(string[] values, int index)
    {
        float x = float.Parse(values[index]) / scaleFactor;
        float y = float.Parse(values[index + 1]) / scaleFactor;
        float z = float.Parse(values[index + 2]) / scaleFactor;

        return new Vector3(x, y, z);
    }

    void UpdateHand(GameObject[] handPoints, string[] values, int index, Vector3 Offset, 
        ref Vector3 rawWrist, ref float smoothSize, ref float baseHandSize, ref float smoothDepth
        , Vector3 centerPoint, bool isLeft, Queue<float> sizeHistory,
        Queue<float> zQueue, ref Vector3[] lastPositions)
    {   
        int required = index + handPoints.Length * 3;
        if (required > values.Length){
            return;
        }
        Vector3 indexMCP = Vector3.zero;
        Vector3 pinkyMCP = Vector3.zero;


        Vector3[] localPoints = new Vector3[handPoints.Length];

        for (int i = 0; i < handPoints.Length; i++)
        {   
            
            int baseIndex = index + i * 3;

            float x = float.Parse(values[baseIndex])/ scaleFactor;
            float y = float.Parse(values[baseIndex + 1])/ scaleFactor;
            float z = float.Parse(values[baseIndex + 2]) / scaleFactor;

            if (flipX)
            {
                x = -x;
            }
            if (flipY)
            {
                y = -y;
            }

            if (flipZ)
            {
                z = -z;
            }

            Vector3 point = new Vector3(x, y, z);
            localPoints[i] = point;

            if (i == 0){
                rawWrist = point;
            }

            if(i == 5)
            {
                indexMCP = point;
            }
            if(i == 17)
            {
                pinkyMCP = point;
            }
        }

        float rawHandSize = Vector3.Distance(indexMCP, pinkyMCP);

        sizeHistory.Enqueue(rawHandSize);
        if (sizeHistory.Count > handQueueSize)
        {
            sizeHistory.Dequeue();
        }

        float avgHandSize = 0f;
        foreach (float size in sizeHistory)
        {
            avgHandSize += size;
        }

        avgHandSize /= sizeHistory.Count;

        smoothSize = Mathf.Lerp(smoothSize, avgHandSize, Time.deltaTime * 3f);
        float handSize = smoothSize;

        if (baseHandSize < 0f || Mathf.Abs(handSize - baseHandSize) > 0.15f)
        {
            baseHandSize = handSize;
            smoothDepth = 0f;
        }

        Vector3 calib = calibrateCenter ? (isLeft ? leftCalib : rightCalib) : Vector3.zero;

        for(int i = 0; i < handPoints.Length; i++)
        {
            Vector3 point = localPoints[i];
            Vector3 calibratedPoint = point - calib;
            
            smoothDepth = Mathf.Lerp(smoothDepth, -handSize * depthMultiplier, Time.deltaTime * 10f);
            float smoothedZ = enableSmooth ? Mathf.Lerp(handPoints[i].transform.position.z, smoothDepth, Time.deltaTime * smoothFactor) : smoothDepth;

            Vector3 targetPosition = calibratedPoint + Offset + globalOffSet + centerPoint + xyOffset + globalCenterPoint;
            targetPosition.z = zBase + smoothedZ + zOffset;
            targetPosition.z = Mathf.Clamp(targetPosition.z, -2f, 3f);

            lastPositions[i] = targetPosition;

            if (enableSmooth){
                float t = Time.deltaTime * smoothFactor;
                handPoints[i].transform.position = Vector3.Lerp(handPoints[i].transform.position, targetPosition, t);
            }

            else{
                handPoints[i].transform.position = targetPosition;
            }
        }
    }

    void InterpolateHands()
    {
        for(int i = 0; i < leftHandPoints.Length; i++)
        {
            if(leftHandPoints[i] != null)
            {
                leftHandPoints[i].transform.position = Vector3.Lerp(leftHandPoints[i].transform.position, lastLeftPositions[i], Time.deltaTime * smoothFactor);
            }
        }

        for(int i = 0; i < rightHandPoints.Length; i++)
        {
            if(rightHandPoints[i] != null)
            {
                rightHandPoints[i].transform.position = Vector3.Lerp(rightHandPoints[i].transform.position, lastRightPositions[i], Time.deltaTime * smoothFactor);
            }
        }
    }

    void CalibrateCenter(){
        leftCalib = leftHandWrist;
        rightCalib = rightHandWrist;
        globalCenterPoint = -leftCalib;
        calibrateCenter = true;
    }
}