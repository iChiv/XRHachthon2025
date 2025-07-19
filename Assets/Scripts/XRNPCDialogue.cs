using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // 添加TextMeshPro命名空间

[System.Serializable]
public class DialogueLine
{
  [TextArea(2, 4)]
  public string npcText; // NPC说的话
  
  [Header("玩家回复选项")]
  public string[] playerOptions = new string[2]; // 玩家的回复选项
  
  [Header("设置")]
  public float displayDuration = 3f; // NPC文本显示时间（秒）
  public bool waitForPlayerResponse = true; // 是否等待玩家回复
}

public class XRNPCDialogue : MonoBehaviour
{
  [Header("NPC设置")]
  [SerializeField] private string npcName = "神秘人";
  [SerializeField] private float triggerDistance = 2f; // 触发对话的距离
  [SerializeField] private bool lookAtPlayer = true; // 是否面向玩家
  [SerializeField] private float rotationSpeed = 3f; // 转向速度
  
  [Header("对话内容")]
  [SerializeField] private DialogueLine[] dialogueLines;
  
  [Header("XR UI设置 - TextMeshPro")]
  [SerializeField] private Canvas dialogueCanvas; // 对话Canvas（World Space）
  [SerializeField] private TextMeshProUGUI npcNameText; // NPC名字 - 使用TextMeshPro UGUI
  [SerializeField] private TextMeshProUGUI npcDialogueText; // NPC对话文本 - 使用TextMeshPro UGUI
  [SerializeField] private GameObject playerResponsePanel; // 玩家回复面板
  [SerializeField] private Button[] responseButtons = new Button[2]; // 回复按钮
  [SerializeField] private TextMeshProUGUI[] responseButtonTexts = new TextMeshProUGUI[2]; // 按钮文本 - 使用TextMeshPro UGUI
  
  [Header("文本样式设置")]
  [SerializeField] private float npcTextSize = 24f; // NPC文本大小
  [SerializeField] private Color npcTextColor = Color.white; // NPC文本颜色
  [SerializeField] private float buttonTextSize = 18f; // 按钮文本大小
  [SerializeField] private Color buttonTextColor = Color.black; // 按钮文本颜色
  [SerializeField] private TMP_FontAsset customFont; // 自定义字体
  
  [Header("视觉效果")]
  [SerializeField] private GameObject dialogueIndicator; // 对话指示器（如头顶的图标）
  [SerializeField] private float canvasDistance = 2f; // Canvas距离玩家的距离
  [SerializeField] private Vector3 canvasOffset = Vector3.up; // Canvas位置偏移
  
  [Header("音效")]
  [SerializeField] private AudioSource audioSource;
  [SerializeField] private AudioClip dialogueStartSound;
  [SerializeField] private AudioClip dialogueEndSound;
  [SerializeField] private AudioClip npcSpeakSound;
  [SerializeField] private AudioClip buttonClickSound;
  
  [Header("调试")]
  [SerializeField] private bool showDebugInfo = true;
  [SerializeField] private Color gizmosColor = Color.cyan;
  
  // 私有变量
  private Transform playerTransform;
  private Camera playerCamera;
  private bool playerInRange = false;
  private bool isDialogueActive = false;
  private int currentDialogueIndex = 0;
  private bool waitingForPlayerResponse = false;
  private Coroutine npcSpeakCoroutine;
  private Coroutine typewriterCoroutine; // 打字机效果协程
  
  // 事件
  public System.Action OnDialogueStarted;
  public System.Action OnDialogueCompleted;
  public System.Action<int, int> OnPlayerResponseSelected; // 对话索引, 选择的回复索引
  
  // 属性
  public bool IsDialogueActive => isDialogueActive;
  public bool PlayerInRange => playerInRange;
  public int CurrentDialogueIndex => currentDialogueIndex;
  public bool WaitingForResponse => waitingForPlayerResponse;
  
  private void Start()
  {
      InitializeXRComponents();
      InitializeUI();
      SetupButtons();
      ApplyTextStyles();
  }
  
  private void InitializeXRComponents()
  {
      // 查找XR玩家组件
      GameObject xrRig = GameObject.FindGameObjectWithTag("Player");
      if (xrRig == null)
      {
          // 尝试查找XR Origin或XR Rig
          // xrRig = FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XROrigin>()?.gameObject;
      }
      
      if (xrRig != null)
      {
          playerTransform = xrRig.transform;
          
          // 查找头部摄像机
          playerCamera = xrRig.GetComponentInChildren<Camera>();
          if (playerCamera == null)
          {
              // 尝试查找Main Camera
              playerCamera = Camera.main;
          }
      }
      
      if (playerTransform == null)
      {
          Debug.LogError("XRNPCDialogue: 未找到XR玩家！请确保玩家GameObject有'Player'标签。");
      }
      
      if (playerCamera == null)
      {
          Debug.LogError("XRNPCDialogue: 未找到玩家摄像机！");
      }
  }
  
