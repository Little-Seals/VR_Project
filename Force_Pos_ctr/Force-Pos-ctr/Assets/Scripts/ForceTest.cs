using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class ForceTest : MonoBehaviour
{
    [Header("Haptic Device Simulation (Input)")]
    [Tooltip("代表触觉设备末端执行器的虚拟物体，如VR手柄")]
    public Transform hapticTool; 
    [Tooltip("触觉工具的接触半径")]
    public float toolRadius = 0.5f;

    [Header("Force Rendering Parameters")]
    [Tooltip("虚拟环境的刚度 - 模拟CHAI3D中的m_maxForceStiffness")]
    public float stiffness = 500f;
    [Tooltip("阻尼系数，用于提高触觉稳定性，防止震荡")]
    public float damping = 10f;
    [Tooltip("工作空间比例因子，映射物理设备与虚拟空间的尺度")]
    public float workspaceScaleFactor = 1.0f;

    private Rigidbody rb;
    private Vector3 lastToolVelocity = Vector3.zero;

    // 模拟CHAI3D中存储的交互力，可用于后续驱动真实的触觉设备
    private Vector3 computedInteractionForce = Vector3.zero;

    // 打印接触力的计时器相关变量
    private float printTimer = 0f;
    private const float PRINT_INTERVAL = 0.5f; // 每0.5秒打印一次

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (hapticTool == null) return;

        // 1. 模拟CHAI3D的 updateFromDevice()：获取设备位置
        Vector3 toolPos = hapticTool.position;
        
        // 2. 模拟CHAI3D的 computeInteractionForces()：计算穿透深度与接触力
        ComputeInteractionForces(toolPos);

        // 3. 模拟CHAI3D的 applyToDevice()：将反作用力应用到虚拟工具（如果有物理属性的话）
        // 这里主要体现在视觉上阻止工具继续穿透，或者将力数据传回底层硬件

        // 4. 牛顿第三定律：将力反向施加给Cube，使其被"推"动
        ApplyForceToRigidBody();

        // 更新打印计时器并定期打印接触力
        UpdateForcePrinting();
    }

    /// <summary>
    /// 核心力计算算法：基于CHAI3D势场模型
    /// </summary>
    private void ComputeInteractionForces(Vector3 toolPos)
    {
        Vector3 closestPoint;
        Vector3 normal;
        float penetrationDepth = 0f;

        // 计算工具中心点到Cube表面的最近点
        closestPoint = ClosestPointOnSurface(toolPos);
        
        float distance = Vector3.Distance(toolPos, closestPoint);

        // 判断是否发生碰撞（考虑工具半径）
        if (distance < toolRadius)
        {
            // 发生穿透
            penetrationDepth = toolRadius - distance;
            
            // 计算碰撞法线方向（从表面指向工具中心）
            if (distance > 0.0001f)
                normal = (toolPos - closestPoint).normalized;
            else
                normal = (toolPos - transform.position).normalized; // 极端情况容错

            // 计算当前工具速度（通过插值估算）
            Vector3 toolVelocity = (toolPos - lastToolVelocity) / Time.fixedDeltaTime;
            lastToolVelocity = toolPos;

            // 势场力计算公式：F = Stiffness * Depth * Normal - Damping * Velocity
            // 对应CHAI3D中材料属性的刚度与阻尼设定[2](@ref)
            float scaledStiffness = stiffness / workspaceScaleFactor;
            
            Vector3 springForce = normal * penetrationDepth * scaledStiffness;
            Vector3 dampingForce = -toolVelocity * damping;
            
            // 合成最终交互力
            computedInteractionForce = springForce + dampingForce;
        }
        else
        {
            // 未接触，力为零
            computedInteractionForce = Vector3.zero;
        }
    }

    /// <summary>
    /// 将计算出的交互力作用于Cube刚体
    /// </summary>
    private void ApplyForceToRigidBody()
    {
        if (computedInteractionForce.magnitude > 0.001f)
        {
            // 根据牛顿第三定律，工具推Cube的力等于计算出的交互力的反作用力
            // 对应Unity物理系统施加力的方式[4](@ref)[5](@ref)
            rb.AddForce(-computedInteractionForce * Time.fixedDeltaTime, ForceMode.Impulse);
            
            // 可选：如果在接触点施加力矩，可以模拟摩擦力或翻转效果
            // rb.AddForceAtPosition(-computedInteractionForce, contactPoint);
        }
    }

    /// <summary>
    /// 更新打印计时器并在达到间隔时打印接触力信息以及Y轴力矩
    /// </summary>
    private void UpdateForcePrinting()
    {
        printTimer += Time.fixedDeltaTime;
        
        if (printTimer >= PRINT_INTERVAL)
        {
            printTimer = 0f; // 重置计时器
            
            // 计算接触点（使用之前计算的最近点）
            Vector3 contactPoint = ClosestPointOnSurface(hapticTool.position);
            
            // 计算从Cube中心到接触点的向量（力臂）
            Vector3 leverArm = contactPoint - transform.position;
            
            // 计算力矩：τ = r × F（叉乘）
            Vector3 torque = Vector3.Cross(leverArm, -computedInteractionForce);
            
            // 提取Y轴方向的力矩分量
            float torqueY = torque.y;
            
            // 打印接触力信息和Y轴力矩到控制台
            Debug.Log($"Contact Force: X={computedInteractionForce.x:F2}, Y={computedInteractionForce.y:F2}, Z={computedInteractionForce.z:F2}, Torque around Y-axis: {torqueY:F2}");
        }
    }

    /// <summary>
    /// 辅助方法：获取点到BoxCollider表面的最近点
    /// Unity的ClosestPoint返回的是内部点，需要额外处理来获取真正的表面点与法线
    /// </summary>
    private Vector3 ClosestPointOnSurface(Vector3 point)
    {
        BoxCollider box = GetComponent<BoxCollider>();
        // 将点转换到Box的局部坐标系
        Vector3 localPoint = transform.InverseTransformPoint(point);
        Vector3 halfSize = box.size / 2f;

        // 计算在局部空间中，点到边界的最短距离方向（近似求法线与接触点）
        Vector3 delta = localPoint - Vector3.zero;
        Vector3 closestLocal = localPoint;

        // 夹紧到盒子边界
        closestLocal.x = Mathf.Clamp(closestLocal.x, -halfSize.x, halfSize.x);
        closestLocal.y = Mathf.Clamp(closestLocal.y, -halfSize.y, halfSize.y);
        closestLocal.z = Mathf.Clamp(closestLocal.z, -halfSize.z, halfSize.z);

        // 如果点在盒子内部，需要推到最近的面上
        if (Mathf.Abs(localPoint.x) < halfSize.x && 
            Mathf.Abs(localPoint.y) < halfSize.y && 
            Mathf.Abs(localPoint.z) < halfSize.z)
        {
            float dx = halfSize.x - Mathf.Abs(localPoint.x);
            float dy = halfSize.y - Mathf.Abs(localPoint.y);
            float dz = halfSize.z - Mathf.Abs(localPoint.z);
            
            if (dx < dy && dx < dz) closestLocal.x = Mathf.Sign(localPoint.x) * halfSize.x;
            else if (dy < dz) closestLocal.y = Mathf.Sign(localPoint.y) * halfSize.y;
            else closestLocal.z = Mathf.Sign(localPoint.z) * halfSize.z;
        }

        // 转回世界坐标
        return transform.TransformPoint(closestLocal);
    }

    // 用于在Scene视图中可视化调试力反馈
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && computedInteractionForce.magnitude > 0.01f)
        {
            Gizmos.color = Color.red;
            // 画出交互力方向
            Gizmos.DrawRay(hapticTool.position, computedInteractionForce.normalized * 0.5f);
            Gizmos.color = Color.blue;
            // 画出Cube受到的反作用力
            Gizmos.DrawRay(transform.position, -computedInteractionForce.normalized * 0.5f);
        }
    }
}
