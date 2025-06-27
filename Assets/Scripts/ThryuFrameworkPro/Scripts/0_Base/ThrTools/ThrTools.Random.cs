using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace ThryuFrameworkPro
{
    /// <summary>
    /// 随机工具类，提供各种随机数生成和随机操作功能
    /// </summary>
    public static partial class ThrTools
    {
        #region 权重随机
        /// <summary>
        /// 权重随机，返回被选中元素的索引
        /// </summary>
        /// <param name="weights">权重数组</param>
        /// <returns>被选中元素的索引，权重全为0时返回-1</returns>
        public static int WeightRandom(IList<float> weights)
        {
            if (weights == null || weights.Count == 0) return -1;
            float total = 0f;
            foreach (var w in weights)
            {
                total += Mathf.Max(0, w);
            }
            if (total <= 0f) return -1;
            float r = UnityEngine.Random.value * total;
            float sum = 0f;
            for (int i = 0; i < weights.Count; i++)
            {
                sum += Mathf.Max(0, weights[i]);
                if (r < sum)
                    return i;
            }
            return weights.Count - 1;
        }

        /// <summary>
        /// 权重随机，返回被选中元素的值
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="items">元素列表</param>
        /// <param name="weights">权重列表</param>
        /// <returns>被选中元素，权重全为0时返回default</returns>
        public static T WeightRandom<T>(IList<T> items, IList<float> weights)
        {
            int idx = WeightRandom(weights);
            if (idx < 0 || idx >= items.Count) return default;
            return items[idx];
        }

        /// <summary>
        /// 权重随机选择多个元素（不重复）
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="items">元素列表</param>
        /// <param name="weights">权重列表</param>
        /// <param name="count">选择数量</param>
        /// <returns>选中的元素列表</returns>
        public static List<T> WeightRandomMultiple<T>(IList<T> items, IList<float> weights, int count)
        {
            if (items == null || items.Count == 0 || count <= 0) return new List<T>();
            
            var result = new List<T>();
            var tempItems = new List<T>(items);
            var tempWeights = new List<float>(weights);
            
            count = Mathf.Min(count, items.Count);
            
            for (int i = 0; i < count; i++)
            {
                int idx = WeightRandom(tempWeights);
                if (idx >= 0)
                {
                    result.Add(tempItems[idx]);
                    tempItems.RemoveAt(idx);
                    tempWeights.RemoveAt(idx);
                }
            }
            
            return result;
        }

        /// <summary>
        /// 范围权重随机，输入一组区间和对应权重，返回命中的区间索引
        /// </summary>
        /// <param name="ranges">区间列表（如[0,10],[10,20]...）</param>
        /// <param name="weights">每个区间的权重</param>
        /// <returns>命中的区间索引，权重全为0时返回-1</returns>
        public static int RangeWeightRandom(IList<(float min, float max)> ranges, IList<float> weights)
        {
            int idx = WeightRandom(weights);
            return idx;
        }

        /// <summary>
        /// 范围权重随机，返回命中的区间内的一个随机值
        /// </summary>
        /// <param name="ranges">区间列表（如[0,10],[10,20]...）</param>
        /// <param name="weights">每个区间的权重</param>
        /// <returns>命中的区间内的随机值，权重全为0时返回0</returns>
        public static float RangeWeightRandomValue(IList<(float min, float max)> ranges, IList<float> weights)
        {
            int idx = WeightRandom(weights);
            if (idx < 0 || idx >= ranges.Count) return 0f;
            var (min, max) = ranges[idx];
            return UnityEngine.Random.Range(min, max);
        }
        #endregion

        #region 基础随机
        /// <summary>
        /// 随机返回true或false
        /// </summary>
        /// <returns>50%概率返回true</returns>
        public static bool RandomBool()
        {
            return UnityEngine.Random.value > 0.5f;
        }

        /// <summary>
        /// 按概率返回true或false
        /// </summary>
        /// <param name="probability">概率（0~1）</param>
        /// <returns>概率命中返回true</returns>
        public static bool RandomChance(float probability)
        {
            return UnityEngine.Random.value < probability;
        }

        /// <summary>
        /// 返回[min, max)区间的随机整数
        /// </summary>
        public static int RandomInt(int min, int max)
        {
            return UnityEngine.Random.Range(min, max);
        }

        /// <summary>
        /// 返回[min, max)区间的随机浮点数
        /// </summary>
        public static float RandomFloat(float min, float max)
        {
            return UnityEngine.Random.Range(min, max);
        }

        /// <summary>
        /// 生成高斯分布随机数（正态分布）
        /// </summary>
        /// <param name="mean">均值</param>
        /// <param name="standardDeviation">标准差</param>
        /// <returns>高斯分布随机数</returns>
        public static float RandomGaussian(float mean = 0f, float standardDeviation = 1f)
        {
            float u1 = UnityEngine.Random.value;
            float u2 = UnityEngine.Random.value;
            
            float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
            return mean + standardDeviation * randStdNormal;
        }
        #endregion

        #region 集合操作
        /// <summary>
        /// 随机打乱List顺序（Fisher-Yates洗牌）
        /// </summary>
        public static void Shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            for (int i = 0; i < n - 1; i++)
            {
                int j = UnityEngine.Random.Range(i, n);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// 随机打乱数组顺序（Fisher-Yates洗牌）
        /// </summary>
        public static void Shuffle<T>(T[] array)
        {
            int n = array.Length;
            for (int i = 0; i < n - 1; i++)
            {
                int j = UnityEngine.Random.Range(i, n);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }

        /// <summary>
        /// 从List中随机取一个元素
        /// </summary>
        public static T RandomItem<T>(IList<T> list)
        {
            if (list == null || list.Count == 0) return default;
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// 从数组中随机取一个元素
        /// </summary>
        public static T RandomItem<T>(T[] array)
        {
            if (array == null || array.Length == 0) return default;
            return array[UnityEngine.Random.Range(0, array.Length)];
        }

        /// <summary>
        /// 从List中随机选择多个元素（不重复）
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="list">元素列表</param>
        /// <param name="count">选择数量</param>
        /// <returns>选中的元素列表</returns>
        public static List<T> RandomItems<T>(IList<T> list, int count)
        {
            if (list == null || list.Count == 0 || count <= 0) return new List<T>();
            
            var tempList = new List<T>(list);
            Shuffle(tempList);
            
            count = Mathf.Min(count, tempList.Count);
            return tempList.Take(count).ToList();
        }

        /// <summary>
        /// 从数组中随机选择多个元素（不重复）
        /// </summary>
        /// <typeparam name="T">元素类型</typeparam>
        /// <param name="array">元素数组</param>
        /// <param name="count">选择数量</param>
        /// <returns>选中的元素列表</returns>
        public static List<T> RandomItems<T>(T[] array, int count)
        {
            if (array == null || array.Length == 0 || count <= 0) return new List<T>();
            
            var tempArray = new T[array.Length];
            Array.Copy(array, tempArray, array.Length);
            Shuffle(tempArray);
            
            count = Mathf.Min(count, tempArray.Length);
            return tempArray.Take(count).ToList();
        }
        #endregion

        #region Unity相关随机
        /// <summary>
        /// 生成一个随机颜色
        /// </summary>
        public static Color RandomColor()
        {
            return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
        }

        /// <summary>
        /// 生成一个随机颜色（带Alpha）
        /// </summary>
        public static Color RandomColorA()
        {
            return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
        }

        /// <summary>
        /// 生成一个随机单位向量（球面均匀分布）
        /// </summary>
        public static Vector3 RandomUnitVector3()
        {
            return UnityEngine.Random.onUnitSphere;
        }

        /// <summary>
        /// 生成一个随机二维单位向量
        /// </summary>
        public static Vector2 RandomUnitVector2()
        {
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        /// <summary>
        /// 生成随机旋转（四元数）
        /// </summary>
        public static Quaternion RandomRotation()
        {
            return UnityEngine.Random.rotation;
        }

        /// <summary>
        /// 生成随机旋转（欧拉角）
        /// </summary>
        /// <param name="minAngle">最小角度</param>
        /// <param name="maxAngle">最大角度</param>
        /// <returns>随机欧拉角</returns>
        public static Vector3 RandomEulerAngles(float minAngle = 0f, float maxAngle = 360f)
        {
            return new Vector3(
                UnityEngine.Random.Range(minAngle, maxAngle),
                UnityEngine.Random.Range(minAngle, maxAngle),
                UnityEngine.Random.Range(minAngle, maxAngle)
            );
        }

        /// <summary>
        /// 在指定范围内生成随机位置
        /// </summary>
        /// <param name="min">最小位置</param>
        /// <param name="max">最大位置</param>
        /// <returns>随机位置</returns>
        public static Vector3 RandomPosition(Vector3 min, Vector3 max)
        {
            return new Vector3(
                UnityEngine.Random.Range(min.x, max.x),
                UnityEngine.Random.Range(min.y, max.y),
                UnityEngine.Random.Range(min.z, max.z)
            );
        }

        /// <summary>
        /// 在指定范围内生成随机位置
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="size">范围大小</param>
        /// <returns>随机位置</returns>
        public static Vector3 RandomPositionCenter(Vector3 center, Vector3 size)
        {
            return center + new Vector3(
                UnityEngine.Random.Range(-size.x * 0.5f, size.x * 0.5f),
                UnityEngine.Random.Range(-size.y * 0.5f, size.y * 0.5f),
                UnityEngine.Random.Range(-size.z * 0.5f, size.z * 0.5f)
            );
        }

        /// <summary>
        /// 在圆形区域内生成随机位置
        /// </summary>
        /// <param name="center">圆心位置</param>
        /// <param name="radius">半径</param>
        /// <returns>随机位置</returns>
        public static Vector3 RandomPositionInCircle(Vector3 center, float radius)
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * radius;
            return center + new Vector3(randomCircle.x, 0, randomCircle.y);
        }

        /// <summary>
        /// 在球形区域内生成随机位置
        /// </summary>
        /// <param name="center">球心位置</param>
        /// <param name="radius">半径</param>
        /// <returns>随机位置</returns>
        public static Vector3 RandomPositionInSphere(Vector3 center, float radius)
        {
            return center + UnityEngine.Random.insideUnitSphere * radius;
        }
        #endregion

        #region 其他随机功能
        /// <summary>
        /// 生成随机字符串
        /// </summary>
        /// <param name="length">字符串长度</param>
        /// <param name="includeNumbers">是否包含数字</param>
        /// <param name="includeSpecialChars">是否包含特殊字符</param>
        /// <returns>随机字符串</returns>
        public static string RandomString(int length, bool includeNumbers = true, bool includeSpecialChars = false)
        {
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            const string numbers = "0123456789";
            const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";
            
            string chars = letters;
            if (includeNumbers) chars += numbers;
            if (includeSpecialChars) chars += specialChars;
            
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
            }
            
            return new string(result);
        }

        /// <summary>
        /// 随机选择枚举值
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <returns>随机枚举值</returns>
        public static T RandomEnum<T>() where T : Enum
        {
            var values = Enum.GetValues(typeof(T));
            return (T)values.GetValue(UnityEngine.Random.Range(0, values.Length));
        }

        /// <summary>
        /// 生成随机时间间隔
        /// </summary>
        /// <param name="minSeconds">最小秒数</param>
        /// <param name="maxSeconds">最大秒数</param>
        /// <returns>随机时间间隔</returns>
        public static float RandomTime(float minSeconds, float maxSeconds)
        {
            return UnityEngine.Random.Range(minSeconds, maxSeconds);
        }

        /// <summary>
        /// 设置随机种子
        /// </summary>
        /// <param name="seed">种子值</param>
        public static void SetRandomSeed(int seed)
        {
            UnityEngine.Random.InitState(seed);
        }

        /// <summary>
        /// 获取当前随机种子
        /// </summary>
        /// <returns>当前种子值</returns>
        public static int GetRandomSeed()
        {
            return UnityEngine.Random.state.GetHashCode();
        }
        #endregion
    }
}


