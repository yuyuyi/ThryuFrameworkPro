using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* 基础框架版本更新说明：
 * 2025/6/30 羽于翼：初始版本
 *
 */
namespace ThryuFrameworkPro
{
    /// <summary>
    /// 音效管理器
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        #region 字段
        /// <summary>
        /// 背景音乐列表
        /// </summary>
        [Header("背景音乐配置")]
        [SerializeField] private List<AudioClip> bgmList = new List<AudioClip>();
        
        /// <summary>
        /// 默认UI音效
        /// </summary>
        [Header("UI音效配置")]
        [SerializeField] private AudioClip defaultUIClickSound;
        
        /// <summary>
        /// 背景音乐AudioSource
        /// </summary>
        [SerializeField] private AudioSource bgmSource;
        
        /// <summary>
        /// 特殊背景音乐AudioSource（用于Boss战等）
        /// </summary>
        [SerializeField] private AudioSource specialBgmSource;
        
        /// <summary>
        /// 战斗音效AudioSource
        /// </summary>
        [SerializeField] private AudioSource fightAudioSource;
        
        /// <summary>
        /// UI音效AudioSource
        /// </summary>
        [SerializeField] private AudioSource UIAudioSource;

        /// <summary>
        /// 当前背景音乐索引
        /// </summary>
        private int currentBgmIndex = 0;
        
        /// <summary>
        /// 是否正在播放特殊背景音乐
        /// </summary>
        private bool isPlayingSpecialBgm = false;
        
        #endregion

        #region 属性
        /// <summary>
        /// 总音量
        /// </summary>
        public float MasterVolume { get; private set; } = 1f;
        
        /// <summary>
        /// 背景音乐音量
        /// </summary>
        public float BgmVolume { get; private set; } = 0.8f;
        
        /// <summary>
        /// UI音效音量
        /// </summary>
        public float UIVolume { get; private set; } = 1f;
        
        /// <summary>
        /// 战斗音效音量
        /// </summary>
        public float FightVolume { get; private set; } = 1f;
        
        /// <summary>
        /// 是否静音
        /// </summary>
        public bool IsMuted { get; private set; } = false;
        #endregion

        #region 初始化
        protected override void OnSingletonAwake()
        {
            base.OnSingletonAwake();
            // 加载音量设置
            LoadVolumeSettings();
            
            // 开始播放背景音乐
            if (bgmList.Count > 0)
            {
                PlayBGM(0);
            }
        }


        /// <summary>
        /// 加载音量设置
        /// </summary>
        private void LoadVolumeSettings()
        {
            MasterVolume = PlayerPrefs.GetFloat("Audio_MasterVolume", 1f);
            BgmVolume = PlayerPrefs.GetFloat("Audio_BgmVolume", 0.8f);
            UIVolume = PlayerPrefs.GetFloat("Audio_UIVolume", 1f);
            FightVolume = PlayerPrefs.GetFloat("Audio_FightVolume", 1f);
            IsMuted = PlayerPrefs.GetInt("Audio_IsMuted", 0) == 1;
            
            UpdateAllVolumes();
        }
        #endregion

        #region 背景音乐管理
        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="index">音乐索引</param>
        public void PlayBGM(int index)
        {
            if (index < 0 || index >= bgmList.Count)
            {
                Log.Warning($"[AudioManager] 背景音乐索引越界: {index}");
                return;
            }
            
            currentBgmIndex = index;
            var clip = bgmList[index];
            
            if (bgmSource.clip != clip)
            {
                bgmSource.clip = clip;
                bgmSource.Play();
            }
        }

        /// <summary>
        /// 播放下一首背景音乐
        /// </summary>
        public void PlayNextBGM()
        {
            int nextIndex = (currentBgmIndex + 1) % bgmList.Count;
            PlayBGM(nextIndex);
        }

        /// <summary>
        /// 播放上一首背景音乐
        /// </summary>
        public void PlayPreviousBGM()
        {
            int prevIndex = (currentBgmIndex - 1 + bgmList.Count) % bgmList.Count;
            PlayBGM(prevIndex);
        }

        /// <summary>
        /// 播放特殊背景音乐（如Boss战音乐）
        /// </summary>
        /// <param name="clip">特殊背景音乐</param>
        public void PlaySpecialBGM(AudioClip clip)
        {
            if (clip == null) return;
            
            // 暂停普通背景音乐
            bgmSource.Pause();
            
            // 播放特殊背景音乐
            specialBgmSource.clip = clip;
            specialBgmSource.Play();
            isPlayingSpecialBgm = true;
            
            UpdateBGMVolume();
        }

        /// <summary>
        /// 结束特殊背景音乐
        /// </summary>
        public void StopSpecialBGM()
        {
            specialBgmSource.Stop();
            specialBgmSource.clip = null;
            isPlayingSpecialBgm = false;
            
            // 恢复普通背景音乐
            bgmSource.UnPause();
            
            UpdateBGMVolume();
        }

        /// <summary>
        /// 暂停背景音乐
        /// </summary>
        public void PauseBGM()
        {
            bgmSource.Pause();
            if (isPlayingSpecialBgm)
            {
                specialBgmSource.Pause();
            }
        }

        /// <summary>
        /// 恢复背景音乐
        /// </summary>
        public void ResumeBGM()
        {
            bgmSource.UnPause();
            if (isPlayingSpecialBgm)
            {
                specialBgmSource.UnPause();
            }
        }