  private void InitializeUI()
  {
      // 设置Canvas为World Space模式
      if (dialogueCanvas != null)
      {
          dialogueCanvas.renderMode = RenderMode.WorldSpace;
          dialogueCanvas.gameObject.SetActive(false);
      }
      
      // 设置NPC名字
      if (npcNameText != null)
      {
          npcNameText.text = npcName;
      }
      
      // 隐藏玩家回复面板
      if (playerResponsePanel != null)
      {
          playerResponsePanel.SetActive(false);
      }
      
      // 隐藏对话指示器
      if (dialogueIndicator != null)
      {
          dialogueIndicator.SetActive(false);
      }
  }
  
  private void ApplyTextStyles()
  {
      // 应用NPC文本样式
      if (npcNameText != null)
      {
          npcNameText.fontSize = npcTextSize + 4f; // 名字稍大一些
          npcNameText.color = npcTextColor;
          if (customFont != null)
              npcNameText.font = customFont;
      }
      
      if (npcDialogueText != null)
      {
          npcDialogueText.fontSize = npcTextSize;
          npcDialogueText.color = npcTextColor;
          if (customFont != null)
              npcDialogueText.font = customFont;
      }
      
      // 应用按钮文本样式
      for (int i = 0; i < responseButtonTexts.Length; i++)
      {
          if (responseButtonTexts[i] != null)
          {
              responseButtonTexts[i].fontSize = buttonTextSize;
              responseButtonTexts[i].color = buttonTextColor;
              if (customFont != null)
                  responseButtonTexts[i].font = customFont;
          }
      }
  }
  
  private void SetupButtons()
  {
      // 设置回复按钮事件
      for (int i = 0; i < responseButtons.Length; i++)
      {
          if (responseButtons[i] != null)
          {
              int buttonIndex = i; // 闭包变量
              responseButtons[i].onClick.AddListener(() => OnResponseButtonClicked(buttonIndex));
          }
      }
  }
  
  private void Update()
  {
      if (playerTransform == null) return;
      
      CheckPlayerDistance();
      UpdateCanvasPosition();
      HandleNPCRotation();
  }
  
  private void CheckPlayerDistance()
  {
      float distance = Vector3.Distance(transform.position, playerTransform.position);
      bool inRange = distance <= triggerDistance;
      
      if (inRange != playerInRange)
      {
          playerInRange = inRange;
          OnPlayerRangeChanged(inRange);
      }
  }
  
  private void OnPlayerRangeChanged(bool inRange)
  {
      if (inRange)
      {
          if (showDebugInfo)
              Debug.Log($"玩家进入NPC {npcName} 的对话范围");
          
          ShowDialogueIndicator();
          
          // 自动开始对话
          if (!isDialogueActive)
          {
              StartDialogue();
          }
      }
      else
      {
          if (showDebugInfo)
              Debug.Log($"玩家离开NPC {npcName} 的对话范围");
          
          HideDialogueIndicator();
          
          // 结束对话
          if (isDialogueActive)
          {
              EndDialogue();
          }
      }
  }
  
  private void ShowDialogueIndicator()
  {
      if (dialogueIndicator != null)
      {
          dialogueIndicator.SetActive(true);
      }
  }
  
  private void HideDialogueIndicator()
  {
      if (dialogueIndicator != null)
      {
          dialogueIndicator.SetActive(false);
      }
  }
  
  private void UpdateCanvasPosition()
  {
      if (dialogueCanvas == null || playerCamera == null || !isDialogueActive) return;
      
      // 将Canvas定位在玩家前方
      Vector3 targetPosition = playerCamera.transform.position + 
                              playerCamera.transform.forward * canvasDistance + canvasOffset;
      
      dialogueCanvas.transform.position = targetPosition;
      
      // 让Canvas面向玩家
      Vector3 lookDirection = playerCamera.transform.position - dialogueCanvas.transform.position;
      lookDirection.y = 0; // 只在水平面旋转
      
      if (lookDirection != Vector3.zero)
      {
          dialogueCanvas.transform.rotation = Quaternion.LookRotation(-lookDirection);
      }
  }
  
