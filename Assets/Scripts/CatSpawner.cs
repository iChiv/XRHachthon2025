using UnityEngine;
using System.Collections.Generic;

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

    void Start()
    {
        if (centerPoint == null) centerPoint = this.transform;
        InitialSpawn();
    }

    void Update()
    {
        // 移除已销毁的小猫
        spawnedCats.RemoveAll(cat => cat == null);

        // 保证每种小猫至少有一只
        for (int i = 0; i < catPrefabs.Count; i++)
        {
            if (!HasCatOfType(i) && spawnedCats.Count < maxCats)
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

    // 检查场上是否还有第index种小猫
    bool HasCatOfType(int index)
    {
        GameObject prefab = catPrefabs[index];
        foreach (var cat in spawnedCats)
        {
            if (cat != null && cat.name.Contains(prefab.name))
                return true;
        }
        return false;
    }

    // 生成指定类型小猫
    void SpawnCat(int prefabIndex)
    {
        if (catPrefabs == null || catPrefabs.Count == 0) return;
        GameObject prefab = catPrefabs[prefabIndex];
        Vector3 pos = GetRandomPosition();
        GameObject cat = Instantiate(prefab, pos, Quaternion.identity);
        cat.name = prefab.name + "_Cat";
        spawnedCats.Add(cat);
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
        Vector2 randCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 pos = centerPoint.position + new Vector3(randCircle.x, 0, randCircle.y);
        pos.y = 0.1f; // 地面高度
        return pos;
    }
}
