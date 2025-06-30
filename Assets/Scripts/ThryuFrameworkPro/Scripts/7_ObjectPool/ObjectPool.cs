using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/* 基础框架版本更新说明：
 * 2025/6/30 羽于翼：初始版本
 *
 */
namespace ThryuFrameworkPro
{
    /// <summary>
    /// 对象池管理器 - 通用、灵活的对象池管理
    /// </summary>
    public class ObjectPool : Singleton<ObjectPool>
    {
        #region 父节点

        /// <summary>
        /// 用于一般对象
        /// </summary>
        public Transform poolParent;
        
        /// <summary>
        /// 用于UI对象父节点
        /// </summary>
        public RectTransform uiPoolParent;

        #endregion
        
        #region 字段
        /// <summary>
        /// 对象池字典 - 类型到对象池的映射
        /// </summary>
        private readonly Dictionary<Type, object> _pools = new Dictionary<Type, object>();
        
        /// <summary>
        /// 预制体字典 - 类型到预制体的映射
        /// </summary>
        private readonly Dictionary<Type, GameObject> _prefabs = new Dictionary<Type, GameObject>();
        
        /// <summary>
        /// 对象池配置
        /// </summary>
        private readonly Dictionary<Type, PoolConfig> _poolConfigs = new Dictionary<Type, PoolConfig>();
        
        /// <summary>
        /// 对象类型字典 - 记录对象是否为UI类型
        /// </summary>
        private readonly Dictionary<Type, bool> _isUITypes = new Dictionary<Type, bool>();
        
        /// <summary>
        /// 默认对象池配置
        /// </summary>
        private const int DEFAULT_INITIAL_SIZE = 20;
        private const int DEFAULT_MAX_SIZE = 200;
        #endregion

        #region 内部类
        /// <summary>
        /// 对象池配置
        /// </summary>
        [System.Serializable]
        public class PoolConfig
        {
            public int initialSize = DEFAULT_INITIAL_SIZE;
            public int maxSize = DEFAULT_MAX_SIZE;
            public bool collectionCheck = true;
            public bool isUI = false; // 是否为UI对象
            
            public PoolConfig(bool isUI = false)
            {
                this.isUI = isUI;
            }
        }
        #endregion

        #region 公共API - 注册和配置
        /// <summary>
        /// 注册对象池
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="prefab">预制体</param>
        /// <param name="config">池配置</param>
        public void RegisterPool<T>(GameObject prefab, PoolConfig config = null) where T : Component
        {
            var type = typeof(T);
            
            if (_pools.ContainsKey(type))
            {
                Log.Warning($"[ObjectPoolManager] 对象池 {type.Name} 已经注册");
                return;
            }

            if (prefab == null)
            {
                Log.Error($"[ObjectPoolManager] 预制体不能为空");
                return;
            }

            // 确定配置
            var finalConfig = config ?? new PoolConfig();
            
            // 自动检测是否为UI对象
            if (!finalConfig.isUI)
            {
                finalConfig.isUI = IsUIObject(prefab);
            }

            // 保存预制体、配置和UI类型
            _prefabs[type] = prefab;
            _poolConfigs[type] = finalConfig;
            _isUITypes[type] = finalConfig.isUI;

            // 创建对象池
            var pool = new ObjectPool<T>(
                createFunc: () => CreateObject<T>(prefab, finalConfig.isUI),
                actionOnGet: OnGetObject<T>,
                actionOnRelease: OnReleaseObject<T>,
                actionOnDestroy: OnDestroyObject<T>,
                collectionCheck: finalConfig.collectionCheck,
                defaultCapacity: finalConfig.initialSize,
                maxSize: finalConfig.maxSize
            );

            _pools[type] = pool;

            Log.Info($"[ObjectPoolManager] 注册对象池 {type.Name}, 类型: {(finalConfig.isUI ? "UI" : "普通")}, 初始大小: {finalConfig.initialSize}, 最大大小: {finalConfig.maxSize}");
        }

        /// <summary>
        /// 注册UI对象池（简化方法）
        /// </summary>
        /// <typeparam name="T">UI组件类型</typeparam>
        /// <param name="prefab">UI预制体</param>
        /// <param name="config">池配置</param>
        public void RegisterUIPool<T>(GameObject prefab, PoolConfig config = null) where T : Component
        {
            var finalConfig = config ?? new PoolConfig();
            finalConfig.isUI = true;
            RegisterPool<T>(prefab, finalConfig);
        }

        /// <summary>
        /// 注册普通对象池（简化方法）
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="prefab">预制体</param>
        /// <param name="config">池配置</param>
        public void RegisterGameObjectPool<T>(GameObject prefab, PoolConfig config = null) where T : Component
        {
            var finalConfig = config ?? new PoolConfig();
            finalConfig.isUI = false;
            RegisterPool<T>(prefab, finalConfig);
        }
        #endregion

