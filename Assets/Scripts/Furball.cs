using UnityEngine;
using DG.Tweening;

public class Furball : MonoBehaviour
{
    public string furType;
    public GameObject furballPrefab; // 用于在箱子中实例化的Prefab
    public float upwardHeight = 1.5f;
    public float upwardDuration = 0.4f;
    public float dropDuration = 5f;
    public float horizontalRandomRange = 0.3f;
    public float groundY = 0.1f;
    public float collectDistance = 1.0f;
    public GameObject collectEffectPrefab;
    public float fadeDuration = 0.3f;

    private bool isCollected = false;
    private Tween currentTween;

    void Start()
    {
        Vector2 randomXZ = Random.insideUnitCircle * horizontalRandomRange;
        Vector3 upwardTarget = transform.position + new Vector3(randomXZ.x, upwardHeight, randomXZ.y);

        currentTween = transform.DOMove(upwardTarget, upwardDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
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
            GameObject box = GameObject.FindGameObjectWithTag("Box");
            if (box != null)
            {
                float dist = Vector3.Distance(transform.position, box.transform.position);
                if (dist <= collectDistance)
                {
                    Collect(box);
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

    void Collect(GameObject box)
    {
        isCollected = true;
        if (collectEffectPrefab != null)
        {
            Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
        }
        // 通知Box收集，并传递furType和furballPrefab
        BoxCollector boxCollector = box.GetComponent<BoxCollector>();
        if (boxCollector != null)
        {
            boxCollector.CollectFurball(furType, furballPrefab);
        }
        if (currentTween != null) currentTween.Kill();
        Destroy(gameObject); // 原场景的毛球销毁
    }
}
