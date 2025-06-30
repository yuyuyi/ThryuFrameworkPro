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
    /// 引用池
    /// 反正能用，但别滥用
    /// </summary>
    public static class ReferencePool
    {
        private static readonly Dictionary<Type, ReferenceCollection> s_ReferenceCollections = new Dictionary<Type, ReferenceCollection>();
        private static bool s_EnableStrictCheck = false;

        /// <summary>
        /// 获取或设置是否开启强制检查
        /// 开发的时候开，查重复，上线的时候可以关
        /// </summary>
        public static bool EnableStrictCheck
        {
            get
            {
                return s_EnableStrictCheck;
            }
            set
            {
                s_EnableStrictCheck = value;
            }
        }

        /// <summary>
        /// 获取引用池的数量
        /// </summary>
        public static int Count
        {
            get
            {
                return s_ReferenceCollections.Count;
            }
        }

        /// <summary>
        /// 清除所有引用池
        /// </summary>
        public static void ClearAll()
        {
            lock (s_ReferenceCollections)
            {
                foreach (KeyValuePair<Type, ReferenceCollection> referenceCollection in s_ReferenceCollections)
                {
                    referenceCollection.Value.RemoveAll();
                }

                s_ReferenceCollections.Clear();
            }
        }

        /// <summary>
        /// 清除指定类型的引用池
        /// </summary>
        /// <typeparam name="T">引用类型</typeparam>
        public static void Clear<T>() where T : class, IReference
        {
            Clear(typeof(T));
        }

        /// <summary>
        /// 清除指定类型的引用池
        /// </summary>
        /// <param name="referenceType">引用类型</param>
        public static void Clear(Type referenceType)
        {
            if (referenceType == null)
            {
                throw new ArgumentNullException("空引用");
            }

            if (!typeof(IReference).IsAssignableFrom(referenceType))
            {
                throw new ArgumentException(ThrTools.Text.Format("'{0}'空.", referenceType.FullName));
            }

            lock (s_ReferenceCollections)
            {
                ReferenceCollection referenceCollection = null;
                if (s_ReferenceCollections.TryGetValue(referenceType, out referenceCollection))
                {
                    referenceCollection.RemoveAll();
                }
            }
        }

        /// <summary>
        /// 从引用池获取引用
        /// </summary>
        /// <typeparam name="T">引用类型</typeparam>
        /// <returns>引用</returns>
        public static T Acquire<T>() where T : class, IReference, new()
        {
            return GetReferenceCollection(typeof(T)).Acquire<T>();
        }

        /// <summary>
        /// 从引用池获取引用
        /// </summary>
        /// <param name="referenceType">引用类型</param>
        /// <returns>引用</returns>
        public static IReference Acquire(Type referenceType)
        {
            if (referenceType == null)
            {
                throw new ArgumentNullException("空引用");
            }

            if (!typeof(IReference).IsAssignableFrom(referenceType))
            {
                throw new ArgumentException(ThrTools.Text.Format("'{0}'空", referenceType.FullName));
            }

            return GetReferenceCollection(referenceType).Acquire(referenceType);
        }

        /// <summary>
        /// 将引用归还引用池
        /// </summary>
        /// <param name="reference">引用</param>
        public static void Release(IReference reference)
        {
            if (reference == null)
            {
                throw new ArgumentNullException("空引用");
            }

            Type referenceType = reference.GetType();
            GetReferenceCollection(referenceType).Release(reference);
        }

        /// <summary>
        /// 向引用池中追加指定数量的引用
        /// </summary>
        /// <typeparam name="T">引用类型</typeparam>
        /// <param name="count">追加数量</param>
        public static void Add<T>(int count) where T : class, IReference, new()
        {
            GetReferenceCollection(typeof(T)).Add<T>(count);
        }

        /// <summary>
        /// 向引用池中追加指定数量的引用
        /// </summary>
        /// <param name="referenceType">引用类型</param>
        /// <param name="count">追加数量</param>
        public static void Add(Type referenceType, int count)
        {
            if (referenceType == null)
            {
                throw new ArgumentNullException("空引用");
            }

            if (!typeof(IReference).IsAssignableFrom(referenceType))
            {
                throw new ArgumentException(ThrTools.Text.Format("'{0}'空", referenceType.FullName));
            }

            GetReferenceCollection(referenceType).Add(referenceType, count);
        }

        /// <summary>
        /// 从引用池中移除指定数量的引用
        /// </summary>
        /// <typeparam name="T">引用类型</typeparam>
        /// <param name="count">移除数量</param>
        public static void Remove<T>(int count) where T : class, IReference
        {
            GetReferenceCollection(typeof(T)).Remove(count);
        }

        /// <summary>
        /// 从引用池中移除指定数量的引用
        /// </summary>
        /// <param name="referenceType">引用类型</param>
        /// <param name="count">移除数量</param>
        public static void Remove(Type referenceType, int count)
        {
            if (referenceType == null)
            {
                throw new ArgumentNullException("空引用");
            }

            if (!typeof(IReference).IsAssignableFrom(referenceType))
            {
                throw new ArgumentException(ThrTools.Text.Format("'{0}'空", referenceType.FullName));
            }

            GetReferenceCollection(referenceType).Remove(count);
        }

        /// <summary>
        /// 从引用池中移除所有的引用
        /// </summary>
        /// <typeparam name="T">引用类型</typeparam>
        public static void RemoveAll<T>() where T : class, IReference
        {
            GetReferenceCollection(typeof(T)).RemoveAll();
        }

        /// <summary>
        /// 从引用池中移除所有的引用
        /// </summary>
        /// <param name="referenceType">引用类型</param>
        public static void RemoveAll(Type referenceType)
        {
            if (referenceType == null)
            {
                throw new ArgumentNullException("空引用");
            }

            if (!typeof(IReference).IsAssignableFrom(referenceType))
            {
                throw new ArgumentException(ThrTools.Text.Format("'{0}'空", referenceType.FullName));
            }

            GetReferenceCollection(referenceType).RemoveAll();
        }

        /// <summary>
        /// 获取引用集合
        /// </summary>
        /// <param name="referenceType">引用类型</param>
        /// <returns>引用集合</returns>
        private static ReferenceCollection GetReferenceCollection(Type referenceType)
        {
            if (referenceType == null)
            {
                throw new ArgumentNullException("空引用");
            }

            lock (s_ReferenceCollections)
            {
                ReferenceCollection referenceCollection = null;
                if (!s_ReferenceCollections.TryGetValue(referenceType, out referenceCollection))
                {
                    referenceCollection = new ReferenceCollection(referenceType);
                    s_ReferenceCollections.Add(referenceType, referenceCollection);
                }

                return referenceCollection;
            }
        }

        /// <summary>
        /// 检查是否存在指定类型的引用池
        /// </summary>
        /// <typeparam name="T">引用类型</typeparam>
        /// <returns>是否存在</returns>
        public static bool CanSpawn<T>() where T : class, IReference
        {
            return CanSpawn(typeof(T));
        }

        /// <summary>
        /// 检查是否存在指定类型的引用池
        /// </summary>
        /// <param name="referenceType">引用类型</param>
        /// <returns>是否存在</returns>
        public static bool CanSpawn(Type referenceType)
        {
            if (referenceType == null)
            {
                throw new ArgumentNullException("空引用");
            }

            if (!typeof(IReference).IsAssignableFrom(referenceType))
            {
                throw new ArgumentException(ThrTools.Text.Format("'{0}'空", referenceType.FullName));
            }

            lock (s_ReferenceCollections)
            {
                ReferenceCollection referenceCollection = null;
                if (s_ReferenceCollections.TryGetValue(referenceType, out referenceCollection))
                {
                    return referenceCollection.CanSpawn();
                }

                return false;
            }
        }
    }

    /// <summary>
    /// 引用集合类。用于管理某一类型的对象池，实现对象的获取、归还、批量添加、移除等操作，并统计相关使用信息。
    /// </summary>
    public sealed class ReferenceCollection
    {
        /// <summary>
        /// 存储未被使用的对象队列。
        /// </summary>
        private readonly Queue<IReference> m_References;
        /// <summary>
        /// 当前集合管理的对象类型。
        /// </summary>
        private readonly Type m_ReferenceType;
        /// <summary>
        /// 当前正在被使用的对象数量。
        /// </summary>
        private int m_UsingReferenceCount;
        /// <summary>
        /// 累计获取对象的次数。
        /// </summary>
        private int m_AcquireReferenceCount;
        /// <summary>
        /// 累计归还对象的次数。
        /// </summary>
        private int m_ReleaseReferenceCount;
        /// <summary>
        /// 累计添加对象到池中的次数。
        /// </summary>
        private int m_AddReferenceCount;
        /// <summary>
        /// 累计从池中移除对象的次数。
        /// </summary>
        private int m_RemoveReferenceCount;

        /// <summary>
        /// 构造函数，初始化引用集合。
        /// </summary>
        /// <param name="referenceType">对象类型</param>
        public ReferenceCollection(Type referenceType)
        {
            m_References = new Queue<IReference>();
            m_ReferenceType = referenceType;
            m_UsingReferenceCount = 0;
            m_AcquireReferenceCount = 0;
            m_ReleaseReferenceCount = 0;
            m_AddReferenceCount = 0;
            m_RemoveReferenceCount = 0;
        }

        /// <summary>
        /// 获取当前集合管理的对象类型。
        /// </summary>
        public Type ReferenceType => m_ReferenceType;
        /// <summary>
        /// 获取未被使用的对象数量。
        /// </summary>
        public int UnusedReferenceCount => m_References.Count;
        /// <summary>
        /// 获取正在被使用的对象数量。
        /// </summary>
        public int UsingReferenceCount => m_UsingReferenceCount;
        /// <summary>
        /// 获取累计获取对象的次数。
        /// </summary>
        public int AcquireReferenceCount => m_AcquireReferenceCount;
        /// <summary>
        /// 获取累计归还对象的次数。
        /// </summary>
        public int ReleaseReferenceCount => m_ReleaseReferenceCount;
        /// <summary>
        /// 获取累计添加对象到池中的次数。
        /// </summary>
        public int AddReferenceCount => m_AddReferenceCount;
        /// <summary>
        /// 获取累计从池中移除对象的次数。
        /// </summary>
        public int RemoveReferenceCount => m_RemoveReferenceCount;

        /// <summary>
        /// 获取一个对象实例。如果池中有未使用对象则复用，否则新建。
        /// </summary>
        public T Acquire<T>() where T : class, IReference, new()
        {
            if (typeof(T) != m_ReferenceType)
            {
                throw new ArgumentException(ThrTools.Text.Format("'{0}'空", typeof(T).FullName));
            }

            m_UsingReferenceCount++;
            m_AcquireReferenceCount++;
            lock (m_References)
            {
                if (m_References.Count > 0)
                {
                    return (T)m_References.Dequeue();
                }
            }

            m_AddReferenceCount++;
            return new T();
        }

        /// <summary>
        /// 获取一个对象实例（通过Type）。如果池中有未使用对象则复用，否则新建。
        /// </summary>
        public IReference Acquire(Type referenceType)
        {
            if (referenceType == null)
            {
                throw new ArgumentNullException("空引用");
            }

            if (referenceType != m_ReferenceType)
            {
                throw new ArgumentException(ThrTools.Text.Format("'{0}'空", referenceType.FullName));
            }

            m_UsingReferenceCount++;
            m_AcquireReferenceCount++;
            lock (m_References)
            {
                if (m_References.Count > 0)
                {
                    return m_References.Dequeue();
                }
            }

            m_AddReferenceCount++;
            return (IReference)Activator.CreateInstance(referenceType);
        }

        /// <summary>
        /// 归还一个对象到池中，并调用其Clear方法。
        /// </summary>
        public void Release(IReference reference)
        {
            reference.Clear();
            lock (m_References)
            {
                if (ReferencePool.EnableStrictCheck && m_References.Contains(reference))
                {
                    throw new ArgumentException("The reference has been released.");
                }

                m_References.Enqueue(reference);
            }

            m_ReleaseReferenceCount++;
            m_UsingReferenceCount--;
        }

        /// <summary>
        /// 批量添加对象到池中。
        /// </summary>
        public void Add<T>(int count) where T : class, IReference, new()
        {
            if (typeof(T) != m_ReferenceType)
            {
                throw new ArgumentException(ThrTools.Text.Format("'{0}'空", typeof(T).FullName));
            }

            lock (m_References)
            {
                m_AddReferenceCount += count;
                while (count-- > 0)
                {
                    m_References.Enqueue(new T());
                }
            }
        }

        /// <summary>
        /// 批量添加对象到池中（通过Type）。
        /// </summary>
        public void Add(Type referenceType, int count)
        {
            if (referenceType == null)
            {
                throw new ArgumentNullException("空引用");
            }

            if (referenceType != m_ReferenceType)
            {
                throw new ArgumentException(ThrTools.Text.Format("'{0}'空", referenceType.FullName));
            }

            lock (m_References)
            {
                m_AddReferenceCount += count;
                while (count-- > 0)
                {
                    m_References.Enqueue((IReference)Activator.CreateInstance(referenceType));
                }
            }
        }

        /// <summary>
        /// 批量移除池中的对象。
        /// </summary>
        public void Remove(int count)
        {
            lock (m_References)
            {
                if (count > m_References.Count)
                {
                    count = m_References.Count;
                }

                m_RemoveReferenceCount += count;
                while (count-- > 0)
                {
                    m_References.Dequeue();
                }
            }
        }

        /// <summary>
        /// 移除池中所有未使用的对象。
        /// </summary>
        public void RemoveAll()
        {
            lock (m_References)
            {
                m_RemoveReferenceCount += m_References.Count;
                m_References.Clear();
            }
        }

        /// <summary>
        /// 判断池中是否有可用对象。
        /// </summary>
        public bool CanSpawn()
        {
            lock (m_References)
            {
                return m_References.Count > 0;
            }
        }
    }

    /// <summary>
    /// 引用接口
    /// </summary>
    public interface IReference
    {
        /// <summary>
        /// 清理引用
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// 引用基类
    /// </summary>
    public abstract class ReferenceBase : IReference
    {
        public abstract void Clear();
    }
}
