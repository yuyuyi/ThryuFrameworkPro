using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThryuFrameworkPro
{
    /// <summary>
    /// 字符串相关工具扩展类
    /// 提供字符串与数值、向量、颜色等类型的互转，以及字符串拼接等常用功能
    /// </summary>
    public static partial class ThrTools
    {
        #region string 输出拓展
        
        /// <summary>
        /// 分隔符数组，用于字符串分割
        /// </summary>
        static char[] m_SeParator = new char[3] { '(', ',', ')' };

        /// <summary>
        /// 将字符串转换为Int16类型
        /// </summary>
        public static Int16 ToInt16(this string value)
        {
            Int16 v = 0;
            if (Int16.TryParse(value, out v) == false)
            {
                Log.Error(string.Format("value = {0} can not convert to int16", value));
            }

            return v;
        }

        /// <summary>
        /// 将字符串转换为Int32类型
        /// </summary>
        public static Int32 ToInt32(this string value)
        {
            int v = 0;
            if (int.TryParse(value, out v) == false)
            {
                Log.Error(string.Format("value = {0} can not convert to int", value));
            }

            return v;
        }

        /// <summary>
        /// 将字符串转换为Int64类型
        /// </summary>
        public static Int64 ToInt64(this string value)
        {
            Int64 v = 0;
            if (Int64.TryParse(value, out v) == false)
            {
                Log.Error(string.Format("value = {0} can not convert to int64", value));
            }

            return v;
        }

        /// <summary>
        /// 将字符串转换为UInt16类型
        /// </summary>
        public static UInt16 ToUInt16(this string value)
        {
            UInt16 v = 0;
            if (UInt16.TryParse(value, out v) == false)
            {
                Log.Error(string.Format("value = {0} can not convert to uint16", value));
            }

            return v;
        }

        /// <summary>
        /// 将字符串转换为UInt32类型
        /// </summary>
        public static UInt32 ToUInt32(this string value)
        {
            UInt32 v = 0;
            if (UInt32.TryParse(value, out v) == false)
            {
                Log.Error(string.Format("value = {0} can not convert to uint32", value));
            }

            return v;
        }

        /// <summary>
        /// 将字符串转换为UInt64类型
        /// </summary>
        public static UInt64 ToUInt64(this string value)
        {
            UInt64 v = 0;
            if (UInt64.TryParse(value, out v) == false)
            {
                Log.Error(string.Format("value = {0} can not convert to uint64", value));
            }

            return v;
        }

        /// <summary>
        /// 将字符串转换为bool类型，仅当字符串为"true"（忽略大小写）时返回true，否则返回false
        /// </summary>
        public static bool ToBool(this string value)
        {
            string s = value.ToLower();
            return s.Equals("true");
        }

        /// <summary>
        /// 将字符串转换为float类型
        /// </summary>
        public static float ToFloat(this string value)
        {
            float v = 0;
            if (float.TryParse(value, out v) == false)
            {
                Log.InfoFormat("value:{0} can not convert to float", value);
            }

            return v;
        }

        /// <summary>
        /// 将int颜色值（0xRRGGBB）转换为Color（不含Alpha）
        /// </summary>
        public static Color ToColor3(this int color)
        {
            float r = (float)(color >> 16) / 255f;
            float g = (float)(color >> 8 & 255) / 255f;
            float b = (float)(color & 255) / 255f;
            return new Color(r, g, b);
        }

        /// <summary>
        /// 将int颜色值（0xAARRGGBB）转换为Color（含Alpha）
        /// </summary>
        public static Color ToColor4(this int color)
        {
            float a = (float)(color >> 24) / 255f;
            float r = (float)(color >> 16 & 255) / 255f;
            float g = (float)(color >> 8 & 255) / 255f;
            float b = (float)(color & 255) / 255f;
            return new Color(r, g, b, a);
        }

        /// <summary>
        /// 将形如"(1,2,3)"的字符串转换为int列表
        /// </summary>
        public static List<int> ToList(this string value)
        {
            List<int> list = new List<int>();
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            string[] array = value.Split(m_SeParator);
            for (int i = 1; i < array.Length - 1; i++)
            {
                int v = 0;
                int.TryParse(array[i], out v);
                list.Add(v);
            }

            return list;
        }

        /// <summary>
        /// 将形如"(x,y)"的字符串转换为Vector2
        /// </summary>
        public static Vector2 ToVector2(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Vector2.zero;
            }

            string[] array = value.Split(m_SeParator);
            return new Vector2(array[1].ToFloat(), array[2].ToFloat());
        }

        /// <summary>
        /// 将形如"(x,y,z)"的字符串转换为Vector3
        /// </summary>
        public static Vector3 ToVector3(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Vector3.zero;
            }

            string[] array = value.Split(m_SeParator);
            return new Vector3(array[1].ToFloat(), array[2].ToFloat(), array[3].ToFloat());
        }

        /// <summary>
        /// 将形如"(x,y,z,w)"的字符串转换为Vector4
        /// </summary>
        public static Vector4 ToVector4(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Vector3.zero;
            }

            string[] array = value.Split(m_SeParator);
            return new Vector4(array[1].ToFloat(), array[2].ToFloat(), array[3].ToFloat(), array[4].ToFloat());
        }

        #endregion

        #region string 拼接拓展
        
        /// <summary>
        /// 将字符串数组用指定分隔符拼接成一个字符串
        /// </summary>
        /// <param name="array">字符串数组</param>
        /// <param name="separator">分隔符</param>
        /// <returns>拼接后的字符串</returns>
        public static string Join(this string[] array, string separator)
        {
            if (array == null || array.Length == 0)
            {
                return string.Empty;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(array[i]);
                if (i < array.Length - 1)
                {
                    sb.Append(separator);
                }
            }
            return sb.ToString();
        }
        
        /// <summary>
        /// 将字符串列表用指定分隔符拼接成一个字符串
        /// </summary>
        /// <param name="list">字符串列表</param>
        /// <param name="separator">分隔符</param>
        /// <returns>拼接后的字符串</returns>
        public static string Join(this List<string> list, string separator)
        {
            if (list == null || list.Count == 0)
            {
                return string.Empty;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                sb.Append(list[i]);
                if (i < list.Count - 1)
                {
                    sb.Append(separator);
                }
            }
            return sb.ToString();
        }
        
        /// <summary>
        /// 将字符串集合用指定分隔符拼接成一个字符串
        /// </summary>
        /// <param name="collection">字符串集合</param>
        /// <param name="separator">分隔符</param>
        /// <returns>拼接后的字符串</returns>
        public static string Join(this IEnumerable<string> collection, string separator)
        {
            if (collection == null)
            {
                return string.Empty;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            using (var enumerator = collection.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    sb.Append(enumerator.Current);
                    while (enumerator.MoveNext())
                    {
                        sb.Append(separator);
                        sb.Append(enumerator.Current);
                    }
                }
            }
            return sb.ToString();
        }
        
        #endregion
    }
}