/* 
 * Copyright (C) 2018 Christoph Kutza
 * 
 * Please refer to the LICENSE file for license information
 */
using Byn.Common;
using Byn.Media;
using Byn.Net;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Byn.Unity.Examples
{
    /// <summary>
    /// USE AT YOUR OWN RISK!
    /// 
    /// This example shows how to use the IMediaNetwork to create video streaming apps
    /// that aren't just a simple 1 on 1 connection.
    /// 
    /// This instances of this class should be created multiple times in a single 
    /// scene to test connections locally.
    /// One app can be set via UnityEditor to uSender=true. It will then stream
    /// video to all other apps using the same address with uSender=false.
    /// You can duplicated multiple receives to test how many your system
    /// can handle. The system doesn't enforce a max number!
    /// 
    /// Note that the address is set to a fixed static value (sAddress) and 
    /// then shared with other local apps. This is done to avoid mistakenly 
    /// connect you to other user that test the same app at the same time. 
    /// Replace it with a fixed address to test with multiple apps.
    /// 
    /// This is just an example and not a multi purpose app! 
    /// </summary>
    public class OneToMany : MonoBehaviour
    {

        private string mSignalingServer = ExampleGlobals.Signaling;
        private string mStunServer = ExampleGlobals.StunUrl;

        /// <summary>
        /// Media interface. It handles the network connection
        /// + output of video frames. Very similar to ICall
        /// </summary>
        private IMediaNetwork mMediaNetwork = null;

        /// <summary>
        /// Media configuration. Will be set during setup.
        /// </summary>
        private MediaConfig mMediaConfig = new MediaConfig();

        /// <summary>
        /// Helper to keep to keep track of each instance
        /// </summary>
        private static int sInstances = 0;

        /// <summary>
        /// Helper to give each instance an id to print via log output
        /// </summary>
        private int mIndex = 0;

        /// <summary>
        /// If true this will create peers that send out video. False will
        /// not send anything.
        /// </summary>
        public bool uSender = false;

        /// <summary>
        /// Will be used to show the texture received (or sent)
        /// </summary>
        public RawImage uVideoOutput;

        /// <summary>
        /// Texture2D used as buffer for local or remote video
        /// </summary>
        private Texture2D mVideoTexture;

        /// <summary>
        /// Static address shared with all other local instances of this app
        /// to connect them.
        /// </summary>
        private static string sAddress = null;


        /// <summary>
        /// Can be used to keep track of each connection. 
        /// </summary>
        private List<ConnectionId> mConnectionIds = new List<ConnectionId>();

        /// <summary>
        /// Init process. Sets configuration and triggers the connection process
        /// </summary>
        /// <returns>
        /// Returns IEnumerator so unity treats it as a Coroutine
        /// </returns>
        private IEnumerator Start()
        {
            if (sAddress == null)
            {
                //to avoid the awkward moment of connecting two random users who test this package
                //we use a randomized addresses now to connect only the local test Apps  ;)
                sAddress = "OneToManyTest_" + Random.Range(0, 1000000);
            }


            if (UnityCallFactory.Instance == null)
            {
                Debug.LogError("No access to webrtc. ");
            }
            else
            {
                //Factory works. Prepare Peers
                NetworkConfig config = new NetworkConfig();
                config.IceServers.Add(new IceServer(mStunServer));
                config.SignalingUrl = mSignalingServer;
                mMediaNetwork = UnityCallFactory.Instance.CreateMediaNetwork(config);

                //keep track of multiple local instances for testing.
                mIndex = sInstances;
                sInstances++;
                Debug.Log("Instance " + mIndex + " created.");


                if (uSender)
                {
                    //sender will broadcast audio and video
                    mMediaConfig.Audio = true;
                    mMediaConfig.Video = true;

                    Debug.Log("Accepting incoming connections on " + sAddress);
                    mMediaNetwork.Configure(mMediaConfig);
                    mMediaNetwork.StartServer(sAddress);
                }
                else
                {
                    //this one will just receive (but could also send if needed)
                    mMediaConfig.Audio = false;
                    mMediaConfig.Video = false;
                    mMediaNetwork.Configure(mMediaConfig);
                }
                //wait a while before trying to connect othe sender
                //so it has time to register at the signaling server
                yield return new WaitForSeconds(5);
                if (uSender == false)
                {
                    Debug.Log("Tring to connect to " + sAddress);
                    mMediaNetwork.Connect(sAddress);
                }
            }
        }

        private void OnDestroy()
        {
            //Destroy the network
            if (mMediaNetwork != null)
            {
                mMediaNetwork.Dispose();
                mMediaNetwork = null;
                Debug.Log("Instance " + mIndex + " destroyed.");
            }
        }

        /// <summary>
        /// Prints log messages generated inside the libraries.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="tags"></param>
        private static void OnLog(object msg, string[] tags)
        {
            StringBuilder builder = new StringBuilder();
            bool warning = false;
            bool error = false;
            builder.Append("TAGS:[");
            foreach (var v in tags)
            {
                builder.Append(v);
                builder.Append(",");
                if (v == SLog.TAG_ERROR || v == SLog.TAG_EXCEPTION)
                {
                    error = true;
                }
                else if (v == SLog.TAG_WARNING)
                {
                    warning = true;
                }
            }
            builder.Append("]");
            builder.Append(msg);
            if (error)
            {
                Debug.LogError(builder.ToString());
            }
            else if (warning)
            {
                Debug.LogWarning(builder.ToString());
            }
            else
            {
                Debug.Log(builder.ToString());
            }
        }

        /// <summary>
        /// Keep updating the network and handle all events.
        /// </summary>
        void Update()
        {
            if (mMediaNetwork == null)
                return;
            mMediaNetwork.Update();

            //This is the event handler via polling.
            //This needs to be called or the memory will fill up with unhanded events!
            NetworkEvent evt;
            while (mMediaNetwork != null && mMediaNetwork.Dequeue(out evt))
            {
                HandleNetworkEvent(evt);
            }
            //polls for video updates
            HandleMediaEvents();

            //Flush will resync changes done in unity to the native implementation
            //(and possibly drop events that aren't handled in the future)
            if (mMediaNetwork != null)
                mMediaNetwork.Flush();
        }

        /// <summary>
        /// Handler polls the media network to check for new video frames.
        /// 
        /// </summary>
        protected virtual void HandleMediaEvents()
        {
            //just for debugging
            bool handleLocalFrames = true;
            bool handleRemoteFrames = true;

            if (mMediaNetwork != null && handleLocalFrames)
            {
                RawFrame localFrame = mMediaNetwork.TryGetFrame(ConnectionId.INVALID);
                if (localFrame != null)
                {
                    UpdateTexture(localFrame);

                }
            }
            if (mMediaNetwork != null && handleRemoteFrames)
            {
                //so far the loop shouldn't be needed. we only expect one
                foreach (var id in mConnectionIds)
                {
                    if (mMediaNetwork != null)
                    {
                        RawFrame remoteFrame = mMediaNetwork.TryGetFrame(id);
                        if (remoteFrame != null)
                        {
                            UpdateTexture(remoteFrame);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Log method to help seeing what each of the different apps does.
        /// </summary>
        /// <param name="txt"></param>
        private void Log(string txt)
        {
            Debug.Log("Instance " + mIndex + ": " + txt);
        }


        /// <summary>
        /// Method is called to handle the network events triggered by the internal media network and 
        /// trigger related event handlers for the call object.
        /// </summary>
        /// <param name="evt"></param>
        protected virtual void HandleNetworkEvent(NetworkEvent evt)
        {
            switch (evt.Type)
            {
                case NetEventType.NewConnection:

                    mConnectionIds.Add(evt.ConnectionId);
                    Log("New connection id " + evt.ConnectionId);

                    break;
                case NetEventType.ConnectionFailed:
                    //call failed
                    Log("Outgoing connection failed");
                    break;
                case NetEventType.Disconnected:

                    if (mConnectionIds.Contains(evt.ConnectionId))
                    {
                        mConnectionIds.Remove(evt.ConnectionId);

                        Log("Connection disconnected");
                    }
                    break;
                case NetEventType.ServerInitialized:
                    //incoming calls possible
                    Log("Server ready for incoming connections. Address: " + evt.Info);
                    break;
                case NetEventType.ServerInitFailed:
                    Log("Server init failed");
                    break;
                case NetEventType.ServerClosed:
                    Log("Server stopped");
                    break;
            }
        }


        /// <summary>
        /// Updates the ui with the new raw frame
        /// </summary>
        /// <param name="frame"></param>
        private void UpdateTexture(RawFrame frame)
        {
            if (uVideoOutput != null)
            {
                if (frame != null)
                {
                    UpdateTexture(ref mVideoTexture, frame);
                    uVideoOutput.texture = mVideoTexture;
                }
            }
        }

        /// <summary>
        /// Wrties the raw frame into the given texture or creates it if null or wrong width/height.
        /// </summary>
        /// <param name="tex"></param>
        /// <param name="frame"></param>
        /// <returns></returns>
        protected bool UpdateTexture(ref Texture2D tex, RawFrame frame)
        {
            bool newTextureCreated = false;
            //texture exists but has the wrong height /width? -> destroy it and set the value to null
            if (tex != null && (tex.width != frame.Width || tex.height != frame.Height))
            {
                Texture2D.Destroy(tex);
                tex = null;
            }
            //no texture? create a new one first
            if (tex == null)
            {
                newTextureCreated = true;
                Debug.Log("Creating new texture with resolution " + frame.Width + "x" + frame.Height + " Format:" + mMediaConfig.Format);
                if (mMediaConfig.Format == FramePixelFormat.ABGR)
                {
                    tex = new Texture2D(frame.Width, frame.Height, TextureFormat.RGBA32, false);
                }
                else
                {
                    //not yet properly supported.
                    tex = new Texture2D(frame.Width, frame.Height, TextureFormat.YUY2, false);
                }
                tex.wrapMode = TextureWrapMode.Clamp;
            }
            ///copy image data into the texture and apply
            tex.LoadRawTextureData(frame.Buffer);
            tex.Apply();
            return newTextureCreated;
        }
    }
}