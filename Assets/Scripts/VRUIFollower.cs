using UnityEngine;

public class VRUIFollower : MonoBehaviour
{
    [Header("跟随设置")]
    public Transform playerCamera;  // 玩家头显摄像机（CenterEyeAnchor）
    public bool followPosition = true;
    public bool followRotation = true;
    public bool smoothFollow = true;
    public float followSpeed = 5f;
    
    [Header("偏移设置")]
    public Vector3 positionOffset = new Vector3(0, 0, 2f);  // 前方2米
    public Vector3 rotationOffset = Vector3.zero;
    
    [Header("距离控制")]
    public float minDistance = 1f;
    public float maxDistance = 10f;
    public float currentDistance = 2f;
    
    [Header("显示控制")]
    public bool showOnlyWhenLooking = false;  // 只在玩家看向时显示
    public float lookingAngle = 45f;  // 视野角度
    
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    
    void Start()
    {
        canvas = GetComponent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        // 自动查找玩家摄像机
        if (playerCamera == null)
        {
            FindPlayerCamera();
        }
        
        // 确保Canvas设置正确
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = playerCamera != null ? playerCamera.GetComponent<Camera>() : null;
            
            // VR特殊设置：确保正确渲染
            canvas.sortingOrder = 1;
            
            // 如果找不到相机，尝试找主相机
            if (canvas.worldCamera == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                    canvas.worldCamera = mainCam;
            }
        }
        
        // 添加CanvasGroup（如果没有的话）
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // 初始位置设置
        UpdatePosition();
    }
    
    void FindPlayerCamera()
    {
        // 查找OVR Camera Rig中的CenterEyeAnchor
        GameObject cameraRig = GameObject.FindGameObjectWithTag("Player");
        if (cameraRig != null)
        {
            Transform centerEye = cameraRig.transform.Find("TrackingSpace/CenterEyeAnchor");
            if (centerEye != null)
            {
                playerCamera = centerEye;
                return;
            }
        }
        
        // 备用方案：查找主摄像机
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            playerCamera = mainCam.transform;
        }
        
        if (playerCamera == null)
        {
            Debug.LogWarning("VRUIFollower: 未找到玩家摄像机！请手动拖拽CenterEyeAnchor到PlayerCamera字段。");
        }
    }
    
    void Update()
    {
        if (playerCamera == null) return;
        
        UpdatePosition();
        UpdateVisibility();
    }
    
    void UpdatePosition()
    {
        if (!followPosition && !followRotation) return;
        
        Vector3 targetPosition = transform.position;
        Quaternion targetRotation = transform.rotation;
        
        if (followPosition)
        {
            // 计算目标位置（摄像机位置 + 偏移）
            Vector3 forward = playerCamera.forward;
            Vector3 right = playerCamera.right;
            Vector3 up = playerCamera.up;
            
            targetPosition = playerCamera.position + 
                           forward * positionOffset.z + 
                           right * positionOffset.x + 
                           up * positionOffset.y;
            
            // 限制距离
            Vector3 directionToUI = (targetPosition - playerCamera.position).normalized;
            float clampedDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
            targetPosition = playerCamera.position + directionToUI * clampedDistance;
        }
        
        if (followRotation)
        {
            // 让UI始终面向玩家
            Vector3 lookDirection = playerCamera.position - transform.position;
            lookDirection.y = 0; // 保持UI垂直
            
            if (lookDirection != Vector3.zero)
            {
                targetRotation = Quaternion.LookRotation(-lookDirection, Vector3.up);
                targetRotation *= Quaternion.Euler(rotationOffset);
            }
        }
        
        // 平滑移动或直接设置
        if (smoothFollow && Application.isPlaying)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, followSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
    }
    
    void UpdateVisibility()
    {
        if (!showOnlyWhenLooking || canvasGroup == null) return;
        
        // 计算玩家是否在看向UI
        Vector3 directionToUI = (transform.position - playerCamera.position).normalized;
        float angle = Vector3.Angle(playerCamera.forward, directionToUI);
        
        bool isLooking = angle <= lookingAngle;
        float targetAlpha = isLooking ? 1f : 0.3f; // 不看时变半透明
        
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, 3f * Time.deltaTime);
    }
    
    // 公共方法：动态调整距离
    public void SetDistance(float distance)
    {
        currentDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        positionOffset.z = currentDistance;
    }
    
    // 公共方法：设置可见性
    public void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
        else
        {
            gameObject.SetActive(visible);
        }
    }
    
    // 公共方法：切换跟随模式
    public void SetFollowMode(bool position, bool rotation)
    {
        followPosition = position;
        followRotation = rotation;
    }
} 