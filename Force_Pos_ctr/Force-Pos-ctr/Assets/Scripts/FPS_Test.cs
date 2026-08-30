using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPS_Test : MonoBehaviour
{
    // 用于计算帧率的时间间隔（秒）
    public float updateInterval = 1.0f;
    
    // 目标帧率，设置为0表示不限制帧率（使用默认值）
    public int targetFPS = 60;
    
    // 累计的帧数和时间（渲染帧）
    private int frameCount = 0;
    private float elapsedTime = 0f;
    
    // 累计的物理帧数和时间
    private int physicsFrameCount = 0;
    private float physicsElapsedTime = 0f;
    
    // 当前计算的帧率和周期（渲染帧）
    private float currentFPS = 0f;
    private float frameTime = 0f;
    
    // 当前计算的物理帧率和周期
    private float currentPhysicsFPS = 0f;
    private float physicsFrameTime = 0f;

    // Start is called before the first frame update
    void Start()
    {
        // 初始化累计变量（渲染帧）
        frameCount = 0;
        elapsedTime = 0f;
        
        // 初始化累计变量（物理帧）
        physicsFrameCount = 0;
        physicsElapsedTime = 0f;
        
        // 设置目标帧率，如果targetFPS大于0则应用设置
        if (targetFPS > 0)
        {
            // Application.targetFrameRate用于设置游戏的目标帧率（渲染帧率）
            Application.targetFrameRate = targetFPS;
            Debug.Log(string.Format("目标渲染帧率已设置为: {0} FPS", targetFPS));
        }
        else
        {
            // 如果设置为0或负数，则不限制帧率，使用平台默认值
            Application.targetFrameRate = -1;
            Debug.Log("渲染帧率限制已解除，使用默认值");
        }
        
        // 打印当前的物理帧率设置（固定时间步长）
        Debug.Log(string.Format("当前物理帧率设置为: {0:F2} FPS (FixedDeltaTime: {1:F4}s)", 
            1.0f / Time.fixedDeltaTime, Time.fixedDeltaTime));
    }

    // Update is called once per frame（渲染帧，每帧调用）
    void Update()
    {
        // 累计渲染帧数
        frameCount++;
        
        // 累计经过的时间（Time.deltaTime是上一帧到当前帧的时间间隔）
        elapsedTime += Time.deltaTime;
        
        // 当累计时间达到设定的更新间隔时，计算并打印渲染帧率
        if (elapsedTime >= updateInterval)
        {
            // 计算渲染帧率：帧数 / 时间 = FPS
            currentFPS = frameCount / elapsedTime;
            
            // 计算每帧的平均时间（毫秒）
            frameTime = (elapsedTime / frameCount) * 1000f;
            
            // 重置计数器，为下一个统计周期做准备（渲染帧）
            frameCount = 0;
            elapsedTime = 0f;
        }
    }
    
    // FixedUpdate is called at a fixed time interval（物理帧，固定时间步长调用）
    void FixedUpdate()
    {
        // 累计物理帧数
        physicsFrameCount++;
        
        // 累计经过的时间（Time.fixedDeltaTime是固定的物理时间步长）
        physicsElapsedTime += Time.fixedDeltaTime;
        
        // 当累计时间达到设定的更新间隔时，计算物理帧率
        if (physicsElapsedTime >= updateInterval)
        {
            // 计算物理帧率：帧数 / 时间 = FPS
            currentPhysicsFPS = physicsFrameCount / physicsElapsedTime;
            
            // 计算每帧的平均时间（毫秒）
            physicsFrameTime = (physicsElapsedTime / physicsFrameCount) * 1000f;
            
            // 在控制台同时打印渲染帧率和物理帧率信息
            Debug.Log(string.Format("渲染FPS: {0:F2} | 渲染周期: {1:F2} ms | 物理FPS: {2:F2} | 物理周期: {3:F2} ms", 
                currentFPS, frameTime, currentPhysicsFPS, physicsFrameTime));
            
            // 重置计数器，为下一个统计周期做准备（物理帧）
            physicsFrameCount = 0;
            physicsElapsedTime = 0f;
        }
    }
}
