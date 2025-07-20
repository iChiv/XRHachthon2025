using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections;

public class BoxCollector : MonoBehaviour
{
    public TextMeshPro monoText; // Inspector拖拽赋值
    [System.Serializable]
    public class FurballText
    {
        public string furType;
        public TextMeshPro text;
    }
    public List<FurballText> furballTexts = new List<FurballText>();

    [System.Serializable]
    public class FurballRequirement
    {
        public string furType;
        public int requiredCount;
    }

    [System.Serializable]
    public class ColorReward
    {
        public string furType;         // 颜色名
        public GameObject rewardPrefab; // 对应颜色的奖励物品
    }
    public List<FurballRequirement> requirements = new List<FurballRequirement>();
    private Dictionary<string, int> collectedCounts = new Dictionary<string, int>();

    [Header("同色箱奖励（颜色-预制体）")]
    public List<ColorReward> colorRewards = new List<ColorReward>();
    private Dictionary<string, GameObject> colorRewardDict = new Dictionary<string, GameObject>();
    private string lockedFurType = null; // 同色箱锁定颜色

    public Transform furballContainer;
    public Vector3 spawnRegionCenter = Vector3.zero;
    public Vector3 spawnRegionSize = new Vector3(0.5f, 0.5f, 0.5f);

    [Header("全部达成生成目标")]
    public GameObject goalPrefab; // 目标预制体

    [Header("爆炸相关")]
    public GameObject explosionEffectPrefab;
    public List<GameObject> boxPrefabs; // 可选箱子预制体列表
    public float newBoxDistance = 2f;   // 新箱子出现在玩家前方的距离

    private static GameObject lastBoxPrefab = null; // 静态变量，跨实例追踪上一个箱子
    
    [Header("音效接口（可选）")]
    public AudioSource boxAudioSource;  // 箱子音效源
    public AudioClip collectSound;      // 收集毛球音效
    public AudioClip explosionSound;    // 爆炸音效
    public AudioClip rewardSound;       // 奖励合成音效
    
    [Header("特效接口（可选）")]
    public GameObject collectSuccessEffect; // 收集成功特效
    public GameObject rewardEffect;         // 奖励合成特效


    void Start()
    {
        // 标准化并初始化需求字典
        foreach (var req in requirements)
        {
            string normalizedType = NormalizeFurType(req.furType);
            collectedCounts[normalizedType] = 0;
            // 更新requirements中的furType为标准化版本
            req.furType = normalizedType;
        }
        
        // 初始化颜色奖励字典
        colorRewardDict.Clear();
        foreach (var colorReward in colorRewards)
        {
            string normalizedType = NormalizeFurType(colorReward.furType);
            if (!string.IsNullOrEmpty(normalizedType) && colorReward.rewardPrefab != null)
            {
                colorRewardDict[normalizedType] = colorReward.rewardPrefab;
                // 更新colorReward中的furType为标准化版本
                colorReward.furType = normalizedType;
            }
        }
        
        RefreshTexts();
    }
    
    /// <summary>
    /// 重置箱子到初始状态（用于生成新箱子时）
    /// </summary>
    public void ResetToInitialState()
    {
        // 1. 确保MeshRenderer启用
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = true;
        }
        
