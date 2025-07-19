using UnityEngine;
using TMPro;
using System.Text;

public class FurballCollectionPanelController : MonoBehaviour
{
    public TextMeshProUGUI infoText;
    public PlayerFurballCollector playerCollector;

    public void UpdateInfo()
    {
        if (playerCollector == null) return;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("已收集毛球：");
        foreach (var entry in playerCollector.collectedFurballs)
        {
            sb.AppendLine($"{entry.furType}：{entry.count}个");
        }
        infoText.text = sb.ToString();
    }
}