        /// <summary>
        /// 停止背景音乐
        /// </summary>
        public void StopBGM()
        {
            bgmSource.Stop();
            specialBgmSource.Stop();
            isPlayingSpecialBgm = false;
        }

        /// <summary>
        /// 添加背景音乐
        /// </summary>
        /// <param name="clip">音乐片段</param>
        public void AddBGM(AudioClip clip)
        {
            if (clip == null) return;
            
            bgmList.Add(clip);
            
            // 如果是第一首音乐，自动播放
            if (bgmList.Count == 1)
            {
                PlayBGM(0);
            }
        }

        /// <summary>
        /// 移除背景音乐
        /// </summary>
        /// <param name="clip">音乐片段</param>
        public void RemoveBGM(AudioClip clip)
        {
            int index = bgmList.IndexOf(clip);
            if (index >= 0)
            {
                bgmList.RemoveAt(index);
                
                // 如果删除的是当前播放的音乐
                if (index == currentBgmIndex)
                {
                    if (bgmList.Count > 0)
                    {
                        PlayBGM(0);
                    }
                    else
                    {
                        StopBGM();
                    }
                }
                else if (index < currentBgmIndex)
                {
                    currentBgmIndex--;
                }
            }
        }

        /// <summary>
        /// 清空背景音乐列表
        /// </summary>
        public void ClearBGM()
        {
            bgmList.Clear();
            StopBGM();
            currentBgmIndex = 0;
        }

        /// <summary>
        /// 刷新所有背景音乐
        /// </summary>
        /// <param name="clips">新的背景音乐数组</param>
        public void RefreshBGM(AudioClip[] clips)
        {
            bgmList.Clear();
            currentBgmIndex = 0;
            
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                {
                    AddBGM(clips[i]);
                }
            }
        }
        #endregion

        #region 音效播放

        /// <summary>
        /// 播放UI音效
        /// </summary>
        /// <param name="clip">音效片段</param>
        /// <param name="volume">音量（可选）</param>
        public void PlayUISound(AudioClip clip)
        {
            if (clip == null) return;
            
            UIAudioSource.PlayOneShot(clip);
        }

        /// <summary>
        /// 播放战斗音效
        /// </summary>
        /// <param name="clip">音效片段</param>
        /// <param name="volume">音量（可选）</param>
        public void PlayFightSound(AudioClip clip)
        {
            if (clip == null) return;
            
            fightAudioSource.PlayOneShot(clip);
        }

        /// <summary>
        /// 播放默认UI点击音效
        /// </summary>
        public void PlayUIClick()
        {
            if (defaultUIClickSound != null)
            {
                PlayUISound(defaultUIClickSound);
            }
        }
        #endregion
        

        #region 音量控制
        /// <summary>
        /// 设置总音量
        /// </summary>
        /// <param name="volume">音量 (0-1)</param>
        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("Audio_MasterVolume", MasterVolume);
            UpdateAllVolumes();
        }

        /// <summary>
        /// 设置背景音乐音量
        /// </summary>
        /// <param name="volume">音量 (0-1)</param>
        public void SetBGMVolume(float volume)
        {
            BgmVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("Audio_BgmVolume", BgmVolume);
            UpdateBGMVolume();
        }

        /// <summary>
        /// 设置UI音效音量
        /// </summary>
        /// <param name="volume">音量 (0-1)</param>
        public void SetUIVolume(float volume)
        {
            UIVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("Audio_UIVolume", UIVolume);
            UpdateUIVolume();
        }

        /// <summary>
        /// 设置战斗音效音量
        /// </summary>
        /// <param name="volume">音量 (0-1)</param>
        public void SetFightVolume(float volume)
        {
            FightVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("Audio_FightVolume", FightVolume);
            UpdateFightVolume();
        }

        /// <summary>
        /// 设置静音状态
        /// </summary>
        /// <param name="muted">是否静音</param>
        public void SetMuted(bool muted)
        {
            IsMuted = muted;
            PlayerPrefs.SetInt("Audio_IsMuted", muted ? 1 : 0);
            UpdateAllVolumes();
        }

        /// <summary>
        /// 切换静音状态
        /// </summary>
        public void ToggleMute()
        {
            SetMuted(!IsMuted);
        }

        /// <summary>
        /// 更新所有音量
        /// </summary>
        private void UpdateAllVolumes()
        {
            UpdateBGMVolume();
            UpdateUIVolume();
            UpdateFightVolume();
        }

        /// <summary>
        /// 更新背景音乐音量
        /// </summary>
        private void UpdateBGMVolume()
        {
            float volume = BgmVolume * MasterVolume * (IsMuted ? 0 : 1);
            bgmSource.volume = volume;
            specialBgmSource.volume = volume;
        }

        /// <summary>
        /// 更新UI音效音量
        /// </summary>
        private void UpdateUIVolume()
        {
            UIAudioSource.volume = UIVolume * MasterVolume * (IsMuted ? 0 : 1);
        }

        /// <summary>
        /// 更新战斗音效音量
        /// </summary>
        private void UpdateFightVolume()
        {
            fightAudioSource.volume = FightVolume * MasterVolume * (IsMuted ? 0 : 1);
        }
        #endregion
        

        #region 生命周期
        protected override void OnSingletonDestroy()
        {
            base.OnSingletonDestroy();
            
            // 保存音量设置
            PlayerPrefs.Save();
        }
        #endregion
    }
}