        // 2. 清理毛球容器
        if (furballContainer != null)
        {
            for (int i = furballContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = furballContainer.GetChild(i);
                if (child != null)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
        
        // 3. 重置收集状态
        collectedCounts.Clear();
        lockedFurType = null;
        
        // 4. 重新初始化需求字典
        foreach (var req in requirements)
        {
            string normalizedType = NormalizeFurType(req.furType);
            collectedCounts[normalizedType] = 0;
            // 更新requirements中的furType为标准化版本
            req.furType = normalizedType;
        }
        
        // 5. 重新初始化颜色奖励字典
        colorRewardDict.Clear();
        foreach (var colorReward in colorRewards)
        {
            string normalizedType = NormalizeFurType(colorReward.furType);
            if (!string.IsNullOrEmpty(normalizedType) && colorReward.rewardPrefab != null)
            {
                colorRewardDict[normalizedType] = colorReward.rewardPrefab;
                // 更新colorReward中的furType为标准化版本
                colorReward.furType = normalizedType;
            }
        }
        
        // 6. 刷新文本显示
        RefreshTexts();
        
        Debug.Log($"箱子已重置到初始状态: {gameObject.name}");
    }

    // 标准化颜色名称，避免大小写不一致问题
    public string NormalizeFurType(string furType)
    {
        if (string.IsNullOrEmpty(furType))
            return furType;
        
        // 统一转换为首字母大写格式
        return char.ToUpper(furType[0]) + furType.Substring(1).ToLower();
    }

    public void CollectFurball(string furType, GameObject furballPrefab)
    {
        // 标准化输入的毛球类型
        furType = NormalizeFurType(furType);
        
        // 判断是否同色箱（只配置一个需求，furType为空或"Any"）
        bool isMonoColorBox = (requirements.Count == 1 && (string.IsNullOrEmpty(requirements[0].furType) || requirements[0].furType == "Any"));

        if (isMonoColorBox)
        {
            // 1. 首次收集，锁定颜色
            if (string.IsNullOrEmpty(lockedFurType))
            {
                lockedFurType = furType;
                collectedCounts[lockedFurType] = 0;
            }
            // 2. 只能收集锁定颜色
            if (furType != lockedFurType)
            {
                Debug.Log("收集到不匹配的毛球颜色，箱子爆炸！");
                Explode();
                return;
            }
            collectedCounts[lockedFurType]++;
            
            // 播放收集成功音效（如果有的话）
            if (boxAudioSource != null && collectSound != null)
                boxAudioSource.PlayOneShot(collectSound);
            
            // 播放收集成功特效（如果有的话）
            if (collectSuccessEffect != null)
            {
                GameObject effect = Instantiate(collectSuccessEffect, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                Destroy(effect, 2f);
            }
            
            if (collectedCounts[lockedFurType] > requirements[0].requiredCount)
            {
                Debug.Log("收集数量超出需求，箱子爆炸！");
                Explode();
                return;
            }
            // 3. 生成毛球到箱子上
            if (furballPrefab != null && furballContainer != null)
            {
                Vector3 localPos = spawnRegionCenter + new Vector3(
                    Random.Range(-spawnRegionSize.x / 2, spawnRegionSize.x / 2),
                    Random.Range(-spawnRegionSize.y / 2, spawnRegionSize.y / 2),
                    Random.Range(-spawnRegionSize.z / 2, spawnRegionSize.z / 2)
                );
                GameObject newFurball = Instantiate(furballPrefab, furballContainer);
                newFurball.transform.localPosition = localPos;
                newFurball.transform.localRotation = Random.rotation;

                var furballScript = newFurball.GetComponent<Furball>();
                if (furballScript != null) furballScript.enabled = false;
                if (newFurball.GetComponent<Rigidbody>() == null)
                    newFurball.AddComponent<Rigidbody>();
                Rigidbody rb = newFurball.GetComponent<Rigidbody>();
                rb.isKinematic = false;
                if (newFurball.GetComponent<Collider>() == null)
                    newFurball.AddComponent<SphereCollider>();
                SphereCollider sc = newFurball.GetComponent<SphereCollider>();
                sc.isTrigger = false;
            }
            // 4. 检查是否完成
            if (collectedCounts[lockedFurType] == requirements[0].requiredCount)
            {
                Debug.Log("全部需求达成！");
                OnAllRequirementMet();
            }
            return;
        }

        // ===== 多色箱逻辑 =====
        if (!collectedCounts.ContainsKey(furType))
        {
            Debug.Log("收集到不需要的毛球：" + furType);
            Explode();
            return;
        }

        collectedCounts[furType]++;
        
        // 播放收集成功音效（如果有的话）
        if (boxAudioSource != null && collectSound != null)
            boxAudioSource.PlayOneShot(collectSound);
        
        // 播放收集成功特效（如果有的话）
        if (collectSuccessEffect != null)
        {
            GameObject effect = Instantiate(collectSuccessEffect, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        if (collectedCounts[furType] > requirements.Find(r => r.furType == furType).requiredCount)
        {
            Debug.Log("收集数量超出需求，箱子爆炸！");
            Explode();
            return;
        }

        if (furballPrefab != null && furballContainer != null)
        {
            Vector3 localPos = spawnRegionCenter + new Vector3(
                Random.Range(-spawnRegionSize.x / 2, spawnRegionSize.x / 2),
                Random.Range(-spawnRegionSize.y / 2, spawnRegionSize.y / 2),
                Random.Range(-spawnRegionSize.z / 2, spawnRegionSize.z / 2)
            );
            GameObject newFurball = Instantiate(furballPrefab, furballContainer);
            newFurball.transform.localPosition = localPos;
            newFurball.transform.localRotation = Random.rotation;

            var furballScript = newFurball.GetComponent<Furball>();
            if (furballScript != null) furballScript.enabled = false;
            if (newFurball.GetComponent<Rigidbody>() == null)
                newFurball.AddComponent<Rigidbody>();
            Rigidbody rb = newFurball.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            if (newFurball.GetComponent<Collider>() == null)
                newFurball.AddComponent<SphereCollider>();
            SphereCollider sc = newFurball.GetComponent<SphereCollider>();
            sc.isTrigger = false;
        }

        if (IsAllRequirementMet())
        {
            Debug.Log("全部需求达成！");
            OnAllRequirementMet();
        }
    }

    IEnumerator GenerateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // 检查游戏状态，防止场景重新加载时生成箱子
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameManager.GameState.Playing)
        {
            yield break; // 如果游戏不在进行中，停止生成
        }
        
        GenerateNewBox();
        Destroy(gameObject);
    }

    bool IsAllRequirementMet()
    {
        foreach (var req in requirements)
        {
            if (collectedCounts[req.furType] < req.requiredCount)
                return false;
        }
        return true;
    }

    void OnAllRequirementMet()
    {
        // 播放奖励合成音效（如果有的话）
        if (boxAudioSource != null && rewardSound != null)
            boxAudioSource.PlayOneShot(rewardSound);
        
        // 播放奖励合成特效（如果有的话）
        if (rewardEffect != null)
        {
            GameObject effect = Instantiate(rewardEffect, transform.position + Vector3.up * 1f, Quaternion.identity);
            Destroy(effect, 3f);
        }
        
        // 1. 播放特效
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        // 2. 生成奖励
        bool isMonoColorBox = (requirements.Count == 1 && (string.IsNullOrEmpty(requirements[0].furType) || requirements[0].furType == "Any"));
        if (isMonoColorBox)
        {
            if (!string.IsNullOrEmpty(lockedFurType) && colorRewardDict.ContainsKey(lockedFurType))
            {
                GameObject reward = Instantiate(colorRewardDict[lockedFurType], transform.position, Quaternion.identity);
                
                // 通知GameManager获得分数（合成奖励）
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnRewardSynthesized(reward.name);
                }
            }
            else
            {
                Debug.LogWarning("未找到对应颜色的奖励预制体：" + lockedFurType);
            }
        }
        else
        {
            if (goalPrefab != null)
            {
                GameObject reward = Instantiate(goalPrefab, transform.position, Quaternion.identity);
                
                // 通知GameManager获得分数（合成多色奖励）
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnRewardSynthesized(reward.name);
                }
            }
        }

        // 3. 箱子消失
        gameObject.GetComponent<MeshRenderer>().enabled = false;

        // 4. 在玩家前方生成新箱子
        StartCoroutine(GenerateAfterDelay(1f));
    }
    
