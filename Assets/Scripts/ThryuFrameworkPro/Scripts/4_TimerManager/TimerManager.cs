using System;
using System.Collections.Generic;
using UnityEngine;

/* 基础框架版本更新说明：
 * 2025/6/30 羽于翼：初始版本
 *
 */
namespace ThryuFrameworkPro
{
    /// <summary>
    /// 计时器管理器
    /// 有考虑对象池，线程安全，内存管理，链式调用，直接用就行了
    /// </summary>
    public class TimerManager : PersistentSingleton<TimerManager>
    {
        #region 字段
        /// <summary>
        /// 活跃的计时器字典
        /// </summary>
        private readonly Dictionary<int, Timer> _activeTimers = new Dictionary<int, Timer>();

        /// <summary>
        /// 待添加的计时器队列
        /// </summary>
        private readonly Queue<Timer> _pendingTimers = new Queue<Timer>();

        /// <summary>
        /// 待移除的计时器ID队列
        /// </summary>
        private readonly Queue<int> _pendingRemovals = new Queue<int>();

        /// <summary>
        /// 计时器对象池
        /// </summary>
        private readonly Queue<Timer> _timerPool = new Queue<Timer>();

        /// <summary>
        /// 下一个计时器ID
        /// </summary>
        private int _nextTimerId = 1;

        /// <summary>
        /// 是否正在更新
        /// </summary>
        private bool _isUpdating = false;

        /// <summary>
        /// 最大计时器数量限制
        /// </summary>
        private const int MAX_TIMER_COUNT = 10000;

        /// <summary>
        /// 对象池初始大小
        /// </summary>
        private const int INITIAL_POOL_SIZE = 100;
        
        #endregion

        #region 属性
        /// <summary>
        /// 当前活跃计时器数量
        /// </summary>
        public int ActiveTimerCount => _activeTimers.Count;

        /// <summary>
        /// 对象池中可用计时器数量
        /// </summary>
        public int PooledTimerCount => _timerPool.Count;

        /// <summary>
        /// 是否启用调试日志
        /// </summary>
        public bool EnableDebugLogs { get; set; } = false;
        
        #endregion

        #region 生命周期
        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            InitializeTimerPool();
        }

        private void Update()
        {
            UpdateTimers();
        }

        protected override void OnSingletonDestroy()
        {
            base.OnSingletonDestroy();
            ClearAllTimers();
        }

        #endregion

        #region 初始化
        /// <summary>
        /// 初始化计时器对象池
        /// </summary>
        private void InitializeTimerPool()
        {
            for (int i = 0; i < INITIAL_POOL_SIZE; i++)
            {
                var timer = CreateNewTimer(0, null);
                _timerPool.Enqueue(timer);
            }
        }
        #endregion

