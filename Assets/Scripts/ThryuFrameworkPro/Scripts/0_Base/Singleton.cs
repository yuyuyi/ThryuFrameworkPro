using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 基础框架版本更新说明：
 * 2025/6/27 羽于翼：没啥可说的，用吧
 *
 */

namespace ThryuFrameworkPro
{
    /// <summary>
    /// 改进的单例模式基类
    /// 继承格式：Singleton<类名>
    /// 
    /// 特性：
    /// 1. 自动创建实例（如果场景中不存在）
    /// 2. 防止重复创建
    /// 3. 销毁保护
    /// 4. 线程安全
    /// 5. 支持DontDestroyOnLoad
    /// </summary>
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        #region 单例实现
        /// <summary>
        /// 单例实例
        /// </summary>
        private static T _Instance;
        
        /// <summary>
        /// 线程锁，确保线程安全
        /// </summary>
        private static readonly object _lock = new object();
        
        /// <summary>
        /// 应用是否正在退出标志
        /// 防止在应用退出时创建新实例
        /// </summary>
        private static bool _applicationIsQuitting = false;

        /// <summary>
        /// 单例实例访问器
        /// 如果实例不存在会自动创建
        /// </summary>
        public static T Instance
        {
            get
            {
                // 如果应用正在退出，返回null避免创建新实例
                if (_applicationIsQuitting)
                {
                    Log.Warning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                    return null;
                }

                // 使用锁确保线程安全
                lock (_lock)
                {
                    if (_Instance == null)
                    {
                        // 查找场景中是否已存在实例
                        _Instance = FindObjectOfType<T>();

                        // 如果场景中不存在，则创建新实例
                        if (_Instance == null)
                        {
                            // 创建GameObject并添加组件
                            GameObject singletonObject = new GameObject();
                            _Instance = singletonObject.AddComponent<T>();
                            singletonObject.name = typeof(T).ToString();

                            Log.Info($"[Singleton] An instance of {typeof(T)} was created with DontDestroyOnLoad.");
                        }
                    }

                    return _Instance;
                }
            }
        }
        #endregion

        #region 生命周期
        /// <summary>
        /// Unity生命周期：Awake
        /// 检查并初始化单例实例
        /// </summary>
        protected virtual void Awake()
        {
            // 检查是否已存在实例
            if (_Instance == null)
            {
                // 设置当前对象为单例实例
                _Instance = this as T;
                
                // 如果需要在场景切换时保持，则调用DontDestroyOnLoad
                if (ShouldDontDestroyOnLoad())
                {
                    DontDestroyOnLoad(gameObject);
                }
                
                // 调用子类的初始化方法
                OnSingletonAwake();
            }
            else if (_Instance != this)
            {
                // 如果已存在实例且不是当前对象，则销毁当前对象
                Log.Warning($"[Singleton] Another instance of {typeof(T)} already exists! Destroying duplicate.");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Unity生命周期：OnDestroy
        /// 清理单例实例引用
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_Instance == this)
            {
                // 设置退出标志
                _applicationIsQuitting = true;
                // 调用子类的销毁方法
                OnSingletonDestroy();
            }
        }

        /// <summary>
        /// Unity生命周期：OnApplicationQuit
        /// 应用退出时设置标志位
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }
        #endregion

        #region 虚方法（子类可重写）
        /// <summary>
        /// 单例Awake时调用，子类可重写
        /// 用于初始化单例特定的逻辑
        /// </summary>
        protected virtual void OnSingletonAwake()
        {
            // 子类可重写此方法进行初始化
        }

        /// <summary>
        /// 单例销毁时调用，子类可重写
        /// 用于清理单例特定的资源
        /// </summary>
        protected virtual void OnSingletonDestroy()
        {
            // 子类可重写此方法进行清理
        }

        /// <summary>
        /// 是否需要在场景切换时保持实例，子类可重写
        /// 返回true表示使用DontDestroyOnLoad保持实例
        /// 返回false表示场景切换时销毁实例
        /// </summary>
        /// <returns>true表示保持，false表示不保持</returns>
        protected virtual bool ShouldDontDestroyOnLoad()
        {
            return false; // 默认不保持
        }
        #endregion

        #region 工具方法
        /// <summary>
        /// 检查单例是否存在
        /// 用于在访问Instance之前检查实例是否可用
        /// </summary>
        /// <returns>true表示存在，false表示不存在</returns>
        public static bool HasInstance()
        {
            return _Instance != null && !_applicationIsQuitting;
        }

        /// <summary>
        /// 销毁单例实例
        /// 手动销毁单例，通常在测试或重置时使用
        /// </summary>
        public static void DestroyInstance()
        {
            if (_Instance != null)
            {
                Destroy(_Instance.gameObject);
                _Instance = null;
            }
        }

        /// <summary>
        /// 重置单例状态（主要用于测试）
        /// 清除实例引用和退出标志
        /// </summary>
        public static void ResetInstance()
        {
            _Instance = null;
            _applicationIsQuitting = false;
        }
        #endregion
    }

    /// <summary>
    /// 持久化单例基类（自动DontDestroyOnLoad）
    /// 继承此类的单例会在场景切换时自动保持
    /// </summary>
    public class PersistentSingleton<T> : Singleton<T> where T : PersistentSingleton<T>
    {
        /// <summary>
        /// 重写方法，使持久化单例自动保持
        /// </summary>
        /// <returns>始终返回true，表示保持实例</returns>
        protected override bool ShouldDontDestroyOnLoad()
        {
            return true; // 持久化单例自动保持
        }
    }
}


