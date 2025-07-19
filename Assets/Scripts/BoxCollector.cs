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


    void Start()
    {
        foreach (var req in requirements)
        {
            collectedCounts[req.furType] = 0;
        }
        RefreshTexts();
    }

    public void CollectFurball(string furType, GameObject furballPrefab)
    {
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
                Instantiate(colorRewardDict[lockedFurType], transform.position, Quaternion.identity);
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
                Instantiate(goalPrefab, transform.position, Quaternion.identity);
            }
        }

        // 3. 箱子消失
        gameObject.GetComponent<MeshRenderer>().enabled = false;

        // 4. 在玩家前方生成新箱子
        StartCoroutine(GenerateAfterDelay(1f));
    }
    void Explode()
    {
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

        BoxCollector newBoxCollector = newBox.GetComponent<BoxCollector>();
        if (newBoxCollector != null && newBoxCollector.furballContainer != null)
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

        RefreshTexts();
    }
    
    void RefreshTexts()
    {
        // 单色箱子
        if (requirements.Count == 1 && (string.IsNullOrEmpty(requirements[0].furType) || requirements[0].furType == "Any"))
        {
            if (!string.IsNullOrEmpty(lockedFurType))
            {
                int remain = requirements[0].requiredCount - collectedCounts[lockedFurType];
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
                var req = requirements.Find(r => r.furType == ft.furType);
                if (req != null && ft.text != null)
                {
                    int remain = req.requiredCount - collectedCounts[ft.furType];
                    ft.text.text = "X" + remain;
                }
            }
        }
    }
}
