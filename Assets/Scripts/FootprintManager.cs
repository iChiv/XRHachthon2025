using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FootprintManager : MonoBehaviour
{
  [Header("路径设置")]
  [SerializeField] private Transform pointA; // 起始点A
  [SerializeField] private Transform pointB; // 终点B
  
  [Header("脚印预制体")]
  [SerializeField] private GameObject leftFootprintPrefab; // 左脚脚印预制体
  [SerializeField] private GameObject rightFootprintPrefab; // 右脚脚印预制体
  
  [Header("间距设置")]
  [SerializeField] private float forwardSpacing = 1f; // 前后间距
  [SerializeField] private float sideSpacing = 0.3f; // 左右间距（从中心线到脚印的距离）
  
  [Header("脚印设置")]
  [SerializeField] private bool alignToGround = true; // 是否贴合地面
  [SerializeField] private LayerMask groundLayer = 1; // 地面层级
  [SerializeField] private float groundCheckDistance = 5f; // 地面检测距离
  [SerializeField] private Vector3 footprintOffset = Vector3.zero; // 脚印偏移
  
  [Header("生成设置")]
  [SerializeField] private bool generateOnStart = false; // 是否在开始时自动生成
  [SerializeField] private bool clearExistingFootprints = true; // 生成前是否清除已有脚印
  
  [Header("调试设置")]
  [SerializeField] private bool showPathGizmos = true; // 是否显示路径辅助线
  [SerializeField] private bool showFootprintGizmos = true; // 是否显示脚印位置辅助点
  [SerializeField] private Color pathColor = Color.yellow;
  [SerializeField] private Color leftFootColor = Color.red;
  [SerializeField] private Color rightFootColor = Color.blue;
  
  // 存储生成的脚印
  private List<GameObject> generatedFootprints = new List<GameObject>();
  private List<Vector3> footprintPositions = new List<Vector3>(); // 用于调试显示
  
  public System.Action OnFootprintsGenerated; // 脚印生成完成事件
  public int FootprintCount => generatedFootprints.Count; // 脚印总数
  
  private void Start()
  {
      if (generateOnStart)
      {
          GenerateFootprints();
      }
  }
  
  [ContextMenu("生成脚印")]
  public void GenerateFootprints()
  {
      if (!ValidateComponents())
      {
          Debug.LogError("FootprintManager: 缺少必要的组件引用！");
          return;
      }
      
      if (clearExistingFootprints)
      {
          ClearFootprints();
      }
      
      StartCoroutine(GenerateFootprintsCoroutine());
  }
  
  private bool ValidateComponents()
  {
      if (pointA == null)
      {
          Debug.LogError("FootprintManager: Point A 未设置！");
          return false;
      }
      
      if (pointB == null)
      {
          Debug.LogError("FootprintManager: Point B 未设置！");
          return false;
      }
      
      if (leftFootprintPrefab == null)
      {
          Debug.LogError("FootprintManager: 左脚脚印预制体未设置！");
          return false;
      }
      
      if (rightFootprintPrefab == null)
      {
          Debug.LogError("FootprintManager: 右脚脚印预制体未设置！");
          return false;
      }
      
      return true;
  }
  
  private IEnumerator GenerateFootprintsCoroutine()
  {
      Vector3 startPos = pointA.position;
      Vector3 endPos = pointB.position;
      
      // 计算路径方向和距离
      Vector3 pathDirection = (endPos - startPos).normalized;
      float pathDistance = Vector3.Distance(startPos, endPos);
      
      // 计算需要生成的脚印数量
      int footprintCount = Mathf.FloorToInt(pathDistance / forwardSpacing);
      
      // 计算垂直于路径的方向（用于左右偏移）
      Vector3 rightDirection = Vector3.Cross(pathDirection, Vector3.up).normalized;
      
      footprintPositions.Clear();
      
      Debug.Log($"开始生成脚印路径：距离 {pathDistance:F2}m，共 {footprintCount} 个脚印");
      
      for (int i = 0; i <= footprintCount; i++)
      {
          // 计算当前脚印在路径上的位置
          float t = (float)i / footprintCount;
          Vector3 centerPosition = Vector3.Lerp(startPos, endPos, t);
          
          // 确定是左脚还是右脚
          bool isLeftFoot = (i % 2 == 0);
          
          // 计算脚印的实际位置（加上左右偏移）
          Vector3 sideOffset = rightDirection * (isLeftFoot ? -sideSpacing : sideSpacing);
          Vector3 footprintPosition = centerPosition + sideOffset + footprintOffset;
          
          // 如果启用地面贴合，进行地面检测
          if (alignToGround)
          {
              footprintPosition = AlignToGround(footprintPosition);
          }
          
          // 计算脚印朝向
          Quaternion footprintRotation = Quaternion.LookRotation(pathDirection, Vector3.up);
          
          // 生成脚印
          GameObject footprintPrefab = isLeftFoot ? leftFootprintPrefab : rightFootprintPrefab;
          GameObject footprint = Instantiate(footprintPrefab, footprintPosition, footprintRotation, transform);
          
          // 设置脚印名称
          footprint.name = $"Footprint_{(isLeftFoot ? "Left" : "Right")}_{i:D3}";
          
          // 添加到列表
          generatedFootprints.Add(footprint);
          footprintPositions.Add(footprintPosition);
          
          // 可选：添加一些延迟来观察生成过程
          yield return null; // 每帧生成一个脚印
      }
      
      Debug.Log($"脚印生成完成！共生成 {generatedFootprints.Count} 个脚印");
      OnFootprintsGenerated?.Invoke();
  }
  
  private Vector3 AlignToGround(Vector3 position)
  {
      // 从上方向下发射射线检测地面
      Vector3 rayStart = position + Vector3.up * groundCheckDistance;
      
      if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundCheckDistance * 2f, groundLayer))
      {
          // 找到地面，将脚印位置设置到地面上
          return hit.point;
      }
      
      // 如果没有找到地面，返回原始位置
      return position;
  }
  
  [ContextMenu("清除脚印")]
  public void ClearFootprints()
  {
      foreach (GameObject footprint in generatedFootprints)
      {
          if (footprint != null)
          {
              if (Application.isPlaying)
              {
                  Destroy(footprint);
              }
              else
              {
                  DestroyImmediate(footprint);
              }
          }
      }
      
      generatedFootprints.Clear();
      footprintPositions.Clear();
      
      Debug.Log("所有脚印已清除");
  }
  
  // 获取指定索引的脚印
  public GameObject GetFootprint(int index)
  {
      if (index >= 0 && index < generatedFootprints.Count)
      {
          return generatedFootprints[index];
      }
      return null;
  }
  
  // 获取所有脚印
  public List<GameObject> GetAllFootprints()
  {
      return new List<GameObject>(generatedFootprints);
  }
  
  // 获取路径总长度
  public float GetPathLength()
  {
      if (pointA != null && pointB != null)
      {
          return Vector3.Distance(pointA.position, pointB.position);
      }
      return 0f;
  }
  
  // 获取路径方向
  public Vector3 GetPathDirection()
  {
      if (pointA != null && pointB != null)
      {
          return (pointB.position - pointA.position).normalized;
      }
      return Vector3.forward;
  }
  
  private void OnDrawGizmos()
  {
      if (!showPathGizmos && !showFootprintGizmos) return;
      
      // 绘制路径线
      if (showPathGizmos && pointA != null && pointB != null)
      {
          Gizmos.color = pathColor;
          Gizmos.DrawLine(pointA.position, pointB.position);
          
          // 绘制起点和终点
          Gizmos.DrawWireSphere(pointA.position, 0.2f);
          Gizmos.DrawWireSphere(pointB.position, 0.2f);
          
          // 绘制方向箭头
          Vector3 direction = (pointB.position - pointA.position).normalized;
          Vector3 arrowPos = Vector3.Lerp(pointA.position, pointB.position, 0.8f);
          DrawArrow(arrowPos, direction, 0.5f);
      }
      
      // 绘制脚印位置预览
      if (showFootprintGizmos && footprintPositions.Count > 0)
      {
          for (int i = 0; i < footprintPositions.Count; i++)
          {
              bool isLeftFoot = (i % 2 == 0);
              Gizmos.color = isLeftFoot ? leftFootColor : rightFootColor;
              Gizmos.DrawWireSphere(footprintPositions[i], 0.1f);
              
              // 绘制脚印编号
              #if UNITY_EDITOR
              UnityEditor.Handles.Label(footprintPositions[i] + Vector3.up * 0.2f, 
                  $"{i}({(isLeftFoot ? "L" : "R")})");
              #endif
          }
      }
  }
  
  private void DrawArrow(Vector3 position, Vector3 direction, float size)
  {
      Vector3 right = Vector3.Cross(direction, Vector3.up).normalized * size * 0.3f;
      Vector3 forward = direction * size;
      
      Gizmos.DrawRay(position, forward);
      Gizmos.DrawRay(position + forward, -forward * 0.3f + right);
      Gizmos.DrawRay(position + forward, -forward * 0.3f - right);
  }
  
  private void OnValidate()
  {
      // 确保间距值合理
      forwardSpacing = Mathf.Max(0.1f, forwardSpacing);
      sideSpacing = Mathf.Max(0.05f, sideSpacing);
      groundCheckDistance = Mathf.Max(0.1f, groundCheckDistance);
  }
  
  private void OnDestroy()
  {
      // 清理事件
      OnFootprintsGenerated = null;
  }
}