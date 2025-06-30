using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Concurrent;

/* 基础框架版本更新说明：
 * 2025/6/30 羽于翼：初始版本
 *
 */
namespace ThryuFrameworkPro
{

    /// <summary>
    /// 事件参数基类
    /// </summary>
    public abstract class BaseEventArgs : EventArgs, IReference
    {
        /// <summary>
        /// 事件类型编号,一般来说需要在EventId类中定义
        /// </summary>
        public abstract int Id { get; }

        /// <summary>
        /// 清理事件参数
        /// </summary>
        public virtual void Clear()
        {
            
        }
    }

    /// <summary>
    /// 事件节点
    /// </summary>
    internal sealed class EventNode : IReference
    {
        public object Sender { get; set; }
        public BaseEventArgs EventArgs { get; set; }

        public static EventNode Create(object sender, BaseEventArgs eventArgs)
        {
            EventNode eventNode = ReferencePool.Acquire<EventNode>();
            eventNode.Sender = sender;
            eventNode.EventArgs = eventArgs;
            return eventNode;
        }


        public void Clear()
        {
            Sender = null;
            EventArgs = null;
        }
    }

    /// <summary>
    /// 现代化事件管理器 - 参考GameFramework设计，提供类型安全、高性能的事件系统
    /// </summary>
    public class EventManager : Singleton<EventManager>
    {
        #region 数据结构

        // 事件处理函数字典
        private readonly Dictionary<Type, List<(Action<BaseEventArgs> handler, int priority)>> _eventHandlers;
        
        // 事件队列
        private readonly ConcurrentQueue<EventNode> _eventQueue;
        
        // 事件统计
        private readonly Dictionary<Type, EventStats> _eventStats;
        
        // 默认事件处理函数
        private Action<BaseEventArgs> _defaultHandler;
        
        // 线程安全锁
        private readonly object _lock = new object();

        // 事件处理器包装器缓存 - 避免重复创建包装器
        private readonly Dictionary<object, Action<BaseEventArgs>> _handlerWrappers;
        
        #endregion

        #region 内部类定义

        /// <summary>
        /// 事件统计信息
        /// </summary>
        public class EventStats
        {
            /// <summary>
            /// 事件被触发的总次数。
            /// </summary>
            public int TotalFired { get; set; }

            /// <summary>
            /// 当前注册的监听器总数。
            /// </summary>
            public int TotalListeners { get; set; }

            /// <summary>
            /// 上一次事件被触发的时间
            /// </summary>
            public float LastFiredTime { get; set; }

            /// <summary>
            /// 事件处理的平均执行时间
            /// </summary>
            public float AverageExecutionTime { get; set; }

            /// <summary>
            /// 事件处理的累计总执行时间
            /// </summary>
            public float TotalExecutionTime { get; set; }

            /// <summary>
            /// 事件被触发时，历史上同时并发的最大监听器数量
            /// </summary>
            public int MaxConcurrentListeners { get; set; }
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化事件管理器
        /// </summary>
        /// <param name="mode">事件池模式</param>
        public EventManager()
        {
            _eventHandlers = new Dictionary<Type, List<(Action<BaseEventArgs> handler, int priority)>>();
            _eventQueue = new ConcurrentQueue<EventNode>();
            _eventStats = new Dictionary<Type, EventStats>();
            _defaultHandler = null;
            _handlerWrappers = new Dictionary<object, Action<BaseEventArgs>>();
        }

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取事件处理函数的数量
        /// </summary>
        public int EventHandlerCount
        {
            get
            {
                lock (_lock)
                {
                    return _eventHandlers.Values.Sum(list => list.Count);
                }
            }
        }

        /// <summary>
        /// 获取事件队列中的事件数量
        /// </summary>
        public int EventCount
        {
            get
            {
                return _eventQueue.Count;
            }
        }

        #endregion

        #region 处理器

        /// <summary>
        /// 获取或创建事件处理器包装器
        /// </summary>
        private Action<BaseEventArgs> GetOrCreateWrapper<T>(Action<T> handler) where T : BaseEventArgs
        {
            lock (_lock)
            {
                if (!_handlerWrappers.TryGetValue(handler, out var wrapper))
                {
                    wrapper = new Action<BaseEventArgs>(e => handler((T)e));
                    _handlerWrappers[handler] = wrapper;
                }
                return wrapper;
            }
        }

