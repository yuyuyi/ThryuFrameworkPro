using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;

namespace ThryuFrameworkPro
{
    /// <summary>
    /// JSON工具类，提供JSON序列化、反序列化、文件操作等功能
    /// </summary>
    public static partial class ThrTools
    {
        public static class Json
        {
            #region 基础序列化
            /// <summary>
            /// 将对象序列化为JSON字符串（美化格式）
            /// </summary>
            /// <param name="obj">要序列化的对象</param>
            /// <returns>JSON字符串</returns>
            public static string Serialize(object obj)
            {
                try
                {
                    return JsonConvert.SerializeObject(obj, Formatting.Indented);
                }
                catch (Exception ex)
                {
                    Log.Error($"JSON序列化失败: {ex.Message}");
                    return null;
                }
            }

            /// <summary>
            /// 将对象序列化为JSON字符串（压缩格式）
            /// </summary>
            /// <param name="obj">要序列化的对象</param>
            /// <returns>压缩的JSON字符串</returns>
            public static string SerializeCompact(object obj)
            {
                try
                {
                    return JsonConvert.SerializeObject(obj, Formatting.None);
                }
                catch (Exception ex)
                {
                    Log.Error($"JSON序列化失败: {ex.Message}");
                    return null;
                }
            }

            /// <summary>
            /// 将对象序列化为JSON字符串（自定义格式）
            /// </summary>
            /// <param name="obj">要序列化的对象</param>
            /// <param name="formatting">格式化选项</param>
            /// <returns>JSON字符串</returns>
            public static string Serialize(object obj, Formatting formatting)
            {
                try
                {
                    return JsonConvert.SerializeObject(obj, formatting);
                }
                catch (Exception ex)
                {
                    Log.Error($"JSON序列化失败: {ex.Message}");
                    return null;
                }
            }

            /// <summary>
            /// 将JSON字符串反序列化为指定类型的对象
            /// </summary>
            /// <typeparam name="T">目标类型</typeparam>
            /// <param name="json">JSON字符串</param>
            /// <returns>反序列化的对象</returns>
            public static T Deserialize<T>(string json)
            {
                try
                {
                    if (string.IsNullOrEmpty(json))
                    {
                        Log.Warning("JSON字符串为空");
                        return default(T);
                    }
                    return JsonConvert.DeserializeObject<T>(json);
                }
                catch (Exception ex)
                {
                    Log.Error($"JSON反序列化失败: {ex.Message}");
                    return default(T);
                }
            }

            /// <summary>
            /// 将JSON字符串反序列化为动态对象
            /// </summary>
            /// <param name="json">JSON字符串</param>
            /// <returns>动态对象</returns>
            public static dynamic Deserialize(string json)
            {
                try
                {
                    if (string.IsNullOrEmpty(json))
                    {
                        Log.Warning("JSON字符串为空");
                        return null;
                    }
                    return JsonConvert.DeserializeObject(json);
                }
                catch (Exception ex)
                {
                    Log.Error($"JSON反序列化失败: {ex.Message}");
                    return null;
                }
            }
            #endregion

            #region 文件操作
            /// <summary>
            /// 将对象序列化并保存到文件
            /// </summary>
            /// <param name="obj">要保存的对象</param>
            /// <param name="filePath">文件路径</param>
            /// <param name="formatting">格式化选项</param>
            /// <returns>是否保存成功</returns>
            public static bool SaveToFile(object obj, string filePath, Formatting formatting = Formatting.Indented)
            {
                try
                {
                    string json = Serialize(obj, formatting);
                    if (json == null) return false;

                    // 确保目录存在
                    string directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.WriteAllText(filePath, json, Encoding.UTF8);
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error($"保存JSON文件失败: {ex.Message}");
                    return false;
                }
            }

            /// <summary>
            /// 从文件加载并反序列化对象
            /// </summary>
            /// <typeparam name="T">目标类型</typeparam>
            /// <param name="filePath">文件路径</param>
            /// <returns>反序列化的对象</returns>
            public static T LoadFromFile<T>(string filePath)
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        Log.Warning($"文件不存在: {filePath}");
                        return default(T);
                    }

