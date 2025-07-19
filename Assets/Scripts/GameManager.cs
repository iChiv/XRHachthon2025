using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

[System.Serializable]
public class PlayerScore
{
    public string playerName;
    public int score;
    public float gameTime;
    
    public PlayerScore(string name, int finalScore, float time)
    {
        playerName = name;
        score = finalScore;
        gameTime = time;
    }
}

public class GameManager : MonoBehaviour
{
    [Header("游戏设置")]
    public float gameTimeLimit = 180f; // 3分钟 = 180秒
    
    [Header("UI组件")]
    public GameUIController gameUIController;  // VR游戏UI控制器
    public GameObject leaderboardUI;
    public Transform leaderboardContent; // 排行榜内容的父对象
    public GameObject leaderboardEntryPrefab; // 排行榜条目的预制体
    public TextMeshProUGUI finalScoreText;
    public TMP_InputField playerNameInput;
    public Button submitScoreButton;
    
    [Header("分数设置")]
    public int furballCollectScore = 1;     // 收集毛球得分
    public int gloveRewardScore = 5;        // 手套合成得分
    public int scarfRewardScore = 10;       // 围巾合成得分
    public int pandaRewardScore = 15;       // 熊猫合成得分
    public int dogRewardScore = 20;         // 小狗合成得分
    
    [Header("音效接口（可选）")]
    public AudioSource gameAudioSource;     // 游戏音效源
    public AudioClip gameStartSound;        // 游戏开始音效
    public AudioClip gameEndSound;          // 游戏结束音效
    public AudioClip scoreSound;            // 计分音效
    
    [Header("特效接口（可选）")]
    public GameObject gameStartEffect;      // 游戏开始特效
    public GameObject gameEndEffect;        // 游戏结束特效
    public GameObject scoreEffect;          // 计分特效
    
    // 游戏状态
    public enum GameState { Ready, Playing, Finished }
    public GameState currentState = GameState.Ready;
    
    // 游戏数据
    private float currentTime;
    private int currentScore = 0;
    private int furballsCollected = 0;
    private int totalRewardsSynthesized = 0;
    
    // 排行榜数据
    private List<PlayerScore> leaderboard = new List<PlayerScore>();
    private const string LEADERBOARD_KEY = "VR_Cat_Game_Leaderboard";
    
    // 单例模式
    public static GameManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 注释掉DontDestroyOnLoad，让每次场景重新加载时都创建新的GameManager
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        InitializeGame();
        LoadLeaderboard();
        