    void OnDestroy()
    {
        // 停止所有协程，防止内存泄漏
        StopAllCoroutines();
    }
    void Explode()
    {
        // 播放爆炸音效（如果有的话）
        if (boxAudioSource != null && explosionSound != null)
            boxAudioSource.PlayOneShot(explosionSound);
        
        // 1. 播放爆炸特效
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        if (furballContainer != null)
        {
            for (int i = furballContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(furballContainer.GetChild(i).gameObject);
            }
        }

        // 2. 箱子消失
        gameObject.GetComponent<MeshRenderer>().enabled = false;

        // 3. 在玩家前方生成新箱子
        StartCoroutine(GenerateAfterDelay(1f));
    }

    void GenerateNewBox()
    {
        // 找到玩家
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || boxPrefabs == null || boxPrefabs.Count == 0) return;

        // 随机选择一个与上次不同的箱子预制体
        GameObject chosenPrefab = null;
        int tryCount = 0;
        do
        {
            chosenPrefab = boxPrefabs[Random.Range(0, boxPrefabs.Count)];
            tryCount++;
        } while (chosenPrefab == lastBoxPrefab && boxPrefabs.Count > 1 && tryCount < 10);

        lastBoxPrefab = chosenPrefab;

        // 算出玩家前方的位置
        Vector3 forward = player.transform.forward;
        Vector3 spawnPos = player.transform.position + forward.normalized * newBoxDistance;
        spawnPos.y = 1.0f; // 可根据箱子高度调整

