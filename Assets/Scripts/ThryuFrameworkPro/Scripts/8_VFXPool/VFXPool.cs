using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 基础框架版本更新说明：
 * 2025/6/30 羽于翼：初始版本
 * 2025/6/30 羽于翼：使用映射字典优化回收性能
 *
 */
namespace ThryuFrameworkPro
{
    /// <summary>
    /// 特效池管理器
    /// </summary>
    public class VFXPool : Singleton<VFXPool>
    {
        #region 字段
        /// <summary>
        /// 特效池字典,承载目前可使用的特效
        /// </summary>
        private readonly Dictionary<GameObject, Queue<GameObject>> _vfxPools = new Dictionary<GameObject, Queue<GameObject>>();
        
        /// <summary>
        /// 活跃的特效
        /// </summary>
        private HashSet<GameObject> activeVFX = new HashSet<GameObject>();
        
        /// <summary>
        /// 特效到池的映射 - 优化回收性能
        /// </summary>
        private readonly Dictionary<GameObject, Queue<GameObject>> _vfxToPool = new Dictionary<GameObject, Queue<GameObject>>();

        /// <summary>
        /// 特效父节点
        /// </summary>
        public Transform vfxParent;
        #endregion

        #region 获取
        /// <summary>
        /// 获取特效（简化版本）
        /// </summary>
        /// <param name="prefab">特效预制体</param>
        /// <returns>特效对象</returns>
        public GameObject Get(GameObject prefab)
        {
            return Get(prefab, Vector3.zero, Quaternion.identity, null);
        }

        /// <summary>
        /// 获取特效（带位置和旋转）
        /// </summary>
        /// <param name="prefab">特效预制体</param>
        /// <param name="position">位置</param>
        /// <param name="rotation">旋转</param>
        /// <param name="parent">父节点</param>
        /// <returns>特效对象</returns>
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                Log.Error("[VFXPool] 特效预制体不能为空");
                return null;
            }

            // 获取或创建特效池
            var vfxQueue = GetOrCreateVFXPool(prefab);
            
            // 从队列中获取可用特效
            GameObject vfx = null;
            if (vfxQueue.Count > 0)
            {
                vfx = vfxQueue.Dequeue();
            }
            else
            {
                // 创建新的特效对象
                vfx = CreateNewVFX(prefab);
            }

            if (vfx != null)
            {
                // 添加到活跃集合
                activeVFX.Add(vfx);
                
                // 建立映射关系 - 优化回收性能
                _vfxToPool[vfx] = vfxQueue;
                
                // 设置特效属性
                SetupVFX(vfx, prefab, position, rotation, parent);
            }

