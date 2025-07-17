using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/* 基础框架版本更新说明：
 * 2025/6/27 羽于翼：初始版本，基本完成了属性系统的设计  
 * 
 */
namespace ThryuFrameworkPro
{
    /* 属性系统使用说明：
     * 使用AttributeSystem类来管理所有属性，可以方便的实现增删
     */
    
    /* 属性系统说明：
     * 最终属性 = 白字属性 + 绿字属性 + 你需要的其他字属性(通过修改AttributeType进行拓展，在End前添加新的属性id)
     *
     * 单一属性计算：
     * 最终值 = ((基础值 + 加值1) * 百分比加值1 + 加值2) * 百分比加值2 ...
     *
     * 例子：
     * | Equipment | +50 | +10% | 精钢剑 |
     * | Skill | +30 | +15% | 狂暴技能 |
     *
     * 初始值: 0
     * Equipment (0): (0 + 50) * (1 + 10%) = 55.0
     * Skill (1): (55 + 30) * (1 + 15%) = 97.75
     *
     * 注意：乘区的计算顺序强绑定于枚举id
     *
     * 实际属性是在游戏中使用的属性，直接添加即可
     */
    
    
    /// <summary>
    /// 属性类型枚举
    /// </summary>
    public enum AttributeType
    {
        /// <summary>
        /// 白字属性
        /// </summary>
        [LabelText("白字属性")]
        Base,
        /// <summary>
        /// 绿字属性
        /// </summary>
        [LabelText("绿字属性")]
        Bonus,
        
        /// <summary>
        /// 用于标识长度，不允许参与计算和使用
        /// </summary>
        [LabelText("禁用-长度标识")]
        End,
    }
    
    /// <summary>
    /// 乘区类型枚举
    /// </summary>
    public enum MultiplierType
    {
        [LabelText("装备")]
        Equipment,
        [LabelText("被动技能")]
        PassiveSkill,
        [LabelText("Buff")]
        Buff,
        
        /// <summary>
        /// 用于标识长度，不允许参与计算使用
        /// </summary>
        [LabelText("禁用-长度标识")]
        End,
    }
    
    /// <summary>
    /// 实际属性枚举
    /// 使用中文原因：可以直接获取到属性中文名称
    /// </summary>
    public enum AttributeEnum
    {
        最大生命值,
        最大魔法值,
        
        破坏因子,
        反射因子,
        体质因子,
        能量因子,
        精神因子,
        
        生命恢复,
        魔法恢复,
        
        斩击增伤,
        穿刺增伤,
        钝器增伤,
        
        科技增伤,
        
        奥术增伤,
        火增伤,
        雷增伤,
        毒增伤,
        混乱增伤,
        神圣增伤,
        
        物理增伤,
        魔法增伤,
        
        最终增伤,
        
        暴击率,
        暴击伤害,
        
        魔法暴击率,
        魔法暴击伤害,
        
        召唤强度,
        
        //抗性
        斩击抗性,
        穿刺抗性,
        钝器抗性,
        
        科技抗性,
        
        奥术抗性,
        火抗性,
        雷抗性,
        毒抗性,
        混乱抗性,
        神圣抗性,
        
        物理减伤,
        魔法减伤,
        最终减伤,
        
        移动速度,
        攻击动作速度,
        施法动作速度,
        换弹速度,
        射速提升,
        
        // 用于标识长度，不允许参与计算和使用
        [LabelText("禁用-长度标识")]
        End 
    }
    
    /// <summary>
    /// 属性变化事件参数
    /// </summary>
    public class AttributeChangedEventArgs : EventArgs
    {
        public string AttributeName { get; }
        public AttributeType AttributeType { get; }
        public double OldValue { get; }
        public double NewValue { get; }