  private void HandleNPCRotation()
  {
      if (!lookAtPlayer || !playerInRange || playerTransform == null) return;
      
      Vector3 direction = (playerTransform.position - transform.position).normalized;
      direction.y = 0; // 只在水平面旋转
      
      if (direction != Vector3.zero)
      {
          Quaternion targetRotation = Quaternion.LookRotation(direction);
          transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
              rotationSpeed * Time.deltaTime);
      }
  }
  
  public void StartDialogue()
  {
      if (isDialogueActive || dialogueLines.Length == 0) return;
      
      if (showDebugInfo)
          Debug.Log($"开始与 {npcName} 的对话");
      
      isDialogueActive = true;
      currentDialogueIndex = 0;
      
      // 显示对话Canvas
      if (dialogueCanvas != null)
      {
          dialogueCanvas.gameObject.SetActive(true);
      }
      
      // 隐藏对话指示器
      HideDialogueIndicator();
      
      // 播放开始音效
      PlaySound(dialogueStartSound);
      
      // 显示第一句对话
      ShowCurrentDialogue();
      
      // 触发事件
      OnDialogueStarted?.Invoke();
  }
  
  private void ShowCurrentDialogue()
  {
      if (currentDialogueIndex >= dialogueLines.Length) return;
      
      DialogueLine currentLine = dialogueLines[currentDialogueIndex];
      
      // 停止之前的协程
      if (npcSpeakCoroutine != null)
      {
          StopCoroutine(npcSpeakCoroutine);
      }
      if (typewriterCoroutine != null)
      {
          StopCoroutine(typewriterCoroutine);
      }
      
      // 启动打字机效果显示NPC文本
      if (npcDialogueText != null)
      {
          typewriterCoroutine = StartCoroutine(TypewriterEffect(currentLine.npcText));
      }
      
      // 播放NPC说话音效
      PlaySound(npcSpeakSound);
      
      if (showDebugInfo)
          Debug.Log($"NPC {npcName}: {currentLine.npcText}");
      
      // 隐藏玩家回复面板
      if (playerResponsePanel != null)
      {
          playerResponsePanel.SetActive(false);
      }
      
      // 开始NPC说话计时
      npcSpeakCoroutine = StartCoroutine(NPCSpeakCoroutine(currentLine));
  }
  
  private IEnumerator TypewriterEffect(string text)
  {
      if (npcDialogueText == null) yield break;
      
      npcDialogueText.text = "";
      
      // 使用TextMeshPro的富文本支持
      foreach (char letter in text.ToCharArray())
      {
          npcDialogueText.text += letter;
          
          // 跳过富文本标签的打字音效
          if (letter != '<' && letter != '>' && letter != ' ')
          {
              // 可以在这里播放打字音效
          }
          
          yield return new WaitForSeconds(0.03f); // 打字速度
      }
      
      typewriterCoroutine = null;
  }
  
  private IEnumerator NPCSpeakCoroutine(DialogueLine dialogueLine)
  {
      // 等待打字机效果完成
      while (typewriterCoroutine != null)
      {
          yield return null;
      }
      
      // 等待额外的显示时间
      yield return new WaitForSeconds(dialogueLine.displayDuration);
      
      // 如果需要等待玩家回复
      if (dialogueLine.waitForPlayerResponse)
      {
          ShowPlayerResponseOptions(dialogueLine);
      }
      else
      {
          // 直接进入下一句对话
          NextDialogue();
      }
      
      npcSpeakCoroutine = null;
  }
  
  private void ShowPlayerResponseOptions(DialogueLine dialogueLine)
  {
      waitingForPlayerResponse = true;
      
      // 显示玩家回复面板
      if (playerResponsePanel != null)
      {
          playerResponsePanel.SetActive(true);
      }
      
      // 设置按钮文本
      for (int i = 0; i < responseButtons.Length && i < dialogueLine.playerOptions.Length; i++)
      {
          if (responseButtons[i] != null && responseButtonTexts[i] != null)
          {
              responseButtons[i].gameObject.SetActive(true);
              responseButtonTexts[i].text = dialogueLine.playerOptions[i];
              
              // 为按钮文本应用样式
              responseButtonTexts[i].fontSize = buttonTextSize;
              responseButtonTexts[i].color = buttonTextColor;
          }
      }
      
      // 隐藏多余的按钮
      for (int i = dialogueLine.playerOptions.Length; i < responseButtons.Length; i++)
      {
          if (responseButtons[i] != null)
          {
              responseButtons[i].gameObject.SetActive(false);
          }
      }
      
      if (showDebugInfo)
          Debug.Log("等待玩家选择回复...");
  }
  
  private void OnResponseButtonClicked(int buttonIndex)
  {
      if (!waitingForPlayerResponse) return;
      
      DialogueLine currentLine = dialogueLines[currentDialogueIndex];
      
      if (buttonIndex < currentLine.playerOptions.Length)
      {
          string selectedResponse = currentLine.playerOptions[buttonIndex];
          
          if (showDebugInfo)
              Debug.Log($"玩家选择: {selectedResponse}");
          
          // 播放按钮点击音效
          PlaySound(buttonClickSound);
          
          // 触发回复选择事件
          OnPlayerResponseSelected?.Invoke(currentDialogueIndex, buttonIndex);
          
          // 隐藏回复面板
          waitingForPlayerResponse = false;
          if (playerResponsePanel != null)
          {
              playerResponsePanel.SetActive(false);
          }
          
          // 进入下一句对话
          NextDialogue();
      }
  }
  
  private void NextDialogue()
  {
      currentDialogueIndex++;
      
      if (currentDialogueIndex < dialogueLines.Length)
      {
          ShowCurrentDialogue();
      }
      else
      {
          EndDialogue();
      }
  }
  
  private void EndDialogue()
  {
      if (!isDialogueActive) return;
      
      if (showDebugInfo)
          Debug.Log($"与 {npcName} 的对话结束");
      
      isDialogueActive = false;
      waitingForPlayerResponse = false;
      
      // 停止所有协程
      if (npcSpeakCoroutine != null)
      {
          StopCoroutine(npcSpeakCoroutine);
          npcSpeakCoroutine = null;
      }
      if (typewriterCoroutine != null)
      {
          StopCoroutine(typewriterCoroutine);
          typewriterCoroutine = null;
      }
      
      // 隐藏对话Canvas
      if (dialogueCanvas != null)
      {
          dialogueCanvas.gameObject.SetActive(false);
      }
      
      // 隐藏回复面板
      if (playerResponsePanel != null)
      {
          playerResponsePanel.SetActive(false);
      }
      
      // 播放结束音效
      PlaySound(dialogueEndSound);
      
      // 如果玩家还在范围内，显示对话指示器
      if (playerInRange)
      {
          ShowDialogueIndicator();
      }
      
      // 触发事件
      OnDialogueCompleted?.Invoke();
  }
  
  private void PlaySound(AudioClip clip)
  {
      if (audioSource != null && clip != null)
      {
          audioSource.PlayOneShot(clip);
      }
  }
  
  // 公共方法
  public void ForceStartDialogue()
  {
      if (playerInRange)
      {
          StartDialogue();
      }
  }
  
  public void ForceEndDialogue()
  {
      if (isDialogueActive)
      {
          EndDialogue();
      }
  }
  
  public void SetDialogueContent(DialogueLine[] newDialogue)
  {
      dialogueLines = newDialogue;
  }
  
  public void SetNPCName(string newName)
  {
      npcName = newName;
      if (npcNameText != null)
      {
          npcNameText.text = npcName;
      }
  }
  
  public void SetTriggerDistance(float distance)
  {
      triggerDistance = Mathf.Max(0.5f, distance);
  }
  
  // TextMeshPro特有方法
  public void SetTextStyles(float npcSize, Color npcColor, float buttonSize, Color buttonColor)
  {
      npcTextSize = npcSize;
      npcTextColor = npcColor;
      buttonTextSize = buttonSize;
      buttonTextColor = buttonColor;
      
      ApplyTextStyles();
  }
  
  public void SetCustomFont(TMP_FontAsset font)
  {
      customFont = font;
      ApplyTextStyles();
  }
  
  // 支持富文本的对话设置
  public void SetRichTextDialogue(int index, string richText)
  {
      if (index >= 0 && index < dialogueLines.Length)
      {
          dialogueLines[index].npcText = richText;
      }
  }
  
  // 调试绘制
  private void OnDrawGizmos()
  {
      Gizmos.color = gizmosColor;
      Gizmos.DrawWireSphere(transform.position, triggerDistance);
      
      // 绘制NPC朝向
      Gizmos.color = Color.blue;
      Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * 1.5f);
      
      // 绘制对话Canvas位置预览
      if (playerCamera != null && isDialogueActive)
      {
          Vector3 canvasPos = playerCamera.transform.position + 
                             playerCamera.transform.forward * canvasDistance + canvasOffset;
          Gizmos.color = Color.green;
          Gizmos.DrawWireCube(canvasPos, Vector3.one * 0.5f);
      }
  }
  
  private void OnDrawGizmosSelected()
  {
      Gizmos.color = Color.yellow;
      Gizmos.DrawWireSphere(transform.position, triggerDistance);
      
      #if UNITY_EDITOR
      UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, 
          $"XR NPC: {npcName}\n触发距离: {triggerDistance}m\n对话数: {dialogueLines?.Length ?? 0}");
      #endif
  }
  
  private void OnDestroy()
  {
      // 清理事件
      OnDialogueStarted = null;
      OnDialogueCompleted = null;
      OnPlayerResponseSelected = null;
      
      // 停止协程
      if (npcSpeakCoroutine != null)
      {
          StopCoroutine(npcSpeakCoroutine);
      }
      if (typewriterCoroutine != null)
      {
          StopCoroutine(typewriterCoroutine);
      }
  }
}