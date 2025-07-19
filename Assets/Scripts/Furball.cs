using UnityEngine;
using DG.Tweening;

public class Furball : MonoBehaviour
{
    public string furType;
    public float upwardHeight = 1.5f;      // 向上飞多高
    public float upwardDuration = 0.4f;    // 向上飞时间
    public float dropDuration = 5f;      // 下落时间
    public float horizontalRandomRange = 0.3f; // 水平轻微随机
    public float groundY = 0.1f;           // 落地高度
    public float collectDistance = 1.0f;
    public GameObject collectEffectPrefab;
    public float fadeDuration = 0.3f;

    private bool isCollected = false;
    private Tween currentTween;

    void Start()
    {
        // 1. 计算目标点（轻微水平随机 + 向上）
        Vector2 randomXZ = Random.insideUnitCircle * horizontalRandomRange;
        Vector3 upwardTarget = transform.position + new Vector3(randomXZ.x, upwardHeight, randomXZ.y);

        // 2. 向上飞动画
        currentTween = transform.DOMove(upwardTarget, upwardDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // 3. 垂直下落动画
                Vector3 dropTarget = new Vector3(upwardTarget.x, groundY, upwardTarget.z);
                currentTween = transform.DOMove(dropTarget, dropDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        FadeAndDestroy();
                    });
            });
    }

    void Update()
    {
        if (!isCollected)
        {
            GameObject player = GameObject.FindGameObjectWithTag("playerrig");
            if (player != null)
            {
                float dist = Vector3.Distance(transform.position, player.transform.position);
                if (dist <= collectDistance)
                {
                    Collect(player);
                }
            }
        }
    }

    void FadeAndDestroy()
    {
        Renderer r = GetComponentInChildren<Renderer>();
        if (r != null && r.material.HasProperty("_Color"))
        {
            Color c = r.material.color;
            DOTween.To(() => c.a, x => {
                c.a = x;
                r.material.color = c;
            }, 0, fadeDuration).OnComplete(() => Destroy(gameObject));
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Collect(GameObject player)
    {
        isCollected = true;
        if (collectEffectPrefab != null)
        {
            Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
        }
        PlayerFurballCollector collector = player.GetComponent<PlayerFurballCollector>();
        if (collector != null)
        {
            collector.CollectFurball(furType);
        }
        if (currentTween != null) currentTween.Kill();
        Destroy(gameObject);
    }
}