        // 设置提交分数按钮事件
        if (submitScoreButton != null)
            submitScoreButton.onClick.AddListener(SubmitScore);
    }
    
    void InitializeGame()
    {
        currentTime = gameTimeLimit;
        currentScore = 0;
        furballsCollected = 0;
        totalRewardsSynthesized = 0;
        currentState = GameState.Ready;
        
        // 显示游戏UI，隐藏排行榜UI
        if (gameUIController != null) gameUIController.SetVisible(true);
        if (leaderboardUI != null) leaderboardUI.SetActive(false);
        
        UpdateUI();
    }
    
    void Update()
    {
        if (currentState == GameState.Playing)
        {
            // 倒计时
            currentTime -= Time.deltaTime;
            
            if (currentTime <= 0)
            {
                currentTime = 0;
                EndGame();
            }
            
            UpdateUI();
        }
        
        // 游戏控制现在由GameUIController处理
        // 这里移除重复的输入处理
    }
    
    public void StartGame()
    {
        if (currentState != GameState.Ready) return;
        
        currentState = GameState.Playing;
        
        // 播放游戏开始音效（如果有的话）
        if (gameAudioSource != null && gameStartSound != null)
            gameAudioSource.PlayOneShot(gameStartSound);
        
        // 播放游戏开始特效（如果有的话）
        if (gameStartEffect != null)
        {
            GameObject effect = Instantiate(gameStartEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
        
        Debug.Log("游戏开始！");
        UpdateUI();
    }
    
    void EndGame()
    {
        if (currentState != GameState.Playing) return;
        
        currentState = GameState.Finished;
        
        // 播放游戏结束音效（如果有的话）
        if (gameAudioSource != null && gameEndSound != null)
            gameAudioSource.PlayOneShot(gameEndSound);
        
        // 播放游戏结束特效（如果有的话）
        if (gameEndEffect != null)
        {
            GameObject effect = Instantiate(gameEndEffect, transform.position, Quaternion.identity);
            Destroy(effect, 5f);
        }
        
        Debug.Log($"游戏结束！最终分数: {currentScore}");
        
        // 显示最终分数
        if (finalScoreText != null)
            finalScoreText.text = $"最终分数: {currentScore}";
        
        // 显示排行榜UI
        ShowLeaderboard();
        UpdateUI();
    }
    
    // 重新开始游戏现在由GameUIController通过重新加载场景来处理
    // void RestartGame() - 已移除，避免冲突
    
    void UpdateUI()
    {
        // UI更新现在由GameUIController处理
        // 这里只保留基本的UI状态管理
    }
    
    // 公共方法：其他脚本调用来增加分数
    public void OnFurballCollected()
    {
        if (currentState != GameState.Playing) return;
        
        furballsCollected++;
        AddScore(furballCollectScore);
        Debug.Log($"收集毛球 +{furballCollectScore}分！总分: {currentScore}");
    }
    
    public void OnRewardSynthesized(string rewardType)
    {
        if (currentState != GameState.Playing) return;
        
        totalRewardsSynthesized++;
        int scoreToAdd = 0;
        
        // 根据奖励类型给分（使用包含检查，不区分大小写）
        string lowerRewardType = rewardType.ToLower();
        if (lowerRewardType.Contains("glove") || lowerRewardType.Contains("手套"))
        {
            scoreToAdd = gloveRewardScore;
        }
        else if (lowerRewardType.Contains("scarf") || lowerRewardType.Contains("围巾"))
        {
            scoreToAdd = scarfRewardScore;
        }
        else if (lowerRewardType.Contains("panda") || lowerRewardType.Contains("熊猫"))
        {
            scoreToAdd = pandaRewardScore;
        }
        else if (lowerRewardType.Contains("dog") || lowerRewardType.Contains("小狗") || lowerRewardType.Contains("狗"))
        {
            scoreToAdd = dogRewardScore;
        }
        
        if (scoreToAdd > 0)
        {
            AddScore(scoreToAdd);
            Debug.Log($"合成{rewardType} +{scoreToAdd}分！总分: {currentScore}");
        }
    }
    
    private void AddScore(int points)
    {
        currentScore += points;
        
        // 播放计分音效（如果有的话）
        if (gameAudioSource != null && scoreSound != null)
            gameAudioSource.PlayOneShot(scoreSound);
        
        // 播放计分特效（如果有的话）
        if (scoreEffect != null)
        {
            GameObject effect = Instantiate(scoreEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        Debug.Log($"获得 {points} 分！当前总分: {currentScore}");
    }
    
    void ShowLeaderboard()
    {
        // 现在排行榜由GameUIController管理，不需要单独的leaderboardUI
        // UI状态切换由GameUIController的ShowPanelByGameState自动处理
    }
    
    public void SubmitScore()
    {
        string playerName = "Player";
        if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
            playerName = playerNameInput.text;
        
        // 添加到排行榜
        PlayerScore newScore = new PlayerScore(playerName, currentScore, gameTimeLimit - currentTime);
        leaderboard.Add(newScore);
        
        // 排序（分数高的在前）
        leaderboard = leaderboard.OrderByDescending(s => s.score).Take(10).ToList(); // 只保留前10名
        
        SaveLeaderboard();
        RefreshLeaderboardDisplay();
        
        Debug.Log($"分数已提交: {playerName} - {currentScore}分");
    }
    
    void RefreshLeaderboardDisplay()
    {
        if (leaderboardContent == null || leaderboardEntryPrefab == null) return;
        
        // 清空现有条目
        foreach (Transform child in leaderboardContent)
        {
            Destroy(child.gameObject);
        }
        
        // 创建新的排行榜条目
        for (int i = 0; i < leaderboard.Count; i++)
        {
            GameObject entry = Instantiate(leaderboardEntryPrefab, leaderboardContent);
            TextMeshProUGUI[] texts = entry.GetComponentsInChildren<TextMeshProUGUI>();
            
            if (texts.Length >= 3)
            {
                texts[0].text = (i + 1).ToString(); // 排名
                texts[1].text = leaderboard[i].playerName; // 玩家名
                texts[2].text = leaderboard[i].score.ToString(); // 分数
            }
        }
    }
    
    void SaveLeaderboard()
    {
        try
        {
            string json = JsonUtility.ToJson(new SerializableList<PlayerScore>(leaderboard));
            PlayerPrefs.SetString(LEADERBOARD_KEY, json);
            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
            Debug.LogError("保存排行榜失败: " + e.Message);
        }
    }
    
    void LoadLeaderboard()
    {
        try
        {
            if (PlayerPrefs.HasKey(LEADERBOARD_KEY))
            {
                string json = PlayerPrefs.GetString(LEADERBOARD_KEY);
                var loadedList = JsonUtility.FromJson<SerializableList<PlayerScore>>(json);
                leaderboard = loadedList.items ?? new List<PlayerScore>();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("加载排行榜失败: " + e.Message);
            leaderboard = new List<PlayerScore>();
        }
    }
    
    // 获取游戏统计信息的公共方法
    public int GetCurrentScore() => currentScore;
    public float GetRemainingTime() => currentTime;
    public bool IsGamePlaying() => currentState == GameState.Playing;
    public int GetFurballsCollected() => furballsCollected;
    public int GetRewardsSynthesized() => totalRewardsSynthesized;
    
    // 获取排行榜数据的公共方法
    public List<PlayerScore> GetLeaderboard() => new List<PlayerScore>(leaderboard);
}

// 用于JSON序列化的辅助类
[System.Serializable]
public class SerializableList<T>
{
    public List<T> items;
    
    public SerializableList(List<T> list)
    {
        items = list;
    }
}
