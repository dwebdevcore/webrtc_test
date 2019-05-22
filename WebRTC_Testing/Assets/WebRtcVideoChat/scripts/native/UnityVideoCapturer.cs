#if !UNITY_WEBGL || UNITY_EDITOR
using System;
using System.Collections.Generic;
using WebRtcCSharp;
using UnityEngine;
using Byn.Common;

namespace Byn.Media.Native
{
    /// <summary>
    /// Use VideoInput if you want to send custom image data.
    /// 
    /// This class will be removed soon.
    /// </summary>
    class UnityVideoCapturer : HLCustomVideoCapturer
    {

        private bool mRunning = false;
        public bool Running
        {
            get
            {
                return mRunning;
            }

            set
            {
                mRunning = value;
            }
        }
        private int mRequestedWidth;
        private int mRequestedHeight;
        private int mRequestedFps = 30;
        private byte[] mImageBuffer = null;

        private string mName = null;

        /// <summary>
        /// true = pointers to unity memory are used ( can easily crash / be a security risk)
        /// false = using byte[] copy instead (slower)
        /// </summary>
        private bool mUseUnsafeMethod = true;


        private WebCamTexture mTexture = null;
        private Color32[] mBuffer = null;

        public UnityVideoCapturer(string name = null)
        {
            mName = name;
            SLog.L("UnityVideoCapturerer " + name + " created", UnityVideoCapturerFactory.LOGTAG);
        }

        //called by webrtc
        public override bool Start(VideoFormat capture_format)
        {
			UpdateBufferSize (capture_format.width, capture_format.height);
			UpdateFrame(VideoType.kARGB, mImageBuffer, (uint)mImageBuffer.Length, mRequestedWidth, mRequestedHeight, 0, false);
            mRequestedFps = capture_format.framerate();
            
            if (mRequestedFps < 1 || mRequestedFps > 60)
                mRequestedFps = 30;

            mRunning = true;
            SLog.L("UnityVideoCapturerer " + mName + " started", UnityVideoCapturerFactory.LOGTAG);
            return true;
        }

		private void UpdateBufferSize(int width, int height)
		{
			mRequestedWidth = width;
			mRequestedHeight = height;
			if(mImageBuffer == null || mImageBuffer.Length != (width * height * 4))
			{
				mImageBuffer = new byte[width * height * 4];
				for (int i = 0; i < mImageBuffer.Length; i++)
					mImageBuffer[i] = 255;
			}
		}

        //called by webrtc
        public override void Stop()
        {
            mRunning = false;
            SLog.L("UnityVideoCapturerer " + mName + " stopped", UnityVideoCapturerFactory.LOGTAG);
        }

        //
        private void SetupUnityCamera()
        {
			SLog.L("UnityVideoCapturerer " + mName + " unity setup " + mRequestedWidth + "x" + mRequestedHeight + " FPS: " + mRequestedFps, UnityVideoCapturerFactory.LOGTAG);
			mTexture = new WebCamTexture(mName, mRequestedWidth, mRequestedHeight, mRequestedFps);
            mTexture.Play();
        }


        //called in unity thread but will call to webrtc
        public void TriggerUpdate()
        {
            if (mRunning)
            {
                //we create during the first update to make sure everything using unity objects is done in the
                //main thread
                if (mTexture == null && mName != null)
                {
                    SetupUnityCamera();
                }

                if (mTexture == null)
                {
                    //test image.
                    DeliverTestImage();
                }
                else
                {
                    DeliverImage();
                }
            }
        }