        /// <summary>
        /// 移除事件处理器包装器
        /// </summary>
        private void RemoveWrapper(object handler)
        {
            lock (_lock)
            {
                _handlerWrappers.Remove(handler);
            }
        }

        #endregion

        #region 订阅

        /// <summary>
        /// 订阅事件 - 类型安全版本
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        /// <param name="priority">优先级 (数字越大优先级越高)</param>
        public void Subscribe<T>(Action<T> handler, int priority = 0) where T : BaseEventArgs
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler), "空事件");
            }

            lock (_lock)
            {
                var eventType = typeof(T);
                if (!_eventHandlers.ContainsKey(eventType))
                {
                    _eventHandlers[eventType] = new List<(Action<BaseEventArgs> handler, int priority)>();
                }

                var handlers = _eventHandlers[eventType];
                var wrappedHandler = GetOrCreateWrapper(handler);

                handlers.Add((wrappedHandler, priority));
                handlers.Sort((a, b) => b.priority.CompareTo(a.priority)); // 按优先级排序

                UpdateEventStats(eventType, handlers.Count);
            }
        }

        

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">事件处理器</param>
        public void Unsubscribe<T>(Action<T> handler) where T : BaseEventArgs
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler), "空事件");
            }

            lock (_lock)
            {
                var eventType = typeof(T);
                var wrappedHandler = GetOrCreateWrapper(handler);

                // 从普通事件中移除
                if (_eventHandlers.ContainsKey(eventType))
                {
                    var handlers = _eventHandlers[eventType];
                    handlers.RemoveAll(x => x.handler == wrappedHandler);

                    if (handlers.Count == 0)
                    {
                        _eventHandlers.Remove(eventType);
                    }
                    else
                    {
                        UpdateEventStats(eventType, handlers.Count);
                    }
                }

                // 移除包装器缓存
                RemoveWrapper(handler);
            }
        }

        /// <summary>
        /// 设置默认事件处理函数
        /// </summary>
        /// <param name="handler">默认事件处理函数</param>
        public void SetDefaultHandler(Action<BaseEventArgs> handler)
        {
            _defaultHandler = handler;
        }

        #endregion

        #region 触发

        /// <summary>
        /// 抛出事件 - 线程安全，事件会在下一帧分发
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="sender">事件源</param>
        /// <param name="eventArgs">事件参数</param>
        public void Fire<T>(object sender, T eventArgs) where T : BaseEventArgs
        {
            if (eventArgs == null)
            {
                throw new ArgumentNullException(nameof(eventArgs), "空事件");
            }

            EventNode eventNode = EventNode.Create(sender, eventArgs);
            _eventQueue.Enqueue(eventNode);
        }

        /// <summary>
        /// 立即抛出事件 - 非线程安全，事件会立刻分发
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="sender">事件源</param>
        /// <param name="eventArgs">事件参数</param>
        public void FireNow<T>(object sender, T eventArgs) where T : BaseEventArgs
        {
            if (eventArgs == null)
            {
                throw new ArgumentNullException(nameof(eventArgs), "空事件");
            }

            HandleEvent(sender, eventArgs);
        }

        #endregion

        #region 统计

        /// <summary>
        /// 检查是否存在事件处理函数
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <param name="handler">要检查的事件处理函数</param>
        /// <returns>是否存在事件处理函数</returns>
        public bool Check<T>(Action<T> handler) where T : BaseEventArgs
        {
            if (handler == null)
            {
                return false;
            }

            lock (_lock)
            {
                var eventType = typeof(T);
                var wrappedHandler = GetOrCreateWrapper(handler);

                if (_eventHandlers.ContainsKey(eventType))
                {
                    return _eventHandlers[eventType].Any(h => h.handler == wrappedHandler);
                }

                return false;
            }
        }

        /// <summary>
        /// 获取事件处理函数的数量
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>事件处理函数的数量</returns>
        public int Count<T>() where T : BaseEventArgs
        {
            lock (_lock)
            {
                var eventType = typeof(T);
                int count = 0;

                if (_eventHandlers.ContainsKey(eventType))
                {
                    count += _eventHandlers[eventType].Count;
                }

                return count;
            }
        }

        /// <summary>
        /// 获取事件统计信息
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        /// <returns>统计信息</returns>
        public EventStats GetEventStats<T>() where T : BaseEventArgs
        {
            var eventType = typeof(T);
            lock (_lock)
            {
                return _eventStats.ContainsKey(eventType) ? _eventStats[eventType] : null;
            }
        }

        #endregion

        #region 清理

        /// <summary>
        /// 关闭并清理事件管理器
        /// </summary>
        public void Shutdown()
        {
            lock (_lock)
            {
                Clear();
                _eventHandlers.Clear();
                _eventStats.Clear();
                _handlerWrappers.Clear();
                _defaultHandler = null;
            }
        }

        /// <summary>
        /// 清理事件队列
        /// </summary>
        public void Clear()
        {
            while (_eventQueue.TryDequeue(out _))
            {
                // 清空队列
            }
        }

        /// <summary>
        /// 清除特定类型的所有事件监听器
        /// </summary>
        /// <typeparam name="T">事件类型</typeparam>
        public void ClearEvents<T>() where T : BaseEventArgs
        {
            var eventType = typeof(T);
            lock (_lock)
            {
                _eventHandlers.Remove(eventType);
                _eventStats.Remove(eventType);
            }
        }

        #endregion

        #region 内部方法

        /// <summary>
        /// 更新事件统计信息
        /// </summary>
        private void UpdateEventStats(Type eventType, int listenerCount)
        {
            if (!_eventStats.ContainsKey(eventType))
            {
                _eventStats[eventType] = new EventStats();
            }

            var stats = _eventStats[eventType];
            stats.TotalListeners = listenerCount;
            stats.MaxConcurrentListeners = Math.Max(stats.MaxConcurrentListeners, listenerCount);
        }

        /// <summary>
        /// 更新事件统计信息
        /// </summary>
        private void UpdateEventStats(Type eventType, float startTime)
        {
            if (!_eventStats.ContainsKey(eventType))
            {
                _eventStats[eventType] = new EventStats();
            }

            var stats = _eventStats[eventType];
            stats.TotalFired++;
            stats.LastFiredTime = Time.time;

            var executionTime = Time.realtimeSinceStartup - startTime;
            stats.TotalExecutionTime += executionTime;
            stats.AverageExecutionTime = stats.TotalExecutionTime / stats.TotalFired;
        }

        /// <summary>
        /// 处理事件
        /// </summary>
        private void HandleEvent(object sender, BaseEventArgs eventArgs)
        {
            var startTime = Time.realtimeSinceStartup;
            var eventType = eventArgs.GetType();
            bool noHandlerException = false;

            try
            {
                // 处理普通事件
                if (_eventHandlers.ContainsKey(eventType))
                {
                    var handlers = _eventHandlers[eventType];
                    foreach (var (handler, _) in handlers)
                    {
                        try
                        {
                            handler?.Invoke(eventArgs);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"[EventManager] 事件处理器执行错误: {ex.Message}");
                        }
                    }
                }

                // 处理默认事件处理函数
                if (_defaultHandler != null)
                {
                    try
                    {
                        _defaultHandler(eventArgs);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[EventManager] 默认事件处理器执行错误: {ex.Message}");
                    }
                }

                // 更新统计信息
                UpdateEventStats(eventType, startTime);
            }
            catch (Exception ex)
            {
                Log.Error($"[EventManager] 事件处理错误: {ex.Message}");
            }
            finally
            {
                ReferencePool.Release(eventArgs);
            }

            if (noHandlerException)
            {
                throw new InvalidOperationException($" '{eventType.Name}' 不许空handler.");
            }
        }

        #endregion

        #region Unity

        /// <summary>
        /// 事件管理器轮询
        /// </summary>
        public void Update()
        {
            // 处理事件队列
            while (_eventQueue.TryDequeue(out EventNode eventNode))
            {
                HandleEvent(eventNode.Sender, eventNode.EventArgs);
                ReferencePool.Release(eventNode);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            Shutdown();
        }

        #endregion
    }
}
