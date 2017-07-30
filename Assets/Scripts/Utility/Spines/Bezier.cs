using UnityEngine;

public static class Bezier {

	public static Vector3 GetPoint (Vector3 p0, Vector3 p1, Vector3 p2, float t) {
		t = Mathf.Clamp01(t);
		float oneMinusT = 1f - t;
		return
			oneMinusT * oneMinusT * p0 +
			2f * oneMinusT * t * p1 +
			t * t * p2;
	}

	public static Vector3 GetFirstDerivative (Vector3 p0, Vector3 p1, Vector3 p2, float t) {
		return
			2f * (1f - t) * (p1 - p0) +
			2f * t * (p2 - p1);
	}

    public static float CalculateLength(Vector3 p0, Vector3 p1, Vector3 p2, int sampleCount)
    {
        float curveLength = 0;
        sampleCount = Mathf.Max(sampleCount, 2);
        float step = 1.0f / sampleCount;
        float t = 0;
        Vector3 lastPoint = GetPoint(p0, p1, p2, t);
        for (int i = 1; i < sampleCount; i++)
        {
            t += step;
            Vector3 point = GetPoint(p0, p1, p2, t + step);
            curveLength += Vector3.Distance(lastPoint, point);
            lastPoint = point;
        }
        return curveLength;
    }

    public static Vector3 GetPoint (Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
        t = Mathf.Clamp01(t);
		float OneMinusT = 1f - t;
		return
			OneMinusT * OneMinusT * OneMinusT * p0 +
			3f * OneMinusT * OneMinusT * t * p1 +
			3f * OneMinusT * t * t * p2 +
			t * t * t * p3;
	}

	public static Vector3 GetFirstDerivative (Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
		t = Mathf.Clamp01(t);
		float oneMinusT = 1f - t;
		return
			3f * oneMinusT * oneMinusT * (p1 - p0) +
			6f * oneMinusT * t * (p2 - p1) +
			3f * t * t * (p3 - p2);
	}

    public static float CalculateLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int sampleCount)
    {
        float curveLength = 0;
        sampleCount = Mathf.Max(sampleCount, 2);
        float step = 1.0f / sampleCount;
        float t = 0;
        Vector3 lastPoint = GetPoint(p0, p1, p2, p3, t);
        for (int i = 1; i < sampleCount; i++)
        {
            t += step;
            Vector3 point = GetPoint(p0, p1, p2, p3, t);
            curveLength += Vector3.Distance(lastPoint, point);
            lastPoint = point;
        }
        return curveLength;
    }
}