        //called in unity thread but will itself call to webrtc (locking here could lead to a deadlock!)
        private void DeliverImage()
        {
            if (mTexture.didUpdateThisFrame)
            {
                mBuffer = mTexture.GetPixels32(mBuffer);

                int rotation = mTexture.videoRotationAngle;
                rotation = rotation % 360;

                int actualWidth = mTexture.width;
                int actualHeight = mTexture.height;


                //webrtc can only handle those exact rotations. everything else will crash
                if (rotation != 0 && rotation != 90 && rotation != 180 && rotation != 270)
                {
                    Debug.LogWarning("Received unexpected rotation value " + rotation + ". Setting to 0");
                    rotation = 0;
                }

                if (mUseUnsafeMethod)
                {
                    //the nasty way gets the ptr from Color32.
                    //If Unity ever changes the struct this will crash badly
                    UnityHelper.PtrFromColor32(mBuffer, (IntPtr address, uint bytelength) =>
                    {
                        //this method can easily crash unity. Make sure bytelength, width and height are 
                        //correct and match what is expected based on the video type
                        if(actualWidth * actualHeight * 4 >= bytelength)
                        {
                            UpdateFrame2(VideoType.kABGR, address, bytelength, actualWidth, actualHeight, rotation, true);
                        }
                        else
                        {
                            //This means there is a bug somewhere
                            Debug.LogError("Image size and type does not match the length of its memory!");
                        }
                    });
                }
                else
                {

                    UpdateBufferSize(mTexture.width, mTexture.height);
                    if (mBuffer.Length == mImageBuffer.Length / 4)
                    {
                        //this is where we loose the FPS
                        for (int i = 0; i < mBuffer.Length; i++)
                        {
                            mImageBuffer[i * 4 + 0] = mBuffer[i].b;
                            mImageBuffer[i * 4 + 1] = mBuffer[i].g;
                            mImageBuffer[i * 4 + 2] = mBuffer[i].r;
                            mImageBuffer[i * 4 + 3] = mBuffer[i].a;
                        }
                        //this method can easily crash unity. Make sure bytelength, width and height are 
                        //correct and match what is expected based on the video type
                        if (actualWidth * actualHeight * 4 >= mImageBuffer.Length)
                        {
                            UpdateFrame(VideoType.kARGB, mImageBuffer, (uint)mImageBuffer.Length, actualWidth, actualHeight, rotation, true);
                        }
                        else
                        {
                            //This means there is a bug somewhere
                            Debug.LogError("Image size and type does not match the length of its memory!");
                        }
                    }
                    else
                    {
                        Debug.LogError("Skipped frame. invalid buffer length: " + mImageBuffer.Length + " expected " + mBuffer.Length * 4);
                    }
                }
            }
            else
            {
                //skip frame
            }
        }

        private void DeliverTestImage()
        {
			for (int i = 0; i < mImageBuffer.Length; i++)
				mImageBuffer[i] = (byte)(mImageBuffer[i] + i);
            //UpdateFrame(VideoType.kARGB, arr, (uint)arr.Length, width, height);
        }

        public override void Dispose()
        {
            SLog.L("UnityVideoCapturerer disposing", UnityVideoCapturerFactory.LOGTAG);
            if (mTexture != null)
            {
                mTexture.Stop();
            }
            base.Dispose();
            SLog.L("UnityVideoCapturerer disposed", UnityVideoCapturerFactory.LOGTAG);
        }
    }

    /// <summary>
    /// Called by native code to list devices and create new video devices.
    /// </summary>
    class UnityVideoCapturerFactory : HLVideoCapturerFactory
    {
        public static readonly string LOGTAG = "UnityVideoCapturerFactory";
        public static List<UnityVideoCapturer> mActiveCapturers = new List<UnityVideoCapturer>();

        public static readonly bool sTestDeviceActive = false;
        public static readonly string sTestDeviceName = "CSharpTestDevice";

        public static readonly string sDevicePrefix = "Unity_";

        //used to make sure nothing is accessed by webrtc and unity at the same time. Also used for UnityVideoCapturer
        public static readonly object sUnityLock = new object();

        //called from native code
        public override HLCustomVideoCapturer Create(string deviceName)
        {
            try
            {
                if (sTestDeviceActive && deviceName == sTestDeviceName)
                {
                    var v = new UnityVideoCapturer();
                    lock (mActiveCapturers)
                    {
                        mActiveCapturers.Add(v);
                    }
                    return v;
                }
                foreach (var device in WebCamTexture.devices)
                {
                    if (deviceName == (sDevicePrefix + device.name))
                    {
                        Debug.Log("Creating new unity video caputurer: " + device.name);
                        var v = new UnityVideoCapturer(device.name);
                        if (v != null)
                        {
                            lock (mActiveCapturers)
                            {
                                mActiveCapturers.Add(v);
                            }
                            return v;
                        }
                    }
                }

            }
            catch (Exception e)
            {
                SLog.LogException(e, LOGTAG);
            }
            return null;
        }
        //called from native code
        public override StringVector GetVideoDevices()
        {
            StringVector vector = new StringVector();
            try
            {

                foreach (var v in WebCamTexture.devices)
                {
                    if (v.isFrontFacing)
                    {
                        string deviceName = sDevicePrefix + v.name;
                        vector.Add(deviceName);
                    }
                }
                foreach (var v in WebCamTexture.devices)
                {
                    if (v.isFrontFacing == false)
                    {
                        string deviceName = sDevicePrefix + v.name;
                        vector.Add(deviceName);
                    }
                }
                if(sTestDeviceActive)
                    vector.Add(sTestDeviceName);

            }
            catch (Exception e)
            {
                SLog.LogException(e, LOGTAG);
            }
            return vector;
        }

        public void Update()
        {
            UnityVideoCapturer[] capturers = null;
            lock (mActiveCapturers)
            {
                capturers = mActiveCapturers.ToArray();
            }
            foreach (var v in capturers)
            {
                if (v.Running)
                {
                    v.TriggerUpdate();
                }
                else
                {
                    lock (mActiveCapturers)
                    {
                        mActiveCapturers.Remove(v);
                    }
                    v.Dispose();
                }
            }

        }
    }

}

#endif