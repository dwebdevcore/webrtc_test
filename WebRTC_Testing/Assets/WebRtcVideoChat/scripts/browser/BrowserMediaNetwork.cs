#if UNITY_WEBGL
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using Byn.Media;
using Byn.Net;
using System;
using System.Text;
using Byn.Common;

namespace Byn.Media.Browser
{
    public class BrowserMediaNetwork : BrowserWebRtcNetwork, IMediaNetwork
    {

        #region CAPI imports
        [DllImport("__Internal")]
        public static extern bool UnityMediaNetwork_IsAvailable();

        [DllImport("__Internal")]
        public static extern int UnityMediaNetwork_Create(string lJsonConfiguration);

        [DllImport("__Internal")]
        public static extern void UnityMediaNetwork_Configure(int lIndex, bool audio, bool video,
            int minWidth, int minHeight, int maxWidth, int maxHeight, int idealWidth, int idealHeight);

        [DllImport("__Internal")]
        public static extern int UnityMediaNetwork_GetConfigurationState(int lIndex);


        [DllImport("__Internal")]
        public static extern string UnityMediaNetwork_GetConfigurationError(int lIndex);


        [DllImport("__Internal")]
        public static extern void UnityMediaNetwork_ResetConfiguration(int lIndex);


        [DllImport("__Internal")]
        public static extern bool UnityMediaNetwork_TryGetFrame(int lIndex, int connectionId, int[] lWidth, int[] lHeight, byte[] lBuffer, int offset, int length);


        [DllImport("__Internal")]
        public static extern int UnityMediaNetwork_TryGetFrameDataLength(int lIndex, int connectionId);


        [DllImport("__Internal")]
        public static extern void UnityMediaNetwork_SetVolume(int lIndex, double volume, int connectionId);

        [DllImport("__Internal")]
        public static extern bool UnityMediaNetwork_HasAudioTrack(int lIndex, int connectionId);

        [DllImport("__Internal")]
        public static extern bool UnityMediaNetwork_HasVideoTrack(int lIndex, int connectionId);

        #endregion


        public BrowserMediaNetwork(NetworkConfig lNetConfig)
        {
            if(lNetConfig.AllowRenegotiation)
            {
                SLog.LW("NetworkConfig.AllowRenegotiation is set to true. This is not supported in the browser version yet! Flag ignored.", this.GetType().Name);
            }
            string signalingUrl = lNetConfig.SignalingUrl;

            IceServer[] iceServers = null;
            if(lNetConfig.IceServers != null)
            {
                iceServers = lNetConfig.IceServers.ToArray();
            }


            //TODO: change this to avoid the use of json

            StringBuilder iceServersJson = new StringBuilder();
            BrowserWebRtcNetwork.IceServersToJson(iceServers, iceServersJson);
            /*
            Example:
            {"{IceServers":[{"urls":["turn:because-why-not.com:12779"],"username":"testuser13","credential":"testpassword"},{"urls":["stun:stun.l.google.com:19302"],"username":"","credential":""}], "SignalingUrl":"ws://because-why-not.com:12776/callapp", "IsConference":"False"}
             */

            string conf = "{\"IceServers\":" + iceServersJson.ToString() + ", \"SignalingUrl\":\"" + signalingUrl + "\", \"IsConference\":\"" + false + "\"}";
            SLog.L("Creating BrowserMediaNetwork config: " + conf, this.GetType().Name);
            mReference = UnityMediaNetwork_Create(conf);
        }

        public static new bool IsAvailable()
        {
            try
            {
                Debug.Log("Check availability via UnityMediaNetwork_IsAvailable");

                //js side will check if all needed functions are available and if the browser is supported
                return UnityMediaNetwork_IsAvailable();
            }
            catch (EntryPointNotFoundException)
            {
                //not available at all
                return false;
            }
        }
        private static bool sInjectionTried = false;
        static public new void InjectJsCode()
        {

            //use sInjectionTried to block multiple calls.
            if (Application.platform == RuntimePlatform.WebGLPlayer && sInjectionTried == false)
            {
                sInjectionTried = true;
                Debug.Log("injecting webrtcvideochatplugin");
                TextAsset txt = Resources.Load<TextAsset>("webrtcvideochatplugin");
                if (txt == null)
                {
                    Debug.LogError("Failed to find webrtcvideochatplugin.txt in Resource folder. Can't inject the JS plugin!");
                    return;
                }
                StringBuilder jsCode = new StringBuilder();
                jsCode.Append("console.log('Start eval webrtcnetworkplugin!');");
                jsCode.Append(txt.text);
                jsCode.Append("console.log('completed eval webrtcnetworkplugin!');");
                BrowserWebRtcNetwork.ExternalEval(jsCode.ToString());
            }
        }
        public void Configure(MediaConfig config)
        {
            UnityMediaNetwork_Configure(mReference,
                config.Audio, config.Video,
                config.MinWidth, config.MinHeight,
                config.MaxWidth, config.MaxHeight,
                config.IdealWidth, config.IdealHeight);
        }

        public RawFrame TryGetFrame(ConnectionId id)
        {
            int length = UnityMediaNetwork_TryGetFrameDataLength(mReference, id.id);
            if (length < 0)
                return null;

            int[] width = new int[1];
            int[] height = new int[1];
            byte[] buffer = new byte[length];

            bool res = UnityMediaNetwork_TryGetFrame(mReference, id.id, width, height, buffer, 0, buffer.Length);
            if (res)
                return new RawFrame(buffer, width[0], height[0]);
            return null;
        }

        public MediaConfigurationState GetConfigurationState()
        {
            int res = UnityMediaNetwork_GetConfigurationState(mReference);
            MediaConfigurationState state = (MediaConfigurationState)res;
            return state;
        }
        public override void Update()
        {
            base.Update();

        }
        public string GetConfigurationError()
        {
            if(GetConfigurationState() == MediaConfigurationState.Failed)
            {
                return "An error occurred while requesting Audio/Video features. Check the browser log for more details.";
            }else
            {
                return null;
            }

        }

        public void ResetConfiguration()
        {
            UnityMediaNetwork_ResetConfiguration(mReference);
        }

        public void SetVolume(double volume, ConnectionId remoteUserId)
        {
            UnityMediaNetwork_SetVolume(mReference, volume, remoteUserId.id);
        }

        public bool HasAudioTrack(ConnectionId remoteUserId)
        {
            return UnityMediaNetwork_HasAudioTrack(mReference, remoteUserId.id);
        }

        public bool HasVideoTrack(ConnectionId remoteUserId)
        {
            return UnityMediaNetwork_HasVideoTrack(mReference, remoteUserId.id);
        }
    }
}
#endif