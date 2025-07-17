using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace ThryuFrameworkPro
{
    public class UIManager : Singleton<UIManager>
    {
        /// <summary>
        /// baseWindow的路径字典
        /// </summary>
        [ShowInInspector]
        private Dictionary<EWindowId, GameObject> windowPath = new Dictionary<EWindowId, GameObject>();


        /// <summary>
        /// 已经打开的baseWindow字典
        /// </summary>
        [ShowInInspector]
        private Dictionary<EWindowId, BaseWindow> windowOpenDict = new Dictionary<EWindowId, BaseWindow>();

        [Header("各层Transform")]
        [Tooltip("用于存放各层的父transform")]
        public Transform[] windowTrans;

        [ShowInInspector]
        public Canvas canvas;

        private int windowNum = 0;
        private int nowLoadWindow = 0;
        

        /// <summary>
        /// 注册窗体
        /// </summary>
        /// <param name="id"></param>
        /// <param name="win"></param>
        async Task RegisterWindow(EWindowId id, string _path)
        {
            //windowPath[id] = _path;
            //加载一遍
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(_path);
            await handle.Task;
            windowPath[id] = handle.Result;
            nowLoadWindow++;
        }

        /// <summary>
        /// 获取窗口,当窗口未打开时返回null
        /// </summary>
        /// <param name="_id"></param>
        public BaseWindow GetWindow(EWindowId _id)
        {
            BaseWindow window = null;
            if (windowOpenDict.TryGetValue(_id, out window))
            {
                return window;
            }
            else
                return null;
        }

        public T GetWindow<T>(EWindowId _id) where T : BaseWindow
        {
            BaseWindow window = null;
            if (windowOpenDict.TryGetValue(_id, out window))
            {
                return window as T;
            }
            else
                return null;
        }

        /// <summary>
        /// 获取窗口active状态
        /// </summary>
        /// <returns></returns>
        public bool GetWindowActive(EWindowId _id)
        {
            BaseWindow window = null;
            if (windowOpenDict.TryGetValue(_id, out window))
            {
                return window.gameObject.activeSelf;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// 关闭窗体，但是根据选中的方式关闭
        /// </summary>
        /// <param name="windowID"></param>
        public void CloseWindow(EWindowId _id)
        {
            BaseWindow window = null;
            if (!windowOpenDict.TryGetValue(_id, out window))
            {
                return;
            }
            else
            {
                if (window.windowCloseType == WindowCloseType.Destroy)
                    windowOpenDict.Remove(_id);
                window.CloseWindow();
                return;
            }


        }

        /// <summary>
        /// 打开窗体
        /// </summary>
        /// <param name="_id"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T OpenWindow<T>(EWindowId _id) where T : BaseWindow
        {
            BaseWindow window = null;
            
            if (!windowPath.ContainsKey(_id))
            {
                return null;
            }

            if (windowOpenDict.TryGetValue(_id, out window))
            {
                window.OpenWindow();
                window.transform.SetAsLastSibling();
                return window as T;
            }


            GameObject res = Instantiate(windowPath[_id], windowTrans[0]);
            window = res.GetComponent<BaseWindow>();
            window.OpenWindow();
            window.transform.SetParent(windowTrans[(int)window.windowType]);
            window.transform.SetAsLastSibling();
            windowOpenDict[_id] = window;
            
            return window as T;
        }
        
        /// <summary>
        /// 打开窗体
        /// </summary>
        /// <param name="_id"></param>
        public void OpenWindow(EWindowId _id)
        {
            BaseWindow window = null;
            
            if (!windowPath.ContainsKey(_id))
            {
                return;
            }

            if (windowOpenDict.TryGetValue(_id, out window))
            {
                window.OpenWindow();
                window.transform.SetAsLastSibling();
                return;
            }


            GameObject res = Instantiate(windowPath[_id], windowTrans[0]);
            window = res.GetComponent<BaseWindow>();
            window.OpenWindow();
            window.transform.SetParent(windowTrans[(int)window.windowType]);
            window.transform.SetAsLastSibling();
            windowOpenDict[_id] = window;
            
            return;
        }
        
        /// <summary>
        /// 打开窗体，带参数的回调初始化
        /// </summary>
        /// <param name="_id"></param>
        public void OpenWindow(EWindowId _id, Action callback)
        {
            OpenWindow(_id);
            callback?.Invoke();
            return;

        }

        
        
        /// <summary>
        /// 注册所有窗体
        /// </summary>
        public async void RegisterAllWindow(Action _onComplete)
        {
            windowNum = Enum.GetNames(typeof(EWindowId)).Length;

            //await RegisterWindow(EWindowId.LoginPanel, "LoginPanel");
            //await RegisterWindow(EWindowId.MainPanel, "MainPanel");
            
            
            
            _onComplete.Invoke();
        }
    }

    public enum EWindowId
    {
        LoginPanel = 0, //开始页面

        MainPanel = 100,  //主界面
        
    }
}
