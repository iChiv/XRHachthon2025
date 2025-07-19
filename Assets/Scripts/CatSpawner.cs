using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CatSpawner : MonoBehaviour
{
    [Header("小猫生成设置")]
    public List<GameObject> catPrefabs; // 各种小猫预制体
    public int maxCats = 6;
    public float spawnRadius = 10f;
    public Transform centerPoint; // 生成中心点（如主角/场景中心）
    public float spawnInterval = 5f; // 生成间隔（秒）

    private List<GameObject> spawnedCats = new List<GameObject>();
    private float timer = 0f;
    private bool initialSpawnDone = false;
    private Dictionary<int, string> catPrefabNames = new Dictionary<int, string>(); // 缓存prefab名称
    private Dictionary<int, int> catTypeCounts = new Dictionary<int, int>(); // 跟踪各类型猫的数量

    void Start()
    {
        if (centerPoint == null) centerPoint = this.transform;
        
        // 初始化prefab名称缓存和计数器
        for (int i = 0; i < catPrefabs.Count; i++)
        {
            if (catPrefabs[i] != null)
            {
                catPrefabNames[i] = catPrefabs[i].name;
                catTypeCounts[i] = 0;
            }
        }
        
        InitialSpawn();
    }

    void Update()
    {
        // 检查关键组件是否还有效，防止场景重新加载时的错误
        if (this == null || catPrefabs == null) return;
        
        // 移除已销毁的小猫并更新计数
        for (int i = spawnedCats.Count - 1; i >= 0; i--)
        {
            if (spawnedCats[i] == null)
            {
                // 找出这只猫是哪种类型并减少计数
                // 由于猫已经被销毁，我们无法直接获取类型，所以重新计算所有计数
                UpdateCatTypeCounts();
                spawnedCats.RemoveAt(i);
                break; // 每帧只处理一个，避免性能问题
            }
        }

        // 保证每种小猫至少有一只
        for (int i = 0; i < catPrefabs.Count; i++)
        {
            if (catTypeCounts.ContainsKey(i) && catTypeCounts[i] == 0 && spawnedCats.Count < maxCats)
            {
                SpawnCat(i);
            }
        }

        if (!initialSpawnDone) return;

        // 定时生成
        timer += Time.deltaTime;
        if (timer >= spawnInterval && spawnedCats.Count < maxCats)
        {
            timer = 0f;
            SpawnCat(Random.Range(0, catPrefabs.Count));
        }
    }

    // 更新各类型猫的数量统计
    private void UpdateCatTypeCounts()
    {
        // 重置所有计数
        foreach (var key in catTypeCounts.Keys.ToList())
        {
            catTypeCounts[key] = 0;
        }
        
        // 重新计算
        foreach (var cat in spawnedCats)
        {
            if (cat != null)
            {
                for (int i = 0; i < catPrefabs.Count; i++)
                {
                    if (catPrefabNames.ContainsKey(i) && cat.name.Contains(catPrefabNames[i]))
                    {
                        catTypeCounts[i]++;
                        break;
                    }
                }
            }
        }
    }

    // 生成指定类型小猫
    void SpawnCat(int prefabIndex)
    {
        if (catPrefabs == null || catPrefabs.Count == 0 || prefabIndex >= catPrefabs.Count) return;
        GameObject prefab = catPrefabs[prefabIndex];
        if (prefab == null) return;
        
        Vector3 pos = GetRandomPosition();
        GameObject cat = Instantiate(prefab, pos, Quaternion.identity);
        cat.name = prefab.name + "_Cat";
        spawnedCats.Add(cat);
        
        // 更新计数器
        if (catTypeCounts.ContainsKey(prefabIndex))
        {
            catTypeCounts[prefabIndex]++;
        }
    }

    // 初始一次性生成
    void InitialSpawn()
    {
        // 1. 每种至少一只
        for (int i = 0; i < catPrefabs.Count && spawnedCats.Count < maxCats; i++)
        {
            SpawnCat(i);
        }
        // 2. 补足到maxCats
        while (spawnedCats.Count < maxCats)
        {
            int randomIndex = Random.Range(0, catPrefabs.Count);
            SpawnCat(randomIndex);
        }
        initialSpawnDone = true;
        timer = 0f;
    }

    // 获取随机位置
    Vector3 GetRandomPosition()
    {
        // 检查centerPoint是否被销毁
        if (centerPoint == null)
        {
            // 如果centerPoint被销毁，使用当前对象的位置
            centerPoint = this.transform;
        }
        
        Vector2 randCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 pos = centerPoint.position + new Vector3(randCircle.x, 0, randCircle.y);
        pos.y = 0.1f; // 地面高度
        return pos;
    }
}