        // 生成新箱子
        GameObject newBox = Instantiate(chosenPrefab, spawnPos, Quaternion.identity);
        newBox.SetActive(true);
        
        // ===== 关键修复：确保新箱子是干净的初始状态 =====
        BoxCollector newBoxCollector = newBox.GetComponent<BoxCollector>();
        if (newBoxCollector != null)
        {
            // 使用专门的重置方法确保箱子状态干净
            newBoxCollector.ResetToInitialState();
            Debug.Log($"生成新的干净箱子: {newBox.name}");
        }

        // 清理当前箱子的毛球（即将销毁的箱子）
        if (furballContainer != null)
        {
            for (int i = furballContainer.childCount - 1; i >= 0; i--)
            {
                var child = furballContainer.GetChild(i).gameObject;
                var collider = child.GetComponent<Collider>();
                if (collider != null) collider.enabled = false; // 先禁用
                child.SetActive(false); // 先隐藏
                Destroy(child, 0.1f);   // 延迟销毁
            }
        }
    }
    
    void RefreshTexts()
    {
        // 单色箱子
        if (requirements.Count == 1 && (string.IsNullOrEmpty(requirements[0].furType) || requirements[0].furType == "Any"))
        {
            if (!string.IsNullOrEmpty(lockedFurType) && collectedCounts.ContainsKey(lockedFurType))
            {
                int remain = requirements[0].requiredCount - collectedCounts[lockedFurType];
                remain = Mathf.Max(0, remain); // 确保不显示负数
                if (monoText != null)
                    monoText.text = "X" + remain;
            }
            else
            {
                if (monoText != null)
                    monoText.text = "X" + requirements[0].requiredCount;
            }
        }
        else // 多色箱子
        {
            foreach (var ft in furballTexts)
            {
                var req = requirements.Find(r => r.furType.Equals(ft.furType, System.StringComparison.OrdinalIgnoreCase));
                if (req != null && ft.text != null)
                {
                    int currentCount = collectedCounts.ContainsKey(req.furType) ? collectedCounts[req.furType] : 0;
                    int remain = req.requiredCount - currentCount;
                    remain = Mathf.Max(0, remain); // 确保不显示负数
                    ft.text.text = "X" + remain;
                }
            }
        }
    }
}
