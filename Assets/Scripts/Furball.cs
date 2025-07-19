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
    
    [Header("音效接口（可选）")]
    public AudioSource furballAudioSource;  // 毛球音效源
    public AudioClip collectSound;          // 收集音效
    
    [Header("特效接口（可选）")]
    public GameObject fadeEffect;           // 消失特效

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

    void OnTriggerEnter(Collider other)
    {
        if (!isCollected && other.CompareTag("Box"))
        {
            Collect(other.gameObject);
        }
    }

    void OnDestroy()
    {
        // 清理DOTween资源，避免内存泄漏
        if (currentTween != null)
        {
            currentTween.Kill();
            currentTween = null;
        }
    }

    void FadeAndDestroy()
    {
        // 播放消失特效（如果有的话）
        if (fadeEffect != null)
        {
            GameObject effect = Instantiate(fadeEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        Renderer r = GetComponentInChildren<Renderer>();
        if (r != null && r.material.HasProperty("_Color"))
        {
            Color c = r.material.color;
            var fadeTween = DOTween.To(() => c.a, x => {
                c.a = x;
                r.material.color = c;
            }, 0, fadeDuration).OnComplete(() => Destroy(gameObject));
            
            // 确保fade动画也能被正确管理
            currentTween = fadeTween;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Collect(GameObject box)
    {
        isCollected = true;
        
        // 播放收集音效（如果有的话）
        if (furballAudioSource != null && collectSound != null)
            furballAudioSource.PlayOneShot(collectSound);
        
        if (collectEffectPrefab != null)
        {
            Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // 通知GameManager毛球被收集（计分系统）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnFurballCollected();
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
