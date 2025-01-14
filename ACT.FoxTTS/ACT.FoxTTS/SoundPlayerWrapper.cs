﻿using System;
using ACT.FoxCommon;
using ACT.FoxCommon.logging;
using ACT.FoxTTS.localization;
using ACT.FoxTTS.playback;
using Advanced_Combat_Tracker;

namespace ACT.FoxTTS
{
    public class SoundPlayerWrapper : IPluginComponent
    {
        private FoxTTSPlugin _plugin;

        private WMMPlayback _wmm = new WMMPlayback();

        public void AttachToAct(FoxTTSPlugin plugin)
        {
            _plugin = plugin;
            _wmm.AttachToAct(plugin);
        }


        public void PostAttachToAct(FoxTTSPlugin plugin)
        {
            _wmm.PostAttachToAct(plugin);
        }

        public void Stop()
        {

        }

        public void Play(string waveFile, dynamic playDevice, bool isSync, float? volume)
        {
            PlaybackSettings settings = _plugin.Settings.PlaybackSettings;
            switch (settings.Method)
            {
                case PlaybackMethod.Yukkuri:
                    _plugin.TtsInjector.PlayTTSYukkuri(waveFile, playDevice, isSync, volume);
                    break;
                case PlaybackMethod.Act:
                    // Play sound with ACT's sound API
                    ActWmpPlay(waveFile, settings.MasterVolume);
                    break;
                case PlaybackMethod.BuiltIn:
                    // Use built-in api to play sounds
                    // atm we support WMM only
                    // And WMM needs to be called in main thread
                    ActGlobals.oFormActMain.SafeInvoke(new Action(() => _wmm.PlaySound(waveFile)));
                    break;
            }
        }

        /// <summary>
        /// Play sound with ACT's WMP wrapper API
        /// </summary>
        private void ActWmpPlay(string WavFilePath, int VolumePercent)
        {
            try
            {
                ActGlobals.oFormActMain.PlaySoundWmpApi(WavFilePath, VolumePercent);
            }
            catch (Exception e)
            {
                if (e.ToString().Contains("6BF52A52-394A-11D3-B153-00C04F79FAA6"))
                {
                    // WMP unavailable
                    Logger.Error(strings.msgErrorWMPUnavailable);
                    Logger.Debug("Detailed exception:", e);

                    ActGlobals.oFormActMain.SafeInvoke(new Action(() =>
                    {
                        // Show notification
                        var ts = new TraySlider
                        {
                            ButtonLayout = TraySlider.ButtonLayoutEnum.OneButton,
                        };
                        ts.ShowTraySlider(strings.msgErrorWMPUnavailable, strings.actPanelTitle);

                        // Automatically switch to WinMM
                        _plugin.SettingsTab.SwitchToWinMMPlayback();

                        // And retry this request using WinMM
                        _wmm.PlaySound(WavFilePath);
                    }));
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
