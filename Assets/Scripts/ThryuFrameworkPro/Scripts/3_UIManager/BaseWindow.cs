using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThryuFrameworkPro
{
    /// <summary>
    /// <para>Window（窗口）：一般的窗口，置于底层。</para>
    /// <para>Pop（弹窗）：高于窗口，一般用作弹窗</para>
    /// <para>Loading（加载）：高于弹窗，一般用作页面加载，此时几乎无法进行任何交互</para>
    /// <para>Max（最高）：仅特殊用途可以，有着最高的层级。</para>
    /// </summary>
    public enum WindowType
    {
        Window,
        Pop,
        Loading,
        Max,
    }

    /// <summary>
    /// <para>Hide(隐藏)：在执行close时，本质上是setActive(false)</para>
    /// <para>Destroy（删除）:在执行close时，本质上是destroy.</para>
    /// </summary>
    public enum WindowCloseType
    {
        Hide,
        Destroy,
    }

    public abstract class BaseWindow : MonoBehaviour
    {
        /// <summary>
        /// 窗口类型
        /// </summary>
        public WindowType windowType = WindowType.Window;

        /// <summary>
        /// 窗口关闭方式
        /// </summary>
        public WindowCloseType windowCloseType = WindowCloseType.Destroy;

        /// <summary>
        /// 关闭或销毁窗体，根据窗体的关闭方式决定
        /// </summary>
        public void CloseWindow()
        {
            OnClose();

            if (windowCloseType == WindowCloseType.Destroy)
            {
                Destroy(gameObject);
            }
            else
            {
                OnHide();
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 打开窗体
        /// </summary>
        public void OpenWindow()
        {
            gameObject.SetActive(true);
            OnOpen();
        }

        /// <summary>
        /// 页面打开时调用
        /// </summary>
        public abstract void OnOpen();

        /// <summary>
        /// 页面关闭前调用,这个是依靠uiManager进行销毁的结果
        /// </summary>
        public abstract void OnClose();

        /// <summary>
        /// 页面隐藏时调用
        /// </summary>
        public abstract void OnHide();


    }
}

