using UnityEngine;
using System.Collections.Generic;

public class PlayerFurballCollector : MonoBehaviour
{
    public List<FurballTypeCount> collectedFurballs = new List<FurballTypeCount>();

    public void CollectFurball(string furType)
    {
        FurballTypeCount entry = collectedFurballs.Find(x => x.furType == furType);
        if (entry != null)
        {
            entry.count++;
        }
        else
        {
            collectedFurballs.Add(new FurballTypeCount { furType = furType, count = 1 });
        }
        Debug.Log("收集到毛球：" + furType + "，当前数量：" + (entry != null ? entry.count : 1));
    }
}

[System.Serializable]
public class FurballTypeCount
{
    public string furType;
    public int count;
}
