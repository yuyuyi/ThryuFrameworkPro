using System;
using UnityEngine;

namespace ThryuFrameworkPro
{
    /// <summary>
    /// 计时器状态枚举
    /// </summary>
    public enum TimerState
    {
        /// <summary>
        /// 等待中
        /// </summary>
        Waiting,
        /// <summary>
        /// 运行中
        /// </summary>
        Running,
        /// <summary>
        /// 暂停中
        /// </summary>
        Paused,
        /// <summary>
        /// 已完成
        /// </summary>
        Completed,
        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled
    }

    /// <summary>
    /// 现代化计时器类
    /// 提供高性能、易用的计时功能
    /// </summary>
    public class Timer
    {
        #region 字段
        /// <summary>
        /// 计时器唯一ID
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// 延迟时间（秒）
        /// </summary>
        public float Delay { get; private set; }

        /// <summary>
        /// 间隔时间（秒），用于循环计时器
        /// </summary>
        public float Interval { get; private set; }

        /// <summary>
        /// 重复次数，-1表示无限循环
        /// </summary>
        public int RepeatCount { get; private set; }

        /// <summary>
        /// 当前重复次数
        /// </summary>
        public int CurrentRepeat { get; private set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public float StartTime { get; private set; }

        /// <summary>
        /// 上次执行时间
        /// </summary>
        public float LastExecuteTime { get; private set; }

        /// <summary>
        /// 下次执行时间
        /// </summary>
        public float NextExecuteTime { get; private set; }

        /// <summary>
        /// 计时器状态
        /// </summary>
        public TimerState State { get; private set; }

        /// <summary>
        /// 是否使用UnscaledTime
        /// </summary>
        public bool UseUnscaledTime { get; private set; }

        /// <summary>
        /// 回调函数,在计时器每次触发时调用
        /// </summary>
        private Action _callback;

        /// <summary>
        /// 完成回调函数，在计时器完成时调用
        /// </summary>
        private Action _onComplete;

        /// <summary>
        /// 取消回调函数
        /// </summary>
        private Action _onCancel;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        private bool _isInitialized;
        #endregion

        #region 构造函数
        /// <summary>
        /// 创建计时器实例
        /// </summary>
        /// <param name="id">计时器ID</param>
        /// <param name="delay">延迟时间</param>
        /// <param name="callback">回调函数</param>
        public Timer(int id, float delay, Action callback)
        {
            Id = id;
            Delay = delay;
            _callback = callback;
            State = TimerState.Waiting;
            _isInitialized = false;
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 初始化计时器
        /// </summary>
        /// <param name="useUnscaledTime">是否使用UnscaledTime</param>
        public void Initialize(bool useUnscaledTime = false)
        {
            if (_isInitialized) return;

            UseUnscaledTime = useUnscaledTime;
            StartTime = GetCurrentTime();
            LastExecuteTime = StartTime;
            NextExecuteTime = StartTime + Delay;
            State = TimerState.Running;
            _isInitialized = true;
        }

        /// <summary>
        /// 更新计时器
        /// </summary>
        /// <returns>true表示需要执行回调，false表示继续等待</returns>
        public bool Update()
        {
            if (State != TimerState.Running) return false;

            float currentTime = GetCurrentTime();
            
            if (currentTime >= NextExecuteTime)
            {
                Execute();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 执行回调
        /// </summary>
        private void Execute()
        {
            if (State != TimerState.Running) return;

            // 执行回调
            _callback?.Invoke();

            CurrentRepeat++;

            // 检查是否需要重复
            if (RepeatCount == -1 || CurrentRepeat < RepeatCount)
            {
                // 继续循环
                LastExecuteTime = GetCurrentTime();
                NextExecuteTime = LastExecuteTime + Interval;
            }
            else
            {
                // 完成
                Complete();
            }
        }

        /// <summary>
        /// 暂停计时器
        /// </summary>
        public void Pause()
        {
            if (State == TimerState.Running)
            {
                State = TimerState.Paused;
            }
        }

        /// <summary>
        /// 恢复计时器
        /// </summary>
        public void Resume()
        {
            if (State == TimerState.Paused)
            {
                State = TimerState.Running;
                // 调整下次执行时间，保持相对时间不变
                float pausedDuration = GetCurrentTime() - LastExecuteTime;
                NextExecuteTime += pausedDuration;
            }
        }

        /// <summary>
        /// 取消计时器
        /// </summary>
        public void Cancel()
        {
            if (State == TimerState.Completed) return;

            State = TimerState.Cancelled;
            _onCancel?.Invoke();
        }

        /// <summary>
        /// 重置计时器
        /// </summary>
        public void Reset()
        {
            CurrentRepeat = 0;
            StartTime = GetCurrentTime();
            LastExecuteTime = StartTime;
            NextExecuteTime = StartTime + Delay;
            State = TimerState.Running;
        }

        /// <summary>
        /// 设置循环参数
        /// </summary>
        /// <param name="interval">间隔时间</param>
        /// <param name="repeatCount">重复次数，-1表示无限循环</param>
        public void SetLoop(float interval, int repeatCount = -1)
        {
            Interval = interval;
            RepeatCount = repeatCount;
        }

        /// <summary>
        /// 设置完成回调
        /// </summary>
        /// <param name="onComplete">完成回调</param>
        public void SetOnComplete(Action onComplete)
        {
            _onComplete = onComplete;
        }

        /// <summary>
        /// 设置取消回调
        /// </summary>
        /// <param name="onCancel">取消回调</param>
        public void SetOnCancel(Action onCancel)
        {
            _onCancel = onCancel;
        }

        /// <summary>
        /// 获取剩余时间
        /// </summary>
        /// <returns>剩余时间（秒）</returns>
        public float GetRemainingTime()
        {
            if (State != TimerState.Running) return 0f;
            
            float remaining = NextExecuteTime - GetCurrentTime();
            return Mathf.Max(0f, remaining);
        }

        /// <summary>
        /// 获取已过时间
        /// </summary>
        /// <returns>已过时间（秒）</returns>
        public float GetElapsedTime()
        {
            return GetCurrentTime() - StartTime;
        }

        /// <summary>
        /// 获取进度（0-1）
        /// </summary>
        /// <returns>进度值</returns>
        public float GetProgress()
        {
            if (Delay <= 0) return 1f;
            
            float elapsed = GetElapsedTime();
            return Mathf.Clamp01(elapsed / Delay);
        }

        /// <summary>
        /// 重置计时器以便对象池重用
        /// </summary>
        /// <param name="newId">新的ID</param>
        /// <param name="newDelay">新的延迟时间</param>
        /// <param name="newCallback">新的回调函数</param>
        public void ResetForReuse(int newId, float newDelay, Action newCallback)
        {
            Id = newId;
            Delay = newDelay;
            _callback = newCallback;
            Interval = 0f;
            RepeatCount = 0;
            CurrentRepeat = 0;
            StartTime = 0f;
            LastExecuteTime = 0f;
            NextExecuteTime = 0f;
            State = TimerState.Waiting;
            UseUnscaledTime = false;
            _onComplete = null;
            _onCancel = null;
            _isInitialized = false;
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 完成计时器
        /// </summary>
        private void Complete()
        {
            State = TimerState.Completed;
            _onComplete?.Invoke();
        }

        /// <summary>
        /// 获取当前时间
        /// </summary>
        /// <returns>当前时间</returns>
        private float GetCurrentTime()
        {
            return UseUnscaledTime ? Time.unscaledTime : Time.time;
        }
        #endregion

        #region 链式调用方法
        /// <summary>
        /// 设置循环
        /// </summary>
        /// <param name="interval">间隔时间</param>
        /// <param name="repeatCount">重复次数</param>
        /// <returns>计时器实例</returns>
        public Timer Loop(float interval, int repeatCount = -1)
        {
            SetLoop(interval, repeatCount);
            return this;
        }

        /// <summary>
        /// 设置完成回调
        /// </summary>
        /// <param name="onComplete">完成回调</param>
        /// <returns>计时器实例</returns>
        public Timer OnComplete(Action onComplete)
        {
            SetOnComplete(onComplete);
            return this;
        }

        /// <summary>
        /// 设置取消回调
        /// </summary>
        /// <param name="onCancel">取消回调</param>
        /// <returns>计时器实例</returns>
        public Timer OnCancel(Action onCancel)
        {
            SetOnCancel(onCancel);
            return this;
        }

        /// <summary>
        /// 设置使用UnscaledTime
        /// </summary>
        /// <param name="useUnscaledTime">是否使用UnscaledTime</param>
        /// <returns>计时器实例</returns>
        public Timer UseUnscaled(bool useUnscaledTime = true)
        {
            UseUnscaledTime = useUnscaledTime;
            return this;
        }
        #endregion
    }
}