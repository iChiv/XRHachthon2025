using UnityEngine;
using System.Collections.Generic;

public class CatController : MonoBehaviour
{
    [Header("自由移动参数")]
    public float moveSpeed = 1.5f;
    public float waitTimeMin = 2f;
    public float waitTimeMax = 4f;
    public float roamRadius = 10f; // 游走半径，需与CatSpawner的spawnRadius一致
    public Transform roamCenter;   // 游走中心点，通常为CatSpawner的centerPoint

    private Vector3 roamTarget;
    private bool isRoaming = true;
    private float waitTimer = 0f;

    [Header("撸猫参数")]
    public float requiredFrictionDistance = 1.0f; // 需要摩擦的距离
    public float speedThreshold = 0.3f;           // 速度阈值（单位/秒，降低难度）
    public float timeWindow = 3.0f;               // 时间窗口（秒，增加容错时间）
    public GameObject[] furballPrefabs;           // 毛球预制体数组
    public float runAwayDistance = 5f;            // 逃跑距离
    public float runAwaySpeed = 3f;               // 逃跑速度
    public GameObject furballSpawnEffectPrefab; // 毛球生成特效
    
    [Header("音效接口（可选）")]
    public AudioSource catAudioSource;            // 猫的音效源
    public AudioClip meowSound;                   // 猫叫音效
    public AudioClip furballSound;                // 生成毛球音效
    public AudioClip runawaySound;                // 逃跑音效
    
    [Header("特效接口（可选）")]
    public GameObject touchEffect;                // 被触摸时的特效
    public GameObject runAwayEffect;              // 逃跑时的特效
    public GameObject caughtEffect;               // 被抓住时的特效


    private float accumulatedDistance = 0f;
    private Vector3 lastPlayerPosition;
    private bool isTouchingPlayer = false;
    private float touchStartTime = 0f;            // 开始接触的时间戳
    private float totalSpeedAccumulated = 0f;     // 累积速度总和
    private int speedSamples = 0;                 // 速度采样次数（用于计算平均速度）
    private int furballCount = 0;
    private List<int> usedPrefabIndices = new List<int>();
    private bool isRunningAway = false;
    private Vector3 runAwayTarget;
    private Animator animator;
    private bool isCaught = false;


    void Start()
    {
        animator = GetComponent<Animator>();
        if (roamCenter == null || roamRadius <= 0f)
        {
            CatSpawner spawner = GameObject.FindObjectOfType<CatSpawner>();
            if (spawner != null)
            {
                roamCenter = spawner.centerPoint;
                roamRadius = spawner.spawnRadius;
            }
        }
        if (roamCenter == null)
            roamCenter = this.transform;
        PickNewRoamTarget();
    }



    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isTouchingPlayer = true;
            lastPlayerPosition = other.transform.position;
            accumulatedDistance = 0f;
            touchStartTime = Time.time;           // 记录开始接触时间
            totalSpeedAccumulated = 0f;           // 重置累积速度
            speedSamples = 0;                     // 重置采样次数
            
            // 播放猫叫音效（如果有的话）
            if (catAudioSource != null && meowSound != null)
                catAudioSource.PlayOneShot(meowSound);
            
            // 播放被触摸特效（如果有的话）
            if (touchEffect != null)
            {
                GameObject effect = Instantiate(touchEffect, transform.position + Vector3.up * 0.3f, Quaternion.identity);
                Destroy(effect, 2f);
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (isTouchingPlayer && furballCount < 3 && other.CompareTag("Player") && !isRunningAway)
        {
            Vector3 currentPlayerPosition = other.transform.position;
            float distance = Vector3.Distance(currentPlayerPosition, lastPlayerPosition);
            float currentSpeed = distance / Time.fixedDeltaTime;
            
            // 累积距离检测（原有逻辑）
            accumulatedDistance += distance;
            
            // 累积速度检测（新增，降低难度）
            totalSpeedAccumulated += currentSpeed;
            speedSamples++;
            float averageSpeed = speedSamples > 0 ? totalSpeedAccumulated / speedSamples : 0f;
            
            // 时间窗口检测（新增，增加容错）
            float touchDuration = Time.time - touchStartTime;
            
            lastPlayerPosition = currentPlayerPosition;

            // 多重检测条件（任何一个满足就能生成毛球，降低难度）
            bool distanceCondition = accumulatedDistance >= requiredFrictionDistance;
            bool speedCondition = averageSpeed >= speedThreshold && touchDuration >= 0.5f; // 至少摸0.5秒
            bool timeWindowCondition = touchDuration >= timeWindow && accumulatedDistance >= requiredFrictionDistance * 0.5f; // 时间够长且有基本移动
            
            if (distanceCondition || speedCondition || timeWindowCondition)
            {
                // 调试输出触发条件
                string triggerReason = "";
                if (distanceCondition) triggerReason += "距离达标 ";
                if (speedCondition) triggerReason += "速度达标 ";
                if (timeWindowCondition) triggerReason += "时间达标 ";
                Debug.Log($"毛球生成触发: {triggerReason}");
                
                // 重置所有检测参数
                accumulatedDistance = 0f;
                totalSpeedAccumulated = 0f;
                speedSamples = 0;
                touchStartTime = Time.time; // 重新开始计时
                GenerateFurball();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isTouchingPlayer = false;
            accumulatedDistance = 0f;
            totalSpeedAccumulated = 0f;       // 重置累积速度
            speedSamples = 0;                 // 重置采样次数
            touchStartTime = 0f;              // 重置时间
        }
    }

    void GenerateFurball()
    {
        // 随机选一个未用过的 prefab
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < furballPrefabs.Length; i++)
        {
            if (!usedPrefabIndices.Contains(i))
                availableIndices.Add(i);
        }
        if (availableIndices.Count == 0) return;

        int randomIndex = availableIndices[Random.Range(0, availableIndices.Count)];
        usedPrefabIndices.Add(randomIndex);

        Vector3 spawnPos = transform.position + Vector3.up * 0.5f;

        // 播放毛球生成音效（如果有的话）
        if (catAudioSource != null && furballSound != null)
            catAudioSource.PlayOneShot(furballSound);

        if (furballSpawnEffectPrefab != null)
        {
            GameObject effect = Instantiate(furballSpawnEffectPrefab, spawnPos, Quaternion.identity);
            Destroy(effect, 1f); // 1秒后销毁特效对象（根据特效时长调整）
        }

        Instantiate(furballPrefabs[randomIndex], transform.position + Vector3.up * 0.5f, Quaternion.identity);

        furballCount++;
        if (furballCount >= 3)
        {
            StartRunAway();
        }
    }