            return vfx;
        }
        #endregion

        #region 回收
        /// <summary>
        /// 回收特效
        /// </summary>
        /// <param name="vfx">特效对象</param>
        public void Release(GameObject vfx)
        {
            if (vfx == null) return;
            
            // 从活跃集合中移除
            activeVFX.Remove(vfx);
            
            // 重置特效
            ResetVFX(vfx);
            
            // 设置父节点
            vfx.transform.SetParent(vfxParent);
            
            // 停用特效
            vfx.SetActive(false);
            
            // O(1)查找对应的池 - 性能优化
            if (_vfxToPool.TryGetValue(vfx, out var queue))
            {
                queue.Enqueue(vfx);
                _vfxToPool.Remove(vfx); // 清理映射
            }
            else
            {
                // 如果找不到对应的池，直接销毁
                Destroy(vfx);
            }
        }
        #endregion

        #region 清理
        /// <summary>
        /// 清理特定特效池
        /// </summary>
        /// <param name="prefab">特效预制体</param>
        public void ClearVFXPool(GameObject prefab)
        {
            if (_vfxPools.TryGetValue(prefab, out var vfxQueue))
            {
                // 清理队列中的所有对象
                while (vfxQueue.Count > 0)
                {
                    var vfx = vfxQueue.Dequeue();
                    if (vfx != null)
                    {
                        _vfxToPool.Remove(vfx); // 清理映射
                        Destroy(vfx);
                    }
                }
                
                _vfxPools.Remove(prefab);
                Log.Info($"[VFXPool] 清理特效池: {prefab.name}");
            }
        }

        /// <summary>
        /// 清理所有特效池
        /// </summary>
        public void ClearAllVFXPool()
        {
            // 清理所有活跃特效
            foreach (var vfx in activeVFX)
            {
                if (vfx != null)
                {
                    _vfxToPool.Remove(vfx); // 清理映射
                    Destroy(vfx);
                }
            }
            activeVFX.Clear();
            
            // 清理所有池
            foreach (var kvp in _vfxPools)
            {
                var vfxQueue = kvp.Value;
                while (vfxQueue.Count > 0)
                {
                    var vfx = vfxQueue.Dequeue();
                    if (vfx != null)
                    {
                        _vfxToPool.Remove(vfx); // 清理映射
                        Destroy(vfx);
                    }
                }
            }
            
            _vfxPools.Clear();
            _vfxToPool.Clear(); // 清理映射字典
            Log.Info("[VFXPool] 清理所有特效池");
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 获取或创建特效池
        /// </summary>
        /// <param name="prefab">特效预制体</param>
        /// <returns>特效队列</returns>
        private Queue<GameObject> GetOrCreateVFXPool(GameObject prefab)
        {
            if (!_vfxPools.TryGetValue(prefab, out var vfxQueue))
            {
                vfxQueue = new Queue<GameObject>();
                _vfxPools[prefab] = vfxQueue;
            }
            
            return vfxQueue;
        }

        /// <summary>
        /// 创建新的特效对象
        /// </summary>
        /// <param name="prefab">特效预制体</param>
        /// <returns>特效对象</returns>
        private GameObject CreateNewVFX(GameObject prefab)
        {
            var vfx = Instantiate(prefab, vfxParent);
            if (vfx == null)
            {
                return null;
            }
            
            // 计算当前池中的对象数量
            int count = 0;
            if (_vfxPools.TryGetValue(prefab, out var queue))
            {
                count = queue.Count;
            }
            
            vfx.name = $"{prefab.name}_Pooled_{count}";
            
            return vfx;
        }

        /// <summary>
        /// 设置特效属性
        /// </summary>
        /// <param name="vfx">特效对象</param>
        /// <param name="prefab">预制体</param>
        /// <param name="position">位置</param>
        /// <param name="rotation">旋转</param>
        /// <param name="parent">父节点</param>
        private void SetupVFX(GameObject vfx, GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            // 设置父节点
            if (parent != null)
            {
                vfx.transform.SetParent(parent);
            }
            
            // 设置位置和旋转
            vfx.transform.position = position;
            vfx.transform.rotation = rotation;
            vfx.transform.localScale = prefab.transform.localScale;
            
            // 激活特效
            vfx.SetActive(true);
        }

        /// <summary>
        /// 重置特效
        /// </summary>
        /// <param name="vfx">特效对象</param>
        private void ResetVFX(GameObject vfx)
        {
            // 重置Transform
            vfx.transform.localPosition = Vector3.zero;
            vfx.transform.localRotation = Quaternion.identity;
            vfx.transform.localScale = Vector3.one;
            
            // 停止所有粒子系统
            var particleSystems = vfx.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                ps.Stop();
                ps.Clear();
            }
            
            // 停止所有音频源
            var audioSources = vfx.GetComponentsInChildren<AudioSource>();
            foreach (var audio in audioSources)
            {
                audio.Stop();
            }
        }

        /// <summary>
        /// 内部回收特效
        /// </summary>
        /// <param name="vfx">特效对象</param>
        private void ReleaseVFXInternal(GameObject vfx)
        {
            // 从活跃集合中移除
            activeVFX.Remove(vfx);
            
            // 重置特效
            ResetVFX(vfx);
            
            // 设置父节点
            vfx.transform.SetParent(vfxParent);
            
            // 停用特效
            vfx.SetActive(false);
            
            // O(1)查找对应的池
            if (_vfxToPool.TryGetValue(vfx, out var queue))
            {
                queue.Enqueue(vfx);
                _vfxToPool.Remove(vfx); // 清理映射
            }
            else
            {
                // 如果找不到对应的池，直接销毁
                Destroy(vfx);
            }
        }
        #endregion

        #region 生命周期
        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            
            // 确保父节点存在
            if (vfxParent == null)
            {
                var vfxParentGO = new GameObject("VFXPool_Parent");
                vfxParent = vfxParentGO.transform;
                vfxParent.SetParent(transform);
                Log.Info("[VFXPool] 自动创建特效父节点");
            }
        }

        protected override void OnSingletonDestroy()
        {
            base.OnSingletonDestroy();
            ClearAllVFXPool();
        }
        #endregion
    }
}