using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameUIController : MonoBehaviour
{
    [Header("Panel Management")]
    public GameObject startPanel;       // 开始界面panel
    public GameObject gamePanel;        // 游戏进行时panel
    public GameObject endPanel;         // 结束界面panel
    
    [Header("Dynamic UI Components")]
    public TextMeshProUGUI timerText;   // 需要实时更新倒计时
    public TextMeshProUGUI scoreText;   // 需要实时更新分数
    public TextMeshProUGUI finalScoreText; // 需要显示最终分数
    
    [Header("UI Style Settings")]
    public Color timerColor = Color.white;
    public Color scoreColor = Color.yellow;
    public float fontSize = 36f;
    
    [Header("VR UI Control")]
    public VRUIFollower uiFollower;
    
    [Header("UI Buttons (Optional)")]
    public Button startGameButton;      // 可选：开始游戏按钮
    public Button restartGameButton;    // 可选：重新开始按钮
    
    private GameManager gameManager;
    private CanvasGroup canvasGroup;
    
    void Start()
    {
        // 获取组件引用
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogError("GameUIController: 未找到GameManager实例！");
            return;
        }
        
        // 获取VRUIFollower组件
        if (uiFollower == null)
            uiFollower = GetComponent<VRUIFollower>();
            
        // 获取或添加CanvasGroup组件
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        InitializeUI();
    }
    
    void InitializeUI()
    {
        // 设置游戏UI文本样式
        if (timerText != null)
        {
            timerText.color = timerColor;
            timerText.fontSize = fontSize;
            timerText.text = "Time: 03:00";
        }
        
        if (scoreText != null)
        {
            scoreText.color = scoreColor;
            scoreText.fontSize = fontSize;
            scoreText.text = "Score: 0";
        }
        
        // 设置UI按钮事件（用于手部追踪点击）
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(() => {
                if (gameManager != null && gameManager.currentState == GameManager.GameState.Ready)
                    gameManager.StartGame();
            });
        }
        
        if (restartGameButton != null)
        {
            restartGameButton.onClick.AddListener(() => {
                if (gameManager != null && gameManager.currentState == GameManager.GameState.Finished)
                    RestartGame();
            });
        }
        
        // 根据游戏状态显示对应panel
        ShowPanelByGameState();
    }
    
    void Update()
    {
        // 检查gameManager是否为空，如果为空尝试重新查找
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
            if (gameManager == null) return;
        }
        
        UpdateGameInfo();
        ShowPanelByGameState();
        HandleVRInput();
    }
    
    void UpdateGameInfo()
    {
        // 更新倒计时
        if (timerText != null)
        {
            float remainingTime = gameManager.GetRemainingTime();
            int minutes = Mathf.FloorToInt(remainingTime / 60);
            int seconds = Mathf.FloorToInt(remainingTime % 60);
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
            
            // 时间不足30秒时闪烁红色警告
            if (remainingTime <= 30f && remainingTime > 0)
            {
                timerText.color = Color.Lerp(Color.red, timerColor, Mathf.PingPong(Time.time * 2f, 1f));
            }
            else
            {
                timerText.color = timerColor;
            }
        }
        
        // 更新分数
        if (scoreText != null)
        {
            scoreText.text = $"Score: {gameManager.GetCurrentScore()}";
        }
    }
    
    void ShowPanelByGameState()
    {
        if (gameManager == null) return;
        
        // 根据游戏状态显示对应的panel
        switch (gameManager.currentState)
        {
            case GameManager.GameState.Ready:
                ShowStartPanel();
                break;
            case GameManager.GameState.Playing:
                ShowGamePanel();
                break;
            case GameManager.GameState.Finished:
                ShowEndPanel();
                break;
        }
    }
    
    void ShowStartPanel()
    {
        SetPanelActive(startPanel, true);
        SetPanelActive(gamePanel, false);
        SetPanelActive(endPanel, false);
    }
    
    void ShowGamePanel()
    {
        SetPanelActive(startPanel, false);
        SetPanelActive(gamePanel, true);
        SetPanelActive(endPanel, false);
    }
    
    void ShowEndPanel()
    {
        SetPanelActive(startPanel, false);
        SetPanelActive(gamePanel, false);
        SetPanelActive(endPanel, true);
        
        // 显示最终分数
        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score: {gameManager.GetCurrentScore()}";
        }
    }
    
    void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }
    
    // 公共方法：手动控制显示
    public void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
    
    // 公共方法：设置UI距离
    public void SetDistance(float distance)
    {
        if (uiFollower != null)
        {
            uiFollower.SetDistance(distance);
        }
    }
    
    // 公共方法：切换跟随模式
    public void SetFollowMode(bool follow)
    {
        if (uiFollower != null)
        {
            uiFollower.SetFollowMode(follow, follow);
        }
    }
    
    // VR输入处理
    void HandleVRInput()
    {
        bool shouldTrigger = false;
        
        // 手柄A键或裸手捏手势控制
        if (OVRInput.GetDown(OVRInput.Button.One)||
            OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger)||
            OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger))
        {
            shouldTrigger = true;
        }

        
        // 执行游戏控制
        if (shouldTrigger)
        {
            switch (gameManager.currentState)
            {
                case GameManager.GameState.Ready:
                    // 开始游戏
                    gameManager.StartGame();
                    break;
                case GameManager.GameState.Finished:
                    // 重新开始游戏 - 重新加载场景
                    RestartGame();
                    break;
            }
        }
    }
    
    // 重新开始游戏 - 重新加载当前场景
    void RestartGame()
    {
        Debug.Log("重新加载场景...");
        
        // 重新加载当前场景
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
} 