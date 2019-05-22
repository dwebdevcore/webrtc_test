using UnityEngine;
using System.Collections;

namespace Byn.Media.Android
{
    /// <summary>
    /// Provides a range of static functions that help dealing with android specific issues.
    /// Main issue is that android treats the WebRTC audio as a voice cal. There are multiple problems
    /// because of this:
    /// 
    /// * the volume isn't set via "Media" but independently via the "Call volume" slider
    ///     (and unity blocks access to this slider)
    /// 
    /// * The volume is optimized for headphones or the users holding the phone directly onto their ears.
    /// -> Use SetSpeakerOn(true) to turn on the phones speaker for increased volume without headsets
    /// 
    /// 
    /// </summary>
    public class AndroidHelper
    {

        /// <summary>
        /// True switches the phone in speaker mode. This will also happen if headphones are connected.
        /// 
        /// This heavily increases the volume of the webrtc audio but often reduces the quality.
        /// 
        /// WARNING: This value is persistent even after restarting the app!!!
        /// </summary>
        /// <param name="value"></param>
        public static void SetSpeakerOn(bool value)
        {
            AndroidJavaObject audioManager = GetAudioManager();
            audioManager.Call("setSpeakerphoneOn", value);
        }

        /// <summary>
        /// Allows to check if the speakers are currently on.
        /// </summary>
        /// <returns></returns>
        public static bool IsSpeakerOn()
        {
            AndroidJavaObject audioManager = GetAudioManager();
            return audioManager.Call<bool>("isSpeakerphoneOn");
        }


        /// <summary>
        /// Wrapper for AndroidManager.getMode
        /// </summary>
        /// <returns></returns>
        public static int GetMode()
        {
            AndroidJavaObject audioManager = GetAudioManager();
            return audioManager.Call<int>("getMode");
        }

        /// <summary>
        /// Wrapper for AndroidManager.setMode
        /// </summary>
        /// <param name="mode"></param>
        public static void SetMode(int mode)
        {
            AndroidJavaObject audioManager = GetAudioManager();
            audioManager.Call("setMode", mode);
        }

        /// <summary>
        /// Checks if the current mode is set to InCommunication
        /// </summary>
        /// <returns></returns>
        public static bool IsModeInCommunication()
        {
            return GetMode() == GetAudioManagerFlag("MODE_IN_COMMUNICATION");
        }

        /// <summary>
        /// Switches Android to In-Communcation. 
        /// 
        /// This will show the Call Volume bar if the volume buttons are used
        /// </summary>
        public static void SetModeInCommunicaion()
        {
            AndroidJavaObject audioManager = GetAudioManager();

            Debug.Log("mode before: " + audioManager.Call<int>("getMode"));
            SetMode(GetAudioManagerFlag("MODE_IN_COMMUNICATION"));
            Debug.Log("mode after: " + audioManager.Call<int>("getMode"));
        }

        /// <summary>
        /// Default mode in android applications. Call volume bar shouldn't be visible
        /// </summary>
        public static void SetModeNormal()
        {
            AndroidJavaObject audioManager = GetAudioManager();

            Debug.Log("mode before: " + audioManager.Call<int>("getMode"));
            SetMode(GetAudioManagerFlag("MODE_NORMAL"));
            Debug.Log("mode after: " + audioManager.Call<int>("getMode"));
        }

        /// <summary>
        /// Returns the volume level of the voice call / webrtc stream
        /// 
        /// </summary>
        /// <returns></returns>
        public static int GetStreamVolume()
        {
            AndroidJavaObject audioManager = GetAudioManager();
            return audioManager.Call<int>("getStreamVolume", GetAudioManagerFlag("STREAM_VOICE_CALL"));
        }

        /// <summary>
        /// Sets the volume level for the webrtc call / stream
        /// </summary>
        /// <param name="volume"></param>
        public static void SetStreamVolume(int volume)
        {
            AndroidJavaObject audioManager = GetAudioManager();
            audioManager.Call("setStreamVolume",
                GetAudioManagerFlag("STREAM_VOICE_CALL"), volume, GetAudioManagerFlag("FLAG_SHOW_UI") | GetAudioManagerFlag("FLAG_PLAY_SOUND"));
        }


        /// <summary>
        /// Doesn't seem to work yet.
        /// </summary>
        /// <param name="isMute"></param>
        public static void SetMute(bool isMute)
        {
            AndroidJavaObject audioManager = GetAudioManager();

            audioManager.Call("setStreamMute",
                    GetAudioManagerFlag("STREAM_VOICE_CALL"), isMute);
        }
        public static bool IsMute()
        {
            AndroidJavaObject audioManager = GetAudioManager();

            return audioManager.Call<bool>("isStreamMute",
                    GetAudioManagerFlag("STREAM_VOICE_CALL"));
        }

        private static AndroidJavaObject GetAudioManager()
        {
            AndroidJavaObject activity = GetActivity();
            AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");

            AndroidJavaClass contextClass = new AndroidJavaClass("android.content.Context");
            AndroidJavaObject contextClass_AUDIO_SERVICE = contextClass.GetStatic<AndroidJavaObject>("AUDIO_SERVICE");

            AndroidJavaObject audioManager = context.Call<AndroidJavaObject>("getSystemService", contextClass_AUDIO_SERVICE);
            return audioManager;
        }

        private static AndroidJavaObject GetActivity()
        {
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
            return activity;
        }

        private static int GetAudioManagerFlag(string flag)
        {
            AndroidJavaClass audioManagerClass = new AndroidJavaClass("android.media.AudioManager");
            return audioManagerClass.GetStatic<int>(flag);
        }
    }
}