        #region 核心方法
        /// <summary>
        /// 更新所有计时器
        /// </summary>
        private void UpdateTimers()
        {
            if (_isUpdating) return;

            _isUpdating = true;

            try
            {
                // 处理待添加的计时器
                ProcessPendingTimers();

                // 更新活跃计时器
                UpdateActiveTimers();

                // 处理待移除的计时器
                ProcessPendingRemovals();
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// 处理待添加的计时器
        /// </summary>
        private void ProcessPendingTimers()
        {
            while (_pendingTimers.Count > 0)
            {
                var timer = _pendingTimers.Dequeue();
                if (timer != null)
                {
                    _activeTimers[timer.Id] = timer;
                    timer.Initialize();
                }
            }
        }

        /// <summary>
        /// 更新活跃计时器
        /// </summary>
        private void UpdateActiveTimers()
        {
            var completedTimers = new List<int>();

            foreach (var kvp in _activeTimers)
            {
                var timer = kvp.Value;
                if (timer == null) continue;

                // 更新计时器
                bool shouldExecute = timer.Update();

                // 检查是否完成
                if (timer.State == TimerState.Completed || timer.State == TimerState.Cancelled)
                {
                    completedTimers.Add(timer.Id);
                }
            }

            // 移除完成的计时器
            foreach (var id in completedTimers)
            {
                RemoveTimerInternal(id);
            }
        }

        /// <summary>
        /// 处理待移除的计时器
        /// </summary>
        private void ProcessPendingRemovals()
        {
            while (_pendingRemovals.Count > 0)
            {
                var id = _pendingRemovals.Dequeue();
                RemoveTimerInternal(id);
            }
        }

        /// <summary>
        /// 内部移除计时器
        /// </summary>
        /// <param name="id">计时器ID</param>
        private void RemoveTimerInternal(int id)
        {
            if (_activeTimers.TryGetValue(id, out var timer))
            {
                _activeTimers.Remove(id);
                RecycleTimer(timer);
            }
        }
        #endregion

        #region 公共API - 创建计时器
        /// <summary>
        /// 延迟执行
        /// </summary>
        /// <param name="delay">延迟时间（秒）</param>
        /// <param name="callback">回调函数</param>
        /// <returns>计时器实例</returns>
        public Timer Delay(float delay, Action callback)
        {
            return CreateTimer(delay, callback);
        }

        /// <summary>
        /// 延迟执行（使用UnscaledTime）
        /// </summary>
        /// <param name="delay">延迟时间（秒）</param>
        /// <param name="callback">回调函数</param>
        /// <returns>计时器实例</returns>
        public Timer DelayUnscaled(float delay, Action callback)
        {
            return CreateTimer(delay, callback).UseUnscaled();
        }

        /// <summary>
        /// 循环执行
        /// </summary>
        /// <param name="interval">间隔时间（秒）</param>
        /// <param name="callback">回调函数</param>
        /// <param name="repeatCount">重复次数，-1表示无限循环</param>
        /// <returns>计时器实例</returns>
        public Timer Loop(float interval, Action callback, int repeatCount = -1)
        {
            return CreateTimer(0, callback).Loop(interval, repeatCount);
        }

        /// <summary>
        /// 循环执行（使用UnscaledTime）
        /// </summary>
        /// <param name="interval">间隔时间（秒）</param>
        /// <param name="callback">回调函数</param>
        /// <param name="repeatCount">重复次数，-1表示无限循环</param>
        /// <returns>计时器实例</returns>
        public Timer LoopUnscaled(float interval, Action callback, int repeatCount = -1)
        {
            return CreateTimer(0, callback).Loop(interval, repeatCount).UseUnscaled();
        }

        /// <summary>
        /// 延迟后循环执行
        /// </summary>
        /// <param name="delay">初始延迟时间（秒）</param>
        /// <param name="interval">后续间隔时间（秒）</param>
        /// <param name="callback">回调函数</param>
        /// <param name="repeatCount">重复次数，-1表示无限循环</param>
        /// <returns>计时器实例</returns>
        public Timer DelayThenLoop(float delay, float interval, Action callback, int repeatCount = -1)
        {
            return CreateTimer(delay, callback).Loop(interval, repeatCount);
        }

        /// <summary>
        /// 延迟后循环执行（使用UnscaledTime）
        /// </summary>
        /// <param name="delay">初始延迟时间（秒）</param>
        /// <param name="interval">后续间隔时间（秒）</param>
        /// <param name="callback">回调函数</param>
        /// <param name="repeatCount">重复次数，-1表示无限循环</param>
        /// <returns>计时器实例</returns>
        public Timer DelayThenLoopUnscaled(float delay, float interval, Action callback, int repeatCount = -1)
        {
            return CreateTimer(delay, callback).Loop(interval, repeatCount).UseUnscaled();
        }

        /// <summary>
        /// 创建计时器
        /// </summary>
        /// <param name="delay">延迟时间</param>
        /// <param name="callback">回调函数</param>
        /// <returns>计时器实例</returns>
        public Timer CreateTimer(float delay, Action callback)
        {
            if (callback == null)
            {
                Log.Error("[TimerManager] 回调函数不能为空");
                return null;
            }

            if (delay < 0)
            {
                Log.Error("[TimerManager] 延迟时间不能为负数");
                return null;
            }

            if (_activeTimers.Count >= MAX_TIMER_COUNT)
            {
                Log.Warning($"[TimerManager] 计时器数量已达到上限 {MAX_TIMER_COUNT}");
                return null;
            }

            var timer = GetTimerFromPool(delay, callback);
            _pendingTimers.Enqueue(timer);

            if (EnableDebugLogs)
            {
                Log.Info($"[TimerManager] 创建计时器 ID:{timer.Id}, 延迟:{delay}s");
            }

            return timer;
        }
        #endregion

        #region 公共API - 管理计时器
        /// <summary>
        /// 暂停计时器
        /// </summary>
        /// <param name="timer">计时器实例</param>
        public void PauseTimer(Timer timer)
        {
            if (timer != null)
            {
                timer.Pause();
            }
        }

        /// <summary>
        /// 暂停计时器
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        public void PauseTimer(int timerId)
        {
            if (_activeTimers.TryGetValue(timerId, out var timer))
            {
                timer.Pause();
            }
        }

        /// <summary>
        /// 恢复计时器
        /// </summary>
        /// <param name="timer">计时器实例</param>
        public void ResumeTimer(Timer timer)
        {
            if (timer != null)
            {
                timer.Resume();
            }
        }

        /// <summary>
        /// 恢复计时器
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        public void ResumeTimer(int timerId)
        {
            if (_activeTimers.TryGetValue(timerId, out var timer))
            {
                timer.Resume();
            }
        }

        /// <summary>
        /// 取消计时器
        /// </summary>
        /// <param name="timer">计时器实例</param>
        public void CancelTimer(Timer timer)
        {
            if (timer != null)
            {
                timer.Cancel();
                _pendingRemovals.Enqueue(timer.Id);
            }
        }

        /// <summary>
        /// 取消计时器
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        public void CancelTimer(int timerId)
        {
            if (_activeTimers.ContainsKey(timerId))
            {
                _pendingRemovals.Enqueue(timerId);
            }
        }

        /// <summary>
        /// 重置计时器
        /// </summary>
        /// <param name="timer">计时器实例</param>
        public void ResetTimer(Timer timer)
        {
            if (timer != null)
            {
                timer.Reset();
            }
        }

        /// <summary>
        /// 重置计时器
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        public void ResetTimer(int timerId)
        {
            if (_activeTimers.TryGetValue(timerId, out var timer))
            {
                timer.Reset();
            }
        }

        /// <summary>
        /// 获取计时器
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        /// <returns>计时器实例</returns>
        public Timer GetTimer(int timerId)
        {
            _activeTimers.TryGetValue(timerId, out var timer);
            return timer;
        }

        /// <summary>
        /// 检查计时器是否存在
        /// </summary>
        /// <param name="timerId">计时器ID</param>
        /// <returns>true表示存在</returns>
        public bool HasTimer(int timerId)
        {
            return _activeTimers.ContainsKey(timerId);
        }

        /// <summary>
        /// 暂停所有计时器
        /// </summary>
        public void PauseAllTimers()
        {
            foreach (var timer in _activeTimers.Values)
            {
                if (timer != null)
                {
                    timer.Pause();
                }
            }
        }

        /// <summary>
        /// 恢复所有计时器
        /// </summary>
        public void ResumeAllTimers()
        {
            foreach (var timer in _activeTimers.Values)
            {
                if (timer != null)
                {
                    timer.Resume();
                }
            }
        }

        /// <summary>
        /// 取消所有计时器
        /// </summary>
        public void CancelAllTimers()
        {
            foreach (var timer in _activeTimers.Values)
            {
                if (timer != null)
                {
                    timer.Cancel();
                }
            }
            _activeTimers.Clear();
        }

        /// <summary>
        /// 清除所有计时器
        /// </summary>
        public void ClearAllTimers()
        {
            CancelAllTimers();
            _pendingTimers.Clear();
            _pendingRemovals.Clear();
        }
        #endregion

        #region 对象池管理
        /// <summary>
        /// 从对象池获取计时器
        /// </summary>
        /// <param name="delay">延迟时间</param>
        /// <param name="callback">回调函数</param>
        /// <returns>计时器实例</returns>
        private Timer GetTimerFromPool(float delay, Action callback)
        {
            Timer timer;

            if (_timerPool.Count > 0)
            {
                timer = _timerPool.Dequeue();
                // 重置计时器状态
                ResetTimerForReuse(timer, delay, callback);
            }
            else
            {
                timer = CreateNewTimer(delay, callback);
            }

            return timer;
        }

        /// <summary>
        /// 创建新的计时器
        /// </summary>
        /// <param name="delay">延迟时间</param>
        /// <param name="callback">回调函数</param>
        /// <returns>计时器实例</returns>
        private Timer CreateNewTimer(float delay, Action callback)
        {
            var timer = new Timer(_nextTimerId++, delay, callback);
            return timer;
        }

        /// <summary>
        /// 重置计时器以便重用
        /// </summary>
        /// <param name="timer">计时器实例</param>
        /// <param name="delay">新的延迟时间</param>
        /// <param name="callback">新的回调函数</param>
        private void ResetTimerForReuse(Timer timer, float delay, Action callback)
        {
            // 使用Timer的ResetForReuse方法重置状态
            timer.ResetForReuse(_nextTimerId++, delay, callback);
        }

        /// <summary>
        /// 回收计时器到对象池
        /// </summary>
        /// <param name="timer">计时器实例</param>
        private void RecycleTimer(Timer timer)
        {
            if (timer != null && _timerPool.Count < MAX_TIMER_COUNT)
            {
                _timerPool.Enqueue(timer);
            }
        }
        #endregion

        #region 调试和统计
        /// <summary>
        /// 获取计时器统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetStatistics()
        {
            return $"活跃计时器: {ActiveTimerCount}, 对象池大小: {PooledTimerCount}";
        }

        /// <summary>
        /// 打印计时器统计信息
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void PrintStatistics()
        {
            Log.Info($"[TimerManager] {GetStatistics()}");
        }
        #endregion
    }
}

