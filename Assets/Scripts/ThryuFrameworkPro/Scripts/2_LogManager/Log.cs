using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThryuFrameworkPro
{
    public enum LogLevel
    {
        Info,
        Warning,
        Error
    }

    public static class Log
    {
        #region 常规
        
        /// <summary>
        /// 日志回调（可用于写文件、网络上传等）
        /// </summary>
        public static Action<LogLevel, string> OnLog;

        public static void Info(object msg, bool tag = true, Color color = default)
        {
            WriteLog(LogLevel.Info, msg, tag, color);
        }

        public static void InfoFormat(string format, params object[] args)
        {
            WriteLog(LogLevel.Info, string.Format(format, args));
        }

        public static void Warning(object msg, bool tag = true, Color color = default)
        {
            WriteLog(LogLevel.Warning, msg, tag, color);
        }

        public static void WarningFormat(string format, params object[] args)
        {
            WriteLog(LogLevel.Warning, string.Format(format, args));
        }

        public static void Error(object msg, bool tag = true, Color color = default)
        {
            WriteLog(LogLevel.Error, msg, tag, color);
        }

        public static void ErrorFormat(string format, params object[] args)
        {
            WriteLog(LogLevel.Error, string.Format(format, args));
        }

        private static void WriteLog(LogLevel level, object msg, bool tag = true, Color color = default)
        {
            if (!FrameworkManager.Instance.allowLog) return;
            if (level < FrameworkManager.Instance.LogLevelFilter) return;

            string colorstr = "98F5F9";
            
            if (color != default)
            {
                colorstr = ColorUtility.ToHtmlStringRGB(color);
            }
            string output = tag ? msg.ToString() : $"<color=#{colorstr}>[thr] {msg}</color>";

            switch (level)
            {
                case LogLevel.Info:
                    Debug.Log(output);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(output);
                    break;
                case LogLevel.Error:
                    Debug.LogError(output);
                    break;
            }

            OnLog?.Invoke(level, output);
        }
        
        #endregion
    }
}

