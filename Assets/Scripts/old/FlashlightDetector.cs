using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashlightDetector : MonoBehaviour
{
  [Header("检测设置")]
  [SerializeField] private LayerMask targetLayers = -1; // 目标层级
  [SerializeField] private float detectionRange = 10f; // 检测距离
  [SerializeField] private float coneAngle = 45f; // 锥形角度
  [SerializeField] private float detectionRate = 0.1f; // 检测频率（秒）
  
  [Header("显示效果设置")]
  [SerializeField] private float displayDuration = 3f; // 显示持续时间
  [SerializeField] private float fadeOutDuration = 1f; // 渐隐持续时间
  
  [Header("调试设置")]
  [SerializeField] private bool showDebugRays = true; // 是否显示调试射线
  [SerializeField] private Color debugRayColor = Color.yellow;
  
  // 存储检测到的物体信息
  private Dictionary<GameObject, DetectedObject> detectedObjects = new Dictionary<GameObject, DetectedObject>();
  
  // 检测到的物体信息类
  private class DetectedObject
  {
      public MeshRenderer meshRenderer;
      public Material[] originalMaterials;
      public Coroutine fadeCoroutine;
      public bool isVisible;
      public float lastDetectionTime;
      
      public DetectedObject(MeshRenderer renderer)
      {
          meshRenderer = renderer;
          originalMaterials = renderer.materials;
          isVisible = false;
          lastDetectionTime = Time.time;
      }
  }
  
  private void Start()
  {
      // 开始检测协程
      StartCoroutine(DetectionCoroutine());
  }
  
  private IEnumerator DetectionCoroutine()
  {
      while (true)
      {
          PerformDetection();
          yield return new WaitForSeconds(detectionRate);
      }
  }
  
  private void PerformDetection()
  {
      // 获取前方方向
      Vector3 forward = transform.forward;
      Vector3 position = transform.position;
      
      // 创建检测到的物体列表
      HashSet<GameObject> currentlyDetected = new HashSet<GameObject>();
      
      // 使用多条射线形成锥形检测
      int rayCount = 20; // 射线数量
      float angleStep = coneAngle / rayCount;
      
      for (int i = 0; i < rayCount; i++)
      {
          for (int j = 0; j < rayCount; j++)
          {
              // 计算射线方向
              float horizontalAngle = (i - rayCount * 0.5f) * angleStep;
              float verticalAngle = (j - rayCount * 0.5f) * angleStep;
              
              Vector3 rayDirection = Quaternion.AngleAxis(horizontalAngle, transform.up) * 
                                   Quaternion.AngleAxis(verticalAngle, transform.right) * forward;
              
              // 发射射线
              if (Physics.Raycast(position, rayDirection, out RaycastHit hit, detectionRange, targetLayers))
              {
                  GameObject hitObject = hit.collider.gameObject;
                  currentlyDetected.Add(hitObject);
                  
                  // 如果是新检测到的物体
                  if (!detectedObjects.ContainsKey(hitObject))
                  {
                      MeshRenderer meshRenderer = hitObject.GetComponent<MeshRenderer>();
                      if (meshRenderer != null)
                      {
                          detectedObjects[hitObject] = new DetectedObject(meshRenderer);
                          ShowObject(hitObject);
                      }
                  }
                  else
                  {
                      // 更新最后检测时间
                      detectedObjects[hitObject].lastDetectionTime = Time.time;
                  }
              }
              
              // 调试射线
              if (showDebugRays)
              {
                  Debug.DrawRay(position, rayDirection * detectionRange, debugRayColor, detectionRate);
              }
          }
      }
      
      // 检查不再被检测到的物体
      List<GameObject> objectsToRemove = new List<GameObject>();
      foreach (var kvp in detectedObjects)
      {
          GameObject obj = kvp.Key;
          DetectedObject detectedObj = kvp.Value;
          
          if (!currentlyDetected.Contains(obj))
          {
              // 如果物体不再被检测到，开始渐隐过程
              if (detectedObj.isVisible && Time.time - detectedObj.lastDetectionTime > displayDuration)
              {
                  StartFadeOut(obj);
              }
          }
      }
  }
  
  private void ShowObject(GameObject obj)
  {
      if (detectedObjects.ContainsKey(obj))
      {
          DetectedObject detectedObj = detectedObjects[obj];
          
          // 停止之前的渐隐协程
          if (detectedObj.fadeCoroutine != null)
          {
              StopCoroutine(detectedObj.fadeCoroutine);
          }
          
          // 启用MeshRenderer
          detectedObj.meshRenderer.enabled = true;
          detectedObj.isVisible = true;
          
          // 重置材质透明度
          ResetMaterialAlpha(detectedObj);
          
          Debug.Log($"显示物体: {obj.name}");
      }
  }
  
  private void StartFadeOut(GameObject obj)
  {
      if (detectedObjects.ContainsKey(obj))
      {
          DetectedObject detectedObj = detectedObjects[obj];
          
          if (detectedObj.fadeCoroutine != null)
          {
              StopCoroutine(detectedObj.fadeCoroutine);
          }
          
          detectedObj.fadeCoroutine = StartCoroutine(FadeOutCoroutine(obj));
      }
  }
  
  private IEnumerator FadeOutCoroutine(GameObject obj)
  {
      DetectedObject detectedObj = detectedObjects[obj];
      float elapsedTime = 0f;
      
      // 确保材质支持透明度
      Material[] materials = detectedObj.meshRenderer.materials;
      Material[] fadeMaterials = new Material[materials.Length];
      
      for (int i = 0; i < materials.Length; i++)
      {
          fadeMaterials[i] = new Material(materials[i]);
          // 设置渲染模式为透明
          SetMaterialToTransparent(fadeMaterials[i]);
      }
      
      detectedObj.meshRenderer.materials = fadeMaterials;
      
      while (elapsedTime < fadeOutDuration)
      {
          elapsedTime += Time.deltaTime;
          float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeOutDuration);
          
          // 更新所有材质的透明度
          foreach (Material mat in fadeMaterials)
          {
              if (mat.HasProperty("_Color"))
              {
                  Color color = mat.color;
                  color.a = alpha;
                  mat.color = color;
              }
              else if (mat.HasProperty("_BaseColor"))
              {
                  Color color = mat.GetColor("_BaseColor");
                  color.a = alpha;
                  mat.SetColor("_BaseColor", color);
              }
          }
          
          yield return null;
      }
      
      // 隐藏物体
      detectedObj.meshRenderer.enabled = false;
      detectedObj.isVisible = false;
      
      // 恢复原始材质
      detectedObj.meshRenderer.materials = detectedObj.originalMaterials;
      
      Debug.Log($"隐藏物体: {obj.name}");
      
      // 从字典中移除
      detectedObjects.Remove(obj);
      
      // 清理临时材质
      foreach (Material mat in fadeMaterials)
      {
          if (mat != null)
          {
              DestroyImmediate(mat);
          }
      }
  }
  
  private void SetMaterialToTransparent(Material material)
  {
      // 对于URP材质
      if (material.HasProperty("_Surface"))
      {
          material.SetFloat("_Surface", 1); // 设置为透明
          material.SetFloat("_Blend", 0); // Alpha blend
      }
      
      // 对于标准材质
      if (material.HasProperty("_Mode"))
      {
          material.SetFloat("_Mode", 3); // Transparent mode
      }
      
      // 设置渲染队列
      material.renderQueue = 3000;
      
      // 启用关键字
      material.EnableKeyword("_ALPHABLEND_ON");
      material.DisableKeyword("_ALPHATEST_ON");
      material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
  }
  
  private void ResetMaterialAlpha(DetectedObject detectedObj)
  {
      foreach (Material mat in detectedObj.meshRenderer.materials)
      {
          if (mat.HasProperty("_Color"))
          {
              Color color = mat.color;
              color.a = 1f;
              mat.color = color;
          }
          else if (mat.HasProperty("_BaseColor"))
          {
              Color color = mat.GetColor("_BaseColor");
              color.a = 1f;
              mat.SetColor("_BaseColor", color);
          }
      }
  }
  
  private void OnDrawGizmosSelected()
  {
      // 绘制检测锥形
      Gizmos.color = Color.yellow;
      Vector3 forward = transform.forward;
      Vector3 position = transform.position;
      
      // 绘制中心线
      Gizmos.DrawRay(position, forward * detectionRange);
      
      // 绘制锥形边界
      float halfAngle = coneAngle * 0.5f;
      Vector3 rightBoundary = Quaternion.AngleAxis(halfAngle, transform.up) * forward;
      Vector3 leftBoundary = Quaternion.AngleAxis(-halfAngle, transform.up) * forward;
      Vector3 upBoundary = Quaternion.AngleAxis(halfAngle, transform.right) * forward;
      Vector3 downBoundary = Quaternion.AngleAxis(-halfAngle, transform.right) * forward;
      
      Gizmos.DrawRay(position, rightBoundary * detectionRange);
      Gizmos.DrawRay(position, leftBoundary * detectionRange);
      Gizmos.DrawRay(position, upBoundary * detectionRange);
      Gizmos.DrawRay(position, downBoundary * detectionRange);
      
      // 绘制锥形底面
      Vector3 endPoint = position + forward * detectionRange;
      float endRadius = detectionRange * Mathf.Tan(halfAngle * Mathf.Deg2Rad);
      
      Gizmos.color = Color.yellow * 0.3f;
      Gizmos.DrawWireSphere(endPoint, endRadius);
  }
  
  private void OnDestroy()
  {
      // 清理所有协程和临时材质
      foreach (var kvp in detectedObjects)
      {
          DetectedObject detectedObj = kvp.Value;
          if (detectedObj.fadeCoroutine != null)
          {
              StopCoroutine(detectedObj.fadeCoroutine);
          }
      }
      detectedObjects.Clear();
  }
}