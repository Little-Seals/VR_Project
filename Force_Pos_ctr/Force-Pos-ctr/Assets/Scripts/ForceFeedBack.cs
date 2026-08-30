using UnityEngine;
using System;
using System.Threading;

public class ForceFeedBack : SerialController
{
    private float _lastSentAngle = 0f;
    private const float AngleThreshold = 0f;
    private bool isSend = false;
    private float Kp = 4;
    private float _currentTorque = 0f; // 存储当前计算的力矩值
    
    
    public Rigidbody rigidBody;
    

    protected override void Start()
    {
        base.Start();
        rigidBody = this.gameObject.GetComponent<Rigidbody>();
    }

    protected override void Update()
    {
        ReadFromCache();
        
        WriteToCache(_currentTorque);
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
    
    
    private void WriteToCache(float torque)
    {
        // 将力矩值转换为字符串并发送
        SendData("T" + torque.ToString("F2")); // 格式化为两位小数发送
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