    void StartRunAway()
    {
        isRunningAway = true;
        isRoaming = false;

        if (animator != null)
            animator.SetTrigger("runaway");

        // 播放逃跑音效（如果有的话）
        if (catAudioSource != null && runawaySound != null)
            catAudioSource.PlayOneShot(runawaySound);
        
        // 播放逃跑特效（如果有的话）
        if (runAwayEffect != null)
        {
            GameObject effect = Instantiate(runAwayEffect, transform.position, Quaternion.identity);
            effect.transform.SetParent(transform); // 跟随猫移动
            Destroy(effect, 3f);
        }

        // 以远离玩家的方向逃跑
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 awayDir = (transform.position - player.transform.position).normalized;
            runAwayTarget = transform.position + awayDir * runAwayDistance;
        }
        else
        {
            runAwayTarget = transform.position + Vector3.forward * runAwayDistance; // 默认方向
        }
        // 关闭Trigger，防止再被摩擦
        runAwayTarget.y = 0.1f;
        GetComponent<Collider>().enabled = false;
    }

    void Update()
    {
        if (isCaught) return; // 只要被抓住，什么都不做

        Vector3 moveDir = Vector3.zero;
        Vector3 newPos = transform.position;

        if (isRunningAway)
            moveDir = (runAwayTarget - transform.position);
        else if (isRoaming)
            moveDir = (roamTarget - transform.position);

        // 朝向移动方向
        if (moveDir.magnitude > 0.05f)
        {
            Vector3 lookDir = new Vector3(moveDir.x, 0, moveDir.z);
            if (lookDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 10f * Time.deltaTime);
        }

        // 控制isMoving参数
        if (animator != null)
            animator.SetBool("isMoving", moveDir.magnitude > 0.05f && (isRunningAway || isRoaming));

        if (isRunningAway)
        {
            newPos = Vector3.MoveTowards(transform.position, runAwayTarget, runAwaySpeed * Time.deltaTime);
            if (Vector3.Distance(newPos, runAwayTarget) < 0.1f)
            {
                Destroy(gameObject);
                return;
            }
        }
        else if (isRoaming)
        {
            newPos = Vector3.MoveTowards(transform.position, roamTarget, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(newPos, roamTarget) < 0.2f)
            {
                isRoaming = false;
                waitTimer = Random.Range(waitTimeMin, waitTimeMax);
            }
        }
        else
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                PickNewRoamTarget();
                isRoaming = true;
            }
        }

        // 保证始终贴地
        newPos.y = 0.1f;
        transform.position = newPos;
    }



    void PickNewRoamTarget()
    {
        Vector2 randCircle = Random.insideUnitCircle * roamRadius;
        roamTarget = roamCenter.position + new Vector3(randCircle.x, 0, randCircle.y);
        roamTarget.y = 0.1f; // 地面高度可调整
    }
    
    // Meta SDK 的抓取事件接口方法
    // 这些方法会被Meta Interaction SDK自动调用
    public void WhenSelect()
    {
        isCaught = true;
        if (animator != null)
            animator.SetBool("isCaught", true);
        
        // 播放被抓住特效（如果有的话）
        if (caughtEffect != null)
        {
            GameObject effect = Instantiate(caughtEffect, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        Debug.Log("猫被抓住了！(Meta SDK WhenSelect)");
    }
    
    public void WhenUnselect()
    {
        isCaught = false;
        if (animator != null)
            animator.SetBool("isCaught", false);
        // 松开时立刻回到地面
        Vector3 pos = transform.position;
        pos.y = 0.1f; // 如果你有地形可用Raycast获取地形高度
        transform.position = pos;
        Debug.Log("猫被释放了！(Meta SDK WhenUnselect)");
    }

    // 备用方法名 - Meta SDK可能使用这些
    public void OnGrabBegin()
    {
        WhenSelect();
    }
    
    public void OnGrabEnd()
    {
        WhenUnselect();
    }
    
    // 另一种可能的接口
    public void OnSelectEntered()
    {
        WhenSelect();
    }
    
    public void OnSelectExited()
    {
        WhenUnselect();
    }


}
