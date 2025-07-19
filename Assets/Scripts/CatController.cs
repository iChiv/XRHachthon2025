using UnityEngine;
using System.Collections.Generic;

public class CatController : MonoBehaviour
{
    public float requiredFrictionDistance = 2.0f; // 需要摩擦的距离
    public GameObject[] furballPrefabs;           // 毛球预制体数组
    public float runAwayDistance = 5f;            // 逃跑距离
    public float runAwaySpeed = 3f;               // 逃跑速度
    public GameObject furballSpawnEffectPrefab; // 毛球生成特效


    private float accumulatedDistance = 0f;
    private Vector3 lastPlayerPosition;
    private bool isTouchingPlayer = false;
    private int furballCount = 0;
    private List<int> usedPrefabIndices = new List<int>();
    private bool isRunningAway = false;
    private Vector3 runAwayTarget;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isTouchingPlayer = true;
            lastPlayerPosition = other.transform.position;
            accumulatedDistance = 0f;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (isTouchingPlayer && furballCount < 3 && other.CompareTag("Player") && !isRunningAway)
        {
            Vector3 currentPlayerPosition = other.transform.position;
            float distance = Vector3.Distance(currentPlayerPosition, lastPlayerPosition);
            accumulatedDistance += distance;
            lastPlayerPosition = currentPlayerPosition;

            if (accumulatedDistance >= requiredFrictionDistance)
            {
                accumulatedDistance = 0f;
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

        if (furballSpawnEffectPrefab != null)
        {
            GameObject effect = Instantiate(furballSpawnEffectPrefab, spawnPos, Quaternion.identity);
            Destroy(effect, 1f); // 2秒后销毁特效对象（根据特效时长调整）
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
        GetComponent<Collider>().enabled = false;
    }

    void Update()
    {
        if (isRunningAway)
        {
            transform.position = Vector3.MoveTowards(transform.position, runAwayTarget, runAwaySpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, runAwayTarget) < 0.1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
