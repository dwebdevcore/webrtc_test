using UnityEngine;
using Byn.Net;
using System.Collections.Generic;
using Byn.Media;
using System;
using Byn.Common;

namespace Byn.Media
{
    /// <summary>
    /// UnityCallFactory allows to create new ICall objects and will dispose them
    /// automatically when unity shuts down. 
    /// 
    /// </summary>
    public class UnityCallFactory : UnitySingleton<UnityCallFactory>, ICallFactory
    {
        private ICallFactory mFactory = null;
        /// <summary>
        /// Do not use. For debugging only.
        /// </summary>
        public ICallFactory InternalFactory
        {
            get
            {
                return mFactory;
            }
        }
        private bool mIsDisposed = false;

#if !UNITY_WEBGL || UNITY_EDITOR
        private Native.UnityVideoCapturerFactory mVideoFactory;
        public Native.NativeVideoInput VideoInput
        {
            get
            {
                if(mFactory != null)
                {
                    var factory = mFactory as Byn.Media.Native.NativeWebRtcCallFactory;
                    return factory.VideoInput;
                }
                return null;
            }
        }
#endif

        //android needs a static init process. 
        /// <summary>
        /// True if the platform specific init process was tried
        /// </summary>
        private static bool sStaticInitTried = false;

        /// <summary>
        /// true if the static init process was successful. false if not yet tried or failed.
        /// </summary>
        private static bool sStaticInitSuccessful = false;

        private void Awake()
        {
            //make sure the wrapper was initialized
            TryStaticInitialize();
            if (sStaticInitSuccessful == false)
            {
                Debug.LogError("Initialization of the webrtc plugin failed. StaticInitSuccessful is false. ");
                mFactory = null;
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
        
        mFactory = new Byn.Media.Browser.BrowserCallFactory();
#else

            try
            {

                Byn.Media.Native.NativeWebRtcCallFactory factory = new Byn.Media.Native.NativeWebRtcCallFactory();
                mFactory = factory;
				if (Application.platform == RuntimePlatform.Android 
                    //for testing only
                    //|| Application.platform == RuntimePlatform.OSXEditor 
                    //|| Application.platform == RuntimePlatform.WindowsEditor
                    )
                {
                    mVideoFactory = new Native.UnityVideoCapturerFactory();
                    factory.AddVideoCapturerFactory(mVideoFactory);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to create the call factory. This might be because a platform specific " +
                    " dll is missing or set to inactive in the unity editor.");
                Debug.LogException(e);
            }
#endif

        }
        public void Update()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
        //nothing to do here yet
#else
            if (mVideoFactory != null)
                mVideoFactory.Update();
#endif
        }
        public static void TryStaticInitialize()
        {
            //make sure it is called only once. no need for multiple static inits...
            if (sStaticInitTried)
                return;

            //this library builds on top of the network version -> make sure this one is initialized
            WebRtcNetworkFactory.TryStaticInitialize();
            if (WebRtcNetworkFactory.StaticInitSuccessful == false)
            {
                Debug.LogError("WebRtcNetwork failed to initialize. UnityCallFactory can't be used without WebRtcNetwork!");
                sStaticInitSuccessful = false;
                return;
            }


#if UNITY_WEBGL && !UNITY_EDITOR  //uncomment to be able to run in the editor using the native version

            //check if the java script part is available
            if (Byn.Media.Browser.BrowserMediaNetwork.IsAvailable() == false)
            {
                //js part is missing -> inject the code into the browser
                Byn.Media.Browser.BrowserMediaNetwork.InjectJsCode();
            }
            //if still not available something failed. setting sStaticInitSuccessful to false
            //will block the use of the factories
            sStaticInitSuccessful = Byn.Media.Browser.BrowserMediaNetwork.IsAvailable();
            if(sStaticInitSuccessful == false)
            {
                Debug.LogError("Failed to access the java script library. This might be because of browser incompatibility or a missing java script plugin!");
            }
#else
            sStaticInitSuccessful = true;
#endif
        }
        /// <summary>
        /// Creates a new ICall object.
        /// Only use this method to ensure that your software will keep working on other platforms supported in 
        /// future versions of this library.
        /// </summary>
        /// <param name="config">Network configuration</param>
        /// <returns></returns>
        public ICall Create(NetworkConfig config = null)
        {
            ICall call = mFactory.Create(config);
            if (call == null)
            {
                Debug.LogError("Creation of call object failed. Platform not supported? Platform specific dll not included?");
            }
            return call;
        }
        public IMediaNetwork CreateMediaNetwork(NetworkConfig config)
        {
            return mFactory.CreateMediaNetwork(config);
        }

        /// <summary>
        /// Returns a list containing the names of all available video devices. 
        /// 
        /// They can be used to select a certian device using the class
        /// MediaConfiguration and the method ICall.Configuration.
        /// </summary>
        /// <returns>Returns a list of video devices </returns>
        public string[] GetVideoDevices()
        {
            if (mFactory != null)
                return mFactory.GetVideoDevices();
            return new string[] { };
        }

        /// <summary>
        /// True if the video device can be chosen by the application. False if the environment (the browser usually)
        /// will automatically choose a suitable device.
        /// </summary>
        /// <returns></returns>
        public bool CanSelectVideoDevice()
        {
            if (mFactory != null)
            {
                return mFactory.CanSelectVideoDevice();
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Unity will call this during shutdown. It will make sure all ICall objects and the factory
        /// itself will be destroyed properly.
        /// </summary>
        protected override void OnDestroy()
        {
            Dispose();
            base.OnDestroy();
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!mIsDisposed)
            {
                if (disposing)
                {
                    Debug.Log("UnityCallFactory is being destroyed. All created calls will be destroyed as well!");
                    //cleanup
                    if (mFactory != null)
                    {
                        mFactory.Dispose();
                        mFactory = null;
                    }
                    Debug.Log("Network factory destroyed");
                }
                mIsDisposed = true;
            }
        }

        /// <summary>
        /// Destroys the factory.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }



        /// <summary>
        /// Mobile native only:
        /// Turns on/off the phones speaker
        /// </summary>
        /// <param name="state"></param>
        public void SetLoudspeakerStatus(bool state)
        {
#if UNITY_IOS
            var factory = InternalFactory as Native.NativeWebRtcCallFactory;
            if (factory != null)
            {
                factory.SetLoudspeakerStatus(state);
            }
#elif UNITY_ANDROID
            //android will just crash if SetLoudspeakerStatus is used
            //workaround via java
            Byn.Media.Android.AndroidHelper.SetSpeakerOn(state);
#else

            Debug.LogError("GetLoudspeakerStatus is only supported on mobile platforms.");
#endif
        }
        /// <summary>
        /// Checks if the phones speaker is turned on. Only for mobile native platforms
        /// </summary>
        /// <returns></returns>
        public bool GetLoudspeakerStatus()
        {
#if UNITY_IOS
            var factory = InternalFactory as Native.NativeWebRtcCallFactory;
            if (factory != null)
            {
                return factory.GetLoudspeakerStatus();
            }
			return false;
#elif UNITY_ANDROID
            //android will just crash if GetLoudspeakerStatus is used
            //workaround via java
            return Byn.Media.Android.AndroidHelper.IsSpeakerOn();
#else
            Debug.LogError("GetLoudspeakerStatus is only supported on mobile platforms.");
            return false;
#endif
        }
    }
}