                    string json = File.ReadAllText(filePath, Encoding.UTF8);
                    return Deserialize<T>(json);
                }
                catch (Exception ex)
                {
                    Log.Error($"加载JSON文件失败: {ex.Message}");
                    return default(T);
                }
            }

            /// <summary>
            /// 从文件加载并反序列化为动态对象
            /// </summary>
            /// <param name="filePath">文件路径</param>
            /// <returns>动态对象</returns>
            public static dynamic LoadFromFile(string filePath)
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        Log.Warning($"文件不存在: {filePath}");
                        return null;
                    }

                    string json = File.ReadAllText(filePath, Encoding.UTF8);
                    return Deserialize(json);
                }
                catch (Exception ex)
                {
                    Log.Error($"加载JSON文件失败: {ex.Message}");
                    return null;
                }
            }
            #endregion

            #region 工具方法
            /// <summary>
            /// 验证JSON字符串是否有效
            /// </summary>
            /// <param name="json">JSON字符串</param>
            /// <returns>是否有效</returns>
            public static bool IsValid(string json)
            {
                try
                {
                    if (string.IsNullOrEmpty(json)) return false;
                    JsonConvert.DeserializeObject(json);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            /// <summary>
            /// 美化JSON字符串（添加缩进和换行）
            /// </summary>
            /// <param name="json">JSON字符串</param>
            /// <returns>美化后的JSON字符串</returns>
            public static string Beautify(string json)
            {
                try
                {
                    if (!IsValid(json)) return json;
                    var obj = JsonConvert.DeserializeObject(json);
                    return JsonConvert.SerializeObject(obj, Formatting.Indented);
                }
                catch (Exception ex)
                {
                    Log.Error($"美化JSON失败: {ex.Message}");
                    return json;
                }
            }

            /// <summary>
            /// 压缩JSON字符串（移除空格和换行）
            /// </summary>
            /// <param name="json">JSON字符串</param>
            /// <returns>压缩后的JSON字符串</returns>
            public static string Compress(string json)
            {
                try
                {
                    if (!IsValid(json)) return json;
                    var obj = JsonConvert.DeserializeObject(json);
                    return JsonConvert.SerializeObject(obj, Formatting.None);
                }
                catch (Exception ex)
                {
                    Log.Error($"压缩JSON失败: {ex.Message}");
                    return json;
                }
            }

            /// <summary>
            /// 深度克隆对象（通过JSON序列化实现）
            /// </summary>
            /// <typeparam name="T">对象类型</typeparam>
            /// <param name="obj">要克隆的对象</param>
            /// <returns>克隆的对象</returns>
            public static T DeepClone<T>(T obj)
            {
                try
                {
                    if (obj == null) return default(T);
                    string json = Serialize(obj);
                    return Deserialize<T>(json);
                }
                catch (Exception ex)
                {
                    Log.Error($"深度克隆失败: {ex.Message}");
                    return default(T);
                }
            }

            /// <summary>
            /// 将对象转换为指定类型
            /// </summary>
            /// <typeparam name="T">目标类型</typeparam>
            /// <param name="obj">源对象</param>
            /// <returns>转换后的对象</returns>
            public static T Convert<T>(object obj)
            {
                try
                {
                    if (obj == null) return default(T);
                    string json = Serialize(obj);
                    return Deserialize<T>(json);
                }
                catch (Exception ex)
                {
                    Log.Error($"对象转换失败: {ex.Message}");
                    return default(T);
                }
            }
            #endregion

            #region 批量操作
            /// <summary>
            /// 批量保存对象到文件
            /// </summary>
            /// <param name="objects">对象字典（文件名->对象）</param>
            /// <param name="basePath">基础路径</param>
            /// <returns>成功保存的文件数量</returns>
            public static int BatchSaveToFile(Dictionary<string, object> objects, string basePath)
            {
                int successCount = 0;
                foreach (var kvp in objects)
                {
                    string filePath = Path.Combine(basePath, kvp.Key + ".json");
                    if (SaveToFile(kvp.Value, filePath))
                    {
                        successCount++;
                    }
                }
                return successCount;
            }

            /// <summary>
            /// 批量加载文件中的对象
            /// </summary>
            /// <typeparam name="T">对象类型</typeparam>
            /// <param name="filePaths">文件路径列表</param>
            /// <returns>加载的对象列表</returns>
            public static List<T> BatchLoadFromFile<T>(List<string> filePaths)
            {
                var results = new List<T>();
                foreach (string filePath in filePaths)
                {
                    T obj = LoadFromFile<T>(filePath);
                    if (obj != null)
                    {
                        results.Add(obj);
                    }
                }
                return results;
            }
            #endregion
        }
    }
}