        public AttributeChangedEventArgs(string attributeName, AttributeType attributeType, 
            double oldValue, double newValue)
        {
            AttributeName = attributeName;
            AttributeType = attributeType;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    /// <summary>
    /// 属性修饰符类
    /// </summary>
    [Serializable]
    public class AttributeModifier
    {
        [LabelText("数值")]
        public double Value;
        
        [LabelText("是否百分比")]
        public bool IsPercentage;
        
        [LabelText("属性类型")]
        public AttributeType AttributeType;
        
        [LabelText("乘区类型")]
        public MultiplierType MultiplierType;

        public AttributeModifier(double _value, bool _isPercentage, AttributeType _attributeType, MultiplierType _multiplierType)
        {
            Value = _value;
            IsPercentage = _isPercentage;
            AttributeType = _attributeType;
            MultiplierType = _multiplierType;
        }
    }

    [Serializable]
    public class AttributeAdd
    {
        [LabelText("属性类型")]
        public AttributeEnum m_type;
        
        [LabelText("属性")]
        public AttributeModifier modifier;
    }

    /// <summary>
    /// 属性修饰符收集器
    /// 用于收集、管理和计算一组属性修饰符（AttributeModifier），
    /// 支持添加、移除、清空修饰符，并自动维护总数值与总百分比值。
    /// </summary>
    public class AttributeModifierCollector
    {
        /// <summary>
        /// 所有修饰符的总数值（非百分比部分）。
        /// 通过Update方法自动计算。
        /// </summary>
        public double totalValue { get; private set; }

        /// <summary>
        /// 所有修饰符的总百分比值（百分比部分）。
        /// 通过Update方法自动计算。
        /// </summary>
        public double totalPercentageValue { get; private set; }
        
        /// <summary>
        /// 当前收集的属性修饰符列表。
        /// </summary>
        public List<AttributeModifier> Modifiers { get; private set; } = new List<AttributeModifier>();

        /// <summary>
        /// 标记当前修饰符集合是否已变更，需重新计算总值。
        /// </summary>
        public bool isDirty;

        /// <summary>
        /// 添加一个属性修饰符到集合中，并标记为Dirty。
        /// </summary>
        /// <param name="_modifier">要添加的属性修饰符</param>
        public void AddModifier(AttributeModifier _modifier)
        {
            if (_modifier == null) return;
            
            Modifiers.Add(_modifier);
            isDirty = true;
        }
        
        /// <summary>
        /// 从集合中移除指定的属性修饰符，并标记为Dirty。
        /// </summary>
        /// <param name="modifier">要移除的属性修饰符</param>
        /// <returns>移除成功返回true，否则返回false</returns>
        public bool RemoveModifier(AttributeModifier modifier)
        {
            if (modifier == null) return false;

            bool removed = Modifiers.Remove(modifier);
            if (removed) isDirty = true;
            return removed;
        }
        
        /// <summary>
        /// 清空所有属性修饰符，并标记为Dirty。
        /// </summary>
        public void Clear()
        {
            Modifiers.Clear();
            isDirty = true;
        }
        
        /// <summary>
        /// 获取当前所有修饰符的总数值与总百分比值。
        /// 若数据已变更会自动重新计算。
        /// </summary>
        /// <returns>元组：(总数值, 总百分比值)</returns>
        public (double totalValue, double totalPercentage) GetTotalValues()
        {
            if (isDirty)
            {
                Update();
            }
            return (totalValue, totalPercentageValue);
        }

        /// <summary>
        /// 重新计算所有修饰符的总数值与总百分比值，并清除Dirty标记。
        /// </summary>
        public void Update()
        {
            totalValue = 0;
            totalPercentageValue = 0;
            
            foreach (var modifier in Modifiers)
            {
                if (modifier.IsPercentage)
                {
                    totalPercentageValue += modifier.Value;
                }
                else
                {
                    totalValue += modifier.Value;
                }
            }

            isDirty = false;
        }
        
        /// <summary>
        /// 手动标记集合为Dirty，表示需要重新计算总值。
        /// </summary>
        public void MarkDirty()
        {
            isDirty = true;
        }
        
        /// <summary>
        /// 获取当前所有属性修饰符的副本列表。
        /// </summary>
        /// <returns>属性修饰符列表副本</returns>
        public List<AttributeModifier> GetModifiers()
        {
            return new List<AttributeModifier>(Modifiers);
        }
    }
    
    /// <summary>
    /// 单个属性
    /// 用于管理属性的基础值、修饰符收集与属性值的动态计算。
    /// 支持事件通知、自动更新、修饰符增删等功能。
    /// </summary>
    public class Attribute
    {
        #region 事件
        /// <summary>
        /// 属性值变化事件。
        /// 当BaseValue或BonusValue发生变化时触发。
        /// </summary>
        public event EventHandler<AttributeChangedEventArgs> OnAttributeChanged;
        #endregion

        #region 字段
        /// <summary>
        /// 属性名称。
        /// </summary>
        [ShowInInspector]
        [LabelText("属性名称")]
        public string Name { get; private set; }

        /// <summary>
        /// 白字属性（基础属性）最终值。
        /// </summary>
        [ShowInInspector]
        [LabelText("白字属性")]
        [BoxGroup("属性值")]
        [ReadOnly]
        public double BaseValue { get; private set; }

        /// <summary>
        /// 绿字属性（附加属性）最终值。
        /// </summary>
        [ShowInInspector]
        [LabelText("绿字属性")]
        [BoxGroup("属性值")]
        [ReadOnly]
        public double BonusValue { get; private set; }

        /// <summary>
        /// 总属性值（白字+绿字）。
        /// </summary>
        [ShowInInspector]
        [LabelText("总属性")]
        [BoxGroup("属性值")]
        [ReadOnly]
        public double TotalValue { get; private set; }
        #endregion

        #region 属性

        /// <summary>
        /// 属性基础值（未加成前）。
        /// </summary>
        private double _baseValue = 0f;
        /// <summary>
        /// 属性修饰符收集器二维数组，按属性类型和乘区类型分类。
        /// </summary>
        private AttributeModifierCollector[,] collectors;

        /// <summary>
        /// 标记属性是否需要重新计算。
        /// </summary>
        private bool _isDirty = true;
        /// <summary>
        /// 是否启用属性变化事件。
        /// </summary>
        private bool _enableEvents = true;
        /// <summary>
        /// 是否启用自动更新（脏时自动刷新）。
        /// </summary>
        private bool _enableAutoUpdate = true;

        #endregion
        
        #region 构造函数
        /// <summary>
        /// 构造函数，初始化属性名称和基础值。
        /// </summary>
        /// <param name="name">属性名称</param>
        /// <param name="baseValue">基础值</param>
        public Attribute(string name, double baseValue = 0f)
        {
            Name = name;
            _baseValue = baseValue;
            InitializeCollectors();
            Update();
        }
        #endregion

        #region 初始化

        /// <summary>
        /// 初始化修饰符收集器数组。
        /// </summary>
        private void InitializeCollectors()
        {
            collectors = new AttributeModifierCollector[(int)AttributeType.End, (int)(MultiplierType.End)];
        }
        
        /// <summary>
        /// 设置基础值，并触发属性变化事件。
        /// </summary>
        /// <param name="value">新的基础值</param>
        public void SetBaseValue(double value)
        {
            double oldBaseValue = BaseValue;
            _baseValue = value;
            MarkDirty();
            UpdateIfNeeded();

            if (_enableEvents && Math.Abs(oldBaseValue - BaseValue) > 0.001f)
            {
                OnAttributeChanged?.Invoke(this, new AttributeChangedEventArgs(Name, AttributeType.Base, oldBaseValue, BaseValue));
            }
        }

        #endregion
        
        #region 性能控制

        /// <summary>
        /// 设置是否启用属性变化事件。
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetEventsEnabled(bool enabled)
        {
            _enableEvents = enabled;
        }

        /// <summary>
        /// 设置是否启用自动更新。
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetAutoUpdateEnabled(bool enabled)
        {
            _enableAutoUpdate = enabled;
        }

        /// <summary>
        /// 标记属性为脏，需要重新计算。
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }

        /// <summary>
        /// 若属性为脏且启用自动更新，则自动刷新属性值。
        /// </summary>
        public void UpdateIfNeeded()
        {
            if (_isDirty && _enableAutoUpdate)
            {
                Update();
            }
        }
        
        #endregion

        #region 数据更新

        /// <summary>
        /// 刷新属性的最终值，重新计算白字、绿字和总属性，并触发事件。
        /// </summary>
        public void Update()
        {
            double oldBaseValue = BaseValue;
            double oldBonusValue = BonusValue;

            // 计算白字属性
            double targetValue = _baseValue;

            // 计算绿字属性
            double targetBonusValue = 0f;

            // 遍历所有乘区
            for (int i = 0; i < (int)AttributeType.End; i++)
            {
                //白字属性和绿字属性计算方式相同
                for (int j = 0; j < (int)MultiplierType.End; j++)
                {
                    if (collectors[i, j] == null) continue;

                    // 获取当前乘区的收集器
                    var collector = collectors[i, j];
                    
                    // 如果收集器是脏的，更新它
                    if (collector.isDirty)
                    {
                        collector.Update();
                    }

                    // 获取总数值和总百分比
                    var (totalValue, totalPercentage) = collector.GetTotalValues();

                    // 根据属性类型累加到对应的值
                    if ((AttributeType)i == AttributeType.Base)
                    {
                        targetValue += totalValue;
                        targetValue *= 1 + totalPercentage; // 百分比
                    }
                    else if ((AttributeType)i == AttributeType.Bonus)
                    {
                        targetBonusValue += totalValue;
                        targetBonusValue *= 1 + totalPercentage; // 百分比
                    }
                }
            }
            
            BaseValue = targetValue;
            BonusValue = targetBonusValue;
            TotalValue = BaseValue + BonusValue;

            _isDirty = false;

            // 触发事件
            if (_enableEvents)
            {
                if (Math.Abs(oldBaseValue - BaseValue) > 0.001f)
                {
                    OnAttributeChanged?.Invoke(this, new AttributeChangedEventArgs(Name, AttributeType.Base, oldBaseValue, BaseValue));
                }
                if (Math.Abs(oldBonusValue - BonusValue) > 0.001f)
                {
                    OnAttributeChanged?.Invoke(this, new AttributeChangedEventArgs(Name, AttributeType.Bonus, oldBonusValue, BonusValue));
                }
            }
        }
        
        #endregion
        
        #region 修饰器管理

        /// <summary>
        /// 添加属性修饰符到对应的收集器，并自动刷新属性。
        /// </summary>
        /// <param name="_modifier">要添加的修饰符</param>
        public void AddModifier(AttributeModifier _modifier)
        {
            if (_modifier == null) return;

            var multiplierType = _modifier.MultiplierType;
            var attributeType = _modifier.AttributeType;

            if (collectors[(int)attributeType, (int)multiplierType] == null)
            {
                collectors[(int)attributeType, (int)multiplierType] = new AttributeModifierCollector();
            }
            
            collectors[(int)attributeType, (int)multiplierType].AddModifier(_modifier);
            
            MarkDirty();
            UpdateIfNeeded();
        }

        /// <summary>
        /// 移除指定的属性修饰符，并自动刷新属性。
        /// </summary>
        /// <param name="_modifier">要移除的修饰符</param>
        /// <returns>移除成功返回true，否则返回false</returns>
        public bool RemoveModifier(AttributeModifier _modifier)
        {
            if (_modifier == null) return false;

            var multiplierType = _modifier.MultiplierType;
            var attributeType = _modifier.AttributeType;
            
            if (collectors[(int)attributeType, (int)multiplierType] == null)
            {
                return false;
            }
            
            bool removed = collectors[(int)attributeType, (int)multiplierType].RemoveModifier(_modifier);
            if (removed)
            {
                MarkDirty();
                UpdateIfNeeded();
            }
            return removed;
        }

        /// <summary>
        /// 清空所有修饰符，并自动刷新属性。
        /// </summary>
        public void ClearAllModifiers()
        {
            for (int i = 0; i < (int)AttributeType.End; i++)
            {
                for (int j = 0; j < (int)MultiplierType.End; j++)
                {
                    if (collectors[i, j] != null)
                    {
                        collectors[i, j].Clear();
                    }
                }
            }
            MarkDirty();
            UpdateIfNeeded();
        }
        #endregion
    }
    
