using UnityEngine;

public class SplineDecorator : MonoBehaviour {

	public BezierSpline spline;

    public float decorationStep = 0;

    public bool generateOnAwake = true;

	public bool lookForward;

	public Transform decoration;

	private void Awake () {
        if (generateOnAwake)
        {
            GenerateDecoration();
        }
    }

    [ContextMenu("Generate Decoration")]
    public void GenerateDecoration()
    {
        if(spline == null)
        {
            spline = GetComponent<BezierSpline>();
        }
        if (spline == null || decorationStep <= 0 || decoration == null)
        {
            return;
        }

        foreach(Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        float distance = decorationStep;
        while (distance < spline.Length)
        {
            float t = distance / spline.Length;
            Transform item = Instantiate(decoration) as Transform;
            Vector3 position = spline.GetPoint(t);
            item.transform.localPosition = position;
            if (lookForward)
            {
                item.transform.LookAt(position + spline.GetDirection(t));
            }
            item.transform.parent = transform;

            distance += decorationStep;
        }
    }
}