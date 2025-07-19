using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject furballPanel; // 你的UI面板GameObject
    public FurballCollectionPanelController panelController; // UI内容刷新脚本

    private bool isVisible = false;
    private bool lastA = false;

    void Start()
    {
        if (furballPanel != null)
            furballPanel.SetActive(false);
    }

    void Update()
    {
        bool aPressed = OVRInput.Get(OVRInput.Button.One);   // 右手柄A
        // bool xPressed = OVRInput.Get(OVRInput.Button.Three); // 左手柄X

        if (aPressed && !lastA)
        {
            isVisible = !isVisible;
            if (furballPanel != null)
                furballPanel.SetActive(isVisible);

            if (isVisible && panelController != null)
                panelController.UpdateInfo();
        }

        lastA = aPressed;
        // lastX = xPressed;
    }
}