        #region 公共API - 获取和释放
        /// <summary>
        /// 从对象池获取对象
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>对象实例</returns>
        public T Get<T>() where T : Component
        {
            var type = typeof(T);
            
            if (!_pools.TryGetValue(type, out var poolObj))
            {
                Log.Error($"[ObjectPoolManager] 对象池 {type.Name} 未注册");
                return null;
            }

            var pool = (ObjectPool<T>)poolObj;
            return pool.Get();
        }

        /// <summary>
        /// 将对象释放回对象池
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="obj">要释放的对象</param>
        public void Release<T>(T obj) where T : Component
        {
            if (obj == null) return;

            var type = typeof(T);
            
            if (!_pools.TryGetValue(type, out var poolObj))
            {
                Log.Warning($"[ObjectPoolManager] 对象池 {type.Name} 未注册，直接销毁对象");
                Destroy(obj.gameObject);
                return;
            }

            var pool = (ObjectPool<T>)poolObj;
            pool.Release(obj);
        }

        /// <summary>
        /// 检查对象池是否存在
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>是否存在</returns>
        public bool HasPool<T>() where T : Component
        {
            return _pools.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 检查对象是否为UI类型
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>是否为UI</returns>
        public bool IsUI<T>() where T : Component
        {
            return _isUITypes.TryGetValue(typeof(T), out var isUI) && isUI;
        }
        #endregion

        #region 公共API - 统计和监控
        /// <summary>
        /// 获取对象池统计信息
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>统计信息</returns>
        public string GetPoolStats<T>() where T : Component
        {
            var type = typeof(T);
            
            if (!_pools.TryGetValue(type, out var poolObj))
            {
                return $"对象池 {type.Name} 未注册";
            }

            var pool = (ObjectPool<T>)poolObj;
            var isUI = _isUITypes.TryGetValue(type, out var uiType) && uiType;
            return $"对象池 {type.Name} ({(isUI ? "UI" : "普通")}): 活跃对象 {pool.CountActive}, 池中对象 {pool.CountInactive}, 总对象 {pool.CountAll}";
        }

        /// <summary>
        /// 获取所有对象池统计信息
        /// </summary>
        /// <returns>统计信息</returns>
        public string GetAllPoolStats()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== 对象池统计信息 ===");
            
            if (_pools.Count == 0)
            {
                stats.AppendLine("  暂无注册的对象池");
                return stats.ToString();
            }
            
            foreach (var kvp in _pools)
            {
                var type = kvp.Key;
                var poolObj = kvp.Value;
                var isUI = _isUITypes.TryGetValue(type, out var uiType) && uiType;
                
                // 使用反射获取统计信息
                var countActive = (int)poolObj.GetType().GetProperty("CountActive").GetValue(poolObj);
                var countInactive = (int)poolObj.GetType().GetProperty("CountInactive").GetValue(poolObj);
                var countAll = (int)poolObj.GetType().GetProperty("CountAll").GetValue(poolObj);
                
                stats.AppendLine($"  {type.Name} ({(isUI ? "UI" : "普通")}): 活跃 {countActive}, 池中 {countInactive}, 总计 {countAll}");
            }
            
            return stats.ToString();
        }

        /// <summary>
        /// 打印所有对象池统计信息
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void PrintAllPoolStats()
        {
            Log.Info(GetAllPoolStats());
        }
        #endregion

        #region 公共API - 清理
        /// <summary>
        /// 清理特定对象池
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        public void ClearPool<T>() where T : Component
        {
            var type = typeof(T);
            
            if (_pools.TryGetValue(type, out var poolObj))
            {
                // 清理对象池
                var clearMethod = poolObj.GetType().GetMethod("Clear");
                clearMethod?.Invoke(poolObj, null);
                
                Log.Info($"[ObjectPoolManager] 清理对象池 {type.Name}");
            }
        }

        /// <summary>
        /// 清理所有对象池
        /// </summary>
        public void ClearAllPools()
        {
            foreach (var kvp in _pools)
            {
                var poolObj = kvp.Value;
                var clearMethod = poolObj.GetType().GetMethod("Clear");
                clearMethod?.Invoke(poolObj, null);
            }
            
            Log.Info("[ObjectPoolManager] 清理所有对象池");
        }
        #endregion

        #region 私有方法 - 对象池回调
        /// <summary>
        /// 创建对象
        /// </summary>
        private T CreateObject<T>(GameObject prefab, bool isUI) where T : Component
        {
            GameObject obj;
            
            if (isUI)
            {
                // UI对象使用uiPoolParent
                obj = Instantiate(prefab, uiPoolParent);
            }
            else
            {
                // 普通对象使用poolParent
                obj = Instantiate(prefab, poolParent);
            }
            
            var component = obj.GetComponent<T>();
            
            if (component == null)
            {
                Log.Error($"[ObjectPoolManager] 预制体 {prefab.name} 上没有找到组件 {typeof(T).Name}");
                Destroy(obj);
                return null;
            }
            
            return component;
        }

