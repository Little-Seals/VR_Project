using UnityEngine;
using System;
using System.Threading;

public class MultiDof : SerialController
{
    private float _lastSentAngle = 0f;
    private const float AngleThreshold = 0f;
    private bool isSend = false;
    private float Kp = 20f;
    
    public Rigidbody rigidBody;
    

    protected override void Start()
    {
        base.Start();
        rigidBody = this.gameObject.GetComponent<Rigidbody>();
    }

    protected override void Update()
    {
        ReadFromCache();
        // 获取当前绕Y轴旋转的角度（假设旋转仅绕Y轴进行）
        float angle = targetCube.rotation.eulerAngles.y;
        WriteToCache(angle);
    }

    private void ReadFromCache()
    {
        if (this.NewDataReceived)
        {
            lock (DataLock)
            {
                this.CurrentAngle = this.ThreadSafeAngle;
                this.ThreadSafeDataFlag = false;
            }
            this.NewDataReceived = false;
            
            float theta1 = WrapAngle180(this.CurrentAngle);
            
            // 获取 Cube 绕 Y 轴的角度（转换为 -180 到 180 范围）
            float theta2 = WrapAngle180(targetCube.rotation.eulerAngles.y);

            // 计算角度差（最短路径）
            float angleDiff = WrapAngle180(theta1 - theta2);

            // 计算力矩
            float torque2 = Kp * angleDiff;

            // 应用力矩（绕 Y 轴）
            rigidBody.AddTorque(Vector3.up * torque2, ForceMode.Acceleration);
        }
    }
    
    
    private void WriteToCache(float AngleValue)
    {
        // 使用最短路径算法计算目标角度
        float targetAngleDeg = CalculateShortestPathAngle(_lastSentAngle, AngleValue);
        
        // 如果角度变化超过阈值，发送新角度
        if (Mathf.Abs(targetAngleDeg - _lastSentAngle) > AngleThreshold)
        {
            // 将角度从度转换为弧度，然后发送命令
            float targetAngleRad = targetAngleDeg * Mathf.Deg2Rad;
            
            // 根据电机控制程序的要求，格式为 "T" + 弧度值，例如 "T1.57"（90度）
            SendData("T" + targetAngleRad.ToString("F2")); // 格式化为两位小数发送
            _lastSentAngle = targetAngleDeg;
        }
    }
    
    
    /// <summary>
    /// 计算从起点到终点的最短路径角度
    /// </summary>
    private float CalculateShortestPathAngle(float startAngle, float endAngle)
    {
        float rawDiff = endAngle - startAngle;
        float wrappedDiff = WrapAngle180(rawDiff);
        return startAngle + wrappedDiff;
    }
    

    /// <summary>
    /// 将角度差值包装到[-180, 180)范围
    /// </summary>
    private float WrapAngle180(float angle)
    {
        angle = angle % 360f;
        if (angle > 180f)
            angle -= 360f;
        else if (angle <= -180f)
            angle += 360f;
        return angle;
    }

    protected override void ProcessReceivedData(string data)
    {
        try
        {
            if (float.TryParse(data, out float angle))
            {
                lock (DataLock)
                {
                    ThreadSafeAngle = angle;
                    ThreadSafeDataFlag = true;
                }
                NewDataReceived = true;
            }
        }
        catch (FormatException ex)
        {
            Debug.LogWarning($"数据格式错误: {data}, 错误: {ex.Message}");
        }
    }
}