    /// <summary>
    /// 用于放在战斗管理器或者参与属性计算单位身上的类
    /// 在需要的时候直接使用这个,每个作战单位身上都应该有一个AttributeSystem
    /// </summary>
    public class AttributeSystem
    {
        #region 事件
        /// <summary>
        /// 属性变化事件
        /// </summary>
        public event EventHandler<AttributeChangedEventArgs> OnAnyAttributeChanged;
        #endregion

        #region 字段
        /// <summary>
        /// 所有属性的字典
        /// </summary>
        public Dictionary<AttributeEnum, Attribute> Attributes { get; private set; } = new Dictionary<AttributeEnum, Attribute>();
        
        /// <summary>
        /// 是否启用事件
        /// </summary>
        private bool _enableEvents = true;
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数，初始化所有属性
        /// </summary>
        public AttributeSystem(bool fullInit = false,List<AttributeEnum> initEnum = null)
        {
            if (fullInit)
            {
                InitializeAllAttributes();
            }
            else
            {
                InitializeAttributes(initEnum);
            }
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化所有属性
        /// </summary>
        private void InitializeAllAttributes()
        {
            
            for (int i = 0; i < (int)AttributeEnum.End; i++)
            {
                InitializeAttribute((AttributeEnum)i);
            }
        }
        
        /// <summary>
        /// 初始化属性组，如场景静物没有暴击等属性，可以只进行部分初始化
        /// </summary>
        /// <param name="initEnum"></param>
        private void InitializeAttributes(List<AttributeEnum> initEnum)
        {
            if (initEnum == null || initEnum.Count == 0)
            {
                // 如果没有指定初始化的属性，则不做初始化
                return;
            }
            
            foreach (var attributeEnum in initEnum)
            {
                if (!Attributes.ContainsKey(attributeEnum))
                {
                    InitializeAttribute(attributeEnum);
                }
            }
        }

        /// <summary>
        /// 初始化单个属性，在设置属性时，如果没有对应属性，则会初始化单个属性
        /// </summary>
        /// <param name="_attributeEnum"></param>
        private void InitializeAttribute(AttributeEnum _attributeEnum)
        {
            // 如果属性已存在则不重复初始化
            if (Attributes.ContainsKey(_attributeEnum))return;
            
            var attribute = new Attribute(_attributeEnum.ToString(), 0);
            // 订阅属性变化事件
            attribute.OnAttributeChanged += OnAttributeChanged;
            Attributes[_attributeEnum] = attribute;
        }
        
        #endregion

        #region 属性查询
        
        /// <summary>
        /// 获取指定属性，一般用来使用。
        /// 在使用该方法时，会自动初始化属性，不需要额外进行初始化
        /// </summary>
        /// <param name="attributeEnum">属性枚举</param>
        /// <returns>属性对象，保证返回非空</returns>
        public Attribute GetAttribute(AttributeEnum attributeEnum)
        {
            if(!Attributes.ContainsKey(attributeEnum))
            {
                // 如果属性不存在，则初始化该属性
                InitializeAttribute(attributeEnum);
            }
            return Attributes[attributeEnum];
        }

        /// <summary>
        /// 获取属性总数值
        /// </summary>
        /// <param name="attributeEnum">属性枚举</param>
        /// <returns>总数值</returns>
        public double GetTotalValue(AttributeEnum attributeEnum)
        {
            return Attributes[attributeEnum]?.TotalValue ?? 0;
        }

        /// <summary>
        /// 获取白字属性值
        /// </summary>
        /// <param name="attributeEnum">属性枚举</param>
        /// <returns>白字属性值</returns>
        public double GetBaseValue(AttributeEnum attributeEnum)
        {
            return Attributes[attributeEnum]?.BaseValue ?? 0;
        }

        /// <summary>
        /// 获取绿字属性值
        /// </summary>
        /// <param name="attributeEnum">属性枚举</param>
        /// <returns>绿字属性值</returns>
        public double GetBonusValue(AttributeEnum attributeEnum)
        {
            return Attributes[attributeEnum]?.BonusValue ?? 0;
        }
        
        #endregion

        #region 属性修改
        /// <summary>
        /// 设置属性基础值
        /// GetAttribute方法会自动初始化属性，不需要进行额外初始化。
        /// </summary>
        /// <param name="attributeEnum">属性枚举</param>
        /// <param name="value">基础值</param>
        public void SetBaseValue(AttributeEnum attributeEnum, double value)
        {
            var attribute = GetAttribute(attributeEnum);
            attribute.SetBaseValue(value);
        }

        /// <summary>
        /// 添加属性修饰器
        /// </summary>
        /// <param name="attributeEnum">属性枚举</param>
        /// <param name="modifier">修饰器</param>
        public void AddModifier(AttributeEnum attributeEnum, AttributeModifier modifier)
        {
            var attribute = GetAttribute(attributeEnum);
            attribute.AddModifier(modifier);
        }

        /// <summary>
        /// 移除属性修饰器
        /// </summary>
        /// <param name="attributeEnum">属性枚举</param>
        /// <param name="modifier">修饰器</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveModifier(AttributeEnum attributeEnum, AttributeModifier modifier)
        {
            var attribute = GetAttribute(attributeEnum);
            return attribute.RemoveModifier(modifier);
        }

        /// <summary>
        /// 清除指定属性的所有修饰器
        /// </summary>
        /// <param name="attributeEnum">属性枚举</param>
        public void ClearModifiers(AttributeEnum attributeEnum)
        {
            var attribute = GetAttribute(attributeEnum);
            attribute.ClearAllModifiers();
        }

        /// <summary>
        /// 清除所有属性的所有修饰器
        /// </summary>
        public void ClearAllModifiers()
        {
            foreach (var attribute in Attributes.Values)
            {
                attribute.ClearAllModifiers();
            }
        }
        #endregion

        #region 事件管理
        /// <summary>
        /// 设置是否启用事件
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetEventsEnabled(bool enabled)
        {
            _enableEvents = enabled;
            foreach (var attribute in Attributes.Values)
            {
                attribute.SetEventsEnabled(enabled);
            }
        }

        /// <summary>
        /// 设置是否启用自动更新
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetAutoUpdateEnabled(bool enabled)
        {
            foreach (var attribute in Attributes.Values)
            {
                attribute.SetAutoUpdateEnabled(enabled);
            }
        }

        /// <summary>
        /// 属性变化事件处理
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">事件参数</param>
        private void OnAttributeChanged(object sender, AttributeChangedEventArgs e)
        {
            if (_enableEvents)
            {
                OnAnyAttributeChanged?.Invoke(this, e);
            }
        }
        #endregion

        #region 性能优化
        /// <summary>
        /// 标记所有属性为脏
        /// </summary>
        public void MarkAllDirty()
        {
            foreach (var attribute in Attributes.Values)
            {
                attribute.MarkDirty();
            }
        }

        /// <summary>
        /// 更新所有属性
        /// </summary>
        public void UpdateAll()
        {
            foreach (var attribute in Attributes.Values)
            {
                attribute.Update();
            }
        }

        /// <summary>
        /// 更新指定属性
        /// </summary>
        /// <param name="attributeEnum">属性枚举</param>
        public void UpdateAttribute(AttributeEnum attributeEnum)
        {
            var attribute = GetAttribute(attributeEnum);
            if (attribute != null)
            {
                attribute.Update();
            }
        }
        #endregion

        #region 调试和日志
        /// <summary>
        /// 打印所有属性信息
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void PrintAllAttributes()
        {
            Log.Info("=== 所有属性信息 ===");
            foreach (var kvp in Attributes)
            {
                var attribute = kvp.Value;
                Log.Info($"{kvp.Key}: 白字{attribute.BaseValue:F2} + 绿字{attribute.BonusValue:F2} = 总计{attribute.TotalValue:F2}");
            }
        }

        /// <summary>
        /// 打印指定属性信息
        /// </summary>
        /// <param name="attributeEnum">属性枚举</param>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void PrintAttribute(AttributeEnum attributeEnum)
        {
            var attribute = GetAttribute(attributeEnum);
            if (attribute != null)
            {
                Log.Info($"=== {attributeEnum} 属性信息 ===");
                Log.Info($"白字: {attribute.BaseValue:F2}");
                Log.Info($"绿字: {attribute.BonusValue:F2}");
                Log.Info($"总计: {attribute.TotalValue:F2}");
            }
        }
        #endregion
    }

    
}