using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ThryuFrameworkPro
{
    public class FrameworkManager : Singleton<FrameworkManager>
    {
        [LabelText("是否允许日志输出")]
        public bool allowLog = true;
        
        [LabelText("日志过滤级别")]
        public LogLevel LogLevelFilter = LogLevel.Info;

        public void Start()
        {
            Log.Info("测试");
        }
    }
}


