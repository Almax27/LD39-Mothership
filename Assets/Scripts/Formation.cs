using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Formation : MonoBehaviour {

    public List<Transform> FormationNodes = new List<Transform>();

    [ContextMenu("Use Children As Nodes")]
    private void UseChildrenAsNodes()
    {
        FormationNodes.Clear();
        foreach (Transform child in transform)
        {
            FormationNodes.Add(child);
        }
    }

    public Vector3 GetPositionAt(int index)
    {
        Vector3 position = Vector3.zero;
        if(index >= 0 && index < FormationNodes.Count)
        {
            position = FormationNodes[index].position;
        }
        return position;
    }

    public static T SelectBestFormation<T>(List<T> formationList, int desiredCount) where T : Formation
    {
        int bestCount = int.MaxValue;
        T bestFormation = default(T);
        foreach (T formation in formationList)
        {
            int countInFormation = formation.FormationNodes.Count;
            if (countInFormation >= desiredCount && countInFormation < bestCount)
            {
                bestCount = countInFormation;
                bestFormation = formation;
            }
        }
        return bestFormation;
    }

    public static int GetMaxCountSupported<T>(List<T> formationList) where T : Formation
    {
        int maxFormations = 0;
        foreach (T formation in formationList)
        {
            maxFormations = Mathf.Max(maxFormations, formation.FormationNodes.Count);
        }
        return maxFormations;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (Transform formationPoint in FormationNodes)
        {
            if (formationPoint)
            {
                Gizmos.DrawSphere(formationPoint.position, 0.05f);
            }
        }
    }
}
