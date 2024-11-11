using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// ratio 매개 변수로 받아 float Min에서 Max 사이의 Lerp 값 반환
    /// </summary>
    [System.Serializable]
    public struct MinMaxFloat
    {
        public float Min;
        public float Max;

        public float getValueFromRatio(float ratio)
        {
            return Mathf.Lerp(Min, Max, ratio);
        }
    }

    /// <summary>
    /// ratio 매개 변수로 받아 Color의 Min에서 Max 사이의 Lerp 값 반환
    /// </summary>
    [System.Serializable]
    public struct MinMaxColor
    {
        public Color Min;
        public Color Max;

        public Color getValueFromRatio(float ratio)
        {
            return Color.Lerp(Min, Max, ratio);
        }
    }
    
    /// <summary>
    /// ratio 매개 변수로 받아 Vector3의 Min에서 Max 사이의 Lerp 값 반환
    /// </summary>
    [System.Serializable]
    public struct MinMaxVector3
    {
        public Vector3 Min;
        public Vector3 Max;

        public Vector3 GetValueFromRatio(float ratio)
        {
            return Vector3.Lerp(Min, Max, ratio);
        }
    }
}