        /// <summary>
        /// 获取对象时的回调
        /// </summary>
        private void OnGetObject<T>(T obj) where T : Component
        {
            if (obj != null)
            {
                obj.gameObject.SetActive(true);
                
                // 调用对象的OnPoolGet方法（如果存在）
                var onPoolGet = obj as IPoolable;
                onPoolGet?.OnPoolGet();
            }
        }

        /// <summary>
        /// 释放对象时的回调
        /// </summary>
        private void OnReleaseObject<T>(T obj) where T : Component
        {
            if (obj != null)
            {
                // 调用对象的OnPoolRelease方法（如果存在）
                var onPoolRelease = obj as IPoolable;
                onPoolRelease?.OnPoolRelease();
                
                obj.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 销毁对象时的回调
        /// </summary>
        private void OnDestroyObject<T>(T obj) where T : Component
        {
            if (obj != null)
            {
                // 调用对象的OnPoolDestroy方法（如果存在）
                var onPoolDestroy = obj as IPoolable;
                onPoolDestroy?.OnPoolDestroy();
                
                Destroy(obj.gameObject);
            }
        }

        /// <summary>
        /// 检测对象是否为UI对象
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <returns>是否为UI</returns>
        private bool IsUIObject(GameObject prefab)
        {
            // 检查是否有Canvas组件
            if (prefab.GetComponent<Canvas>() != null)
                return true;
                
            // 检查是否有RectTransform组件
            if (prefab.GetComponent<RectTransform>() != null)
                return true;
                
            // 检查是否有UI相关组件
            if (prefab.GetComponent<UnityEngine.UI.Graphic>() != null)
                return true;
                
            if (prefab.GetComponent<UnityEngine.UI.Selectable>() != null)
                return true;
                
            return false;
        }
        #endregion

        #region 生命周期
        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            
            // 确保父节点存在
            EnsureParentNodes();
        }

        protected override void OnSingletonDestroy()
        {
            base.OnSingletonDestroy();
            ClearAllPools();
        }

        /// <summary>
        /// 确保父节点存在
        /// </summary>
        private void EnsureParentNodes()
        {
            // 确保普通对象父节点存在
            if (poolParent == null)
            {
                var poolParentGO = new GameObject("ObjectPool_Parent");
                poolParent = poolParentGO.transform;
                poolParent.SetParent(transform);
                Log.Info("[ObjectPoolManager] 自动创建普通对象父节点");
            }

            // 确保UI对象父节点存在
            if (uiPoolParent == null)
            {
                var uiParentGO = new GameObject("ObjectPool_UI_Parent");
                uiPoolParent = uiParentGO.AddComponent<RectTransform>();
                uiPoolParent.SetParent(transform);
                
                // 设置UI父节点的基本属性
                uiPoolParent.anchorMin = Vector2.zero;
                uiPoolParent.anchorMax = Vector2.one;
                uiPoolParent.offsetMin = Vector2.zero;
                uiPoolParent.offsetMax = Vector2.zero;
                
                Log.Info("[ObjectPoolManager] 自动创建UI对象父节点");
            }
        }
        #endregion
    }

    #region 接口和辅助类
    /// <summary>
    /// 可池化对象接口
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// 从对象池获取时调用
        /// </summary>
        void OnPoolGet();
        
        /// <summary>
        /// 释放回对象池时调用
        /// </summary>
        void OnPoolRelease();
        
        /// <summary>
        /// 从对象池销毁时调用
        /// </summary>
        void OnPoolDestroy();
    }

    /// <summary>
    /// 对象池条目
    /// </summary>
    [System.Serializable]
    public class PoolEntry
    {
        public GameObject prefab;
        public Type componentType;
        public ObjectPool.PoolConfig config;
    }

    /// <summary>
    /// 对象池管理器扩展方法
    /// </summary>
    public static class ObjectPoolExtensions
    {
        /// <summary>
        /// 获取对象（简化调用）
        /// </summary>
        public static T Get<T>(this ObjectPool manager) where T : Component
        {
            return manager.Get<T>();
        }

        /// <summary>
        /// 释放对象（简化调用）
        /// </summary>
        public static void Release<T>(this ObjectPool manager, T obj) where T : Component
        {
            manager.Release(obj);
        }

        /// <summary>
        /// 注册UI对象池（简化调用）
        /// </summary>
        public static void RegisterUI<T>(this ObjectPool manager, GameObject prefab, ObjectPool.PoolConfig config = null) where T : Component
        {
            manager.RegisterUIPool<T>(prefab, config);
        }

        /// <summary>
        /// 注册普通对象池（简化调用）
        /// </summary>
        public static void RegisterGameObject<T>(this ObjectPool manager, GameObject prefab, ObjectPool.PoolConfig config = null) where T : Component
        {
            manager.RegisterGameObjectPool<T>(prefab, config);
        }
    }
    #endregion
}

