/* 
 * Copyright (C) 2015 Christoph Kutza
 * 
 * Please refer to the LICENSE file for license information
 */
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Byn.Media;

/// <summary>
/// This class + prefab is a complete app allowing to call another app using a shared text or password
/// to meet online.
/// 
/// It supports Audio, Video and Text chat. Audio / Video can optionally turned on/off via toggles.
/// 
/// After the join button is pressed the (first) app will initialize a native webrtc plugin 
/// and contact a server to wait for incoming connections under the given string.
/// 
/// Another instance of the app can connect using the same string. (It will first try to
/// wait for incoming connections which will fail as another app is already waiting and after
/// that it will connect to the other side)
/// 
/// The important methods are "Setup" to initialize the call class (after join button is pressed) and
/// "Call_CallEvent" which reacts to events triggered by the class.
/// 
/// Also make sure to use your own servers for production (uSignalingUrl and uStunServer).
/// 
/// NOTE: Currently, only 1 to 1 connections are supported. This will change in the future.
/// </summary>
public class CallAppUi : MonoBehaviour
{

    /// <summary>
    /// Texture of the local video
    /// </summary>
    protected Texture2D mLocalVideoTexture = null;

    /// <summary>
    /// Texture of the remote video
    /// </summary>
    protected Texture2D mRemoteVideoTexture = null;


    [Header("Setup panel")]
    /// <summary>
    /// Panel with the join button. Will be hidden after setup
    /// </summary>
    public RectTransform uSetupPanel;
    public RectTransform uMainSetupPanel;

    /// <summary>
    /// Input field used to enter the room name.
    /// </summary>
    public InputField uRoomNameInputField;
    /// <summary>
    /// Join button to connect to a server.
    /// </summary>
    public Button uJoinButton;

    public Toggle uAudioToggle;
    public Toggle uVideoToggle;
    public Dropdown uVideoDropdown;


    [Header("Settings panel")]
    public RectTransform uSettingsPanel;
    public InputField uIdealWidth;
    public InputField uIdealHeight;
    public InputField uIdealFps;
    public Toggle uRejoinToggle;
    public Toggle uLocalVideoToggle;


    [Header("Video and Chat panel")]
    public RectTransform uInCallBase;
    public RectTransform uVideoPanel;
    public RectTransform uChatPanel;
    public RectTransform uVideoOverlay;

    [Header("Default positions/transformations")]
    public RectTransform uVideoBase;
    public RectTransform uChatBase;


    [Header("Fullscreen positions/transformations")]
    public RectTransform uFullscreenPanel;
    public RectTransform uVideoBaseFullscreen;
    public RectTransform uChatBaseFullscreen;




    [Header("Chat panel elements")]
    /// <summary>
    /// Input field to enter a new message.
    /// </summary>
    public InputField uMessageInputField;

    /// <summary>
    /// Output message list to show incoming and sent messages + output messages of the
    /// system itself.
    /// </summary>
    public MessageList uMessageOutput;


    /// <summary>
    /// Send button.
    /// </summary>
    public Button uSendMessageButton;

    /// <summary>
    /// Shutdown button. Disconnects all connections + shuts down the server if started.
    /// </summary>
    public Button uShutdownButton;

    /// <summary>
    /// Button to switch the loudspeakers on / off. Only for mobile visible.
    /// </summary>
    public Button uLoudspeakerButton;

    /// <summary>
    /// Slider to just the remote users volume.
    /// </summary>
    public Slider uVolumeSlider;

    /// <summary>
    /// Slider to just the remote users volume.
    /// </summary>
    public Text uOverlayInfo;


    [Header("Video panel elements")]
    /// <summary>
    /// Image of the local camera
    /// </summary>
    public RawImage uLocalVideoImage;

    /// <summary>
    /// Image of the remote camera
    /// </summary>
    public RawImage uRemoteVideoImage;


    [Header("Resources")]
    public Texture2D uNoCameraTexture;

    protected bool mFullscreen = false;


    protected CallApp mApp;



    private float mVideoOverlayTimeout = 0;
    private static readonly float sDefaultOverlayTimeout = 8;

    private bool mHasLocalVideo = false;
    private int mLocalVideoWidth = -1;
    private int mLocalVideoHeight = -1;
    private int mLocalFps = 0;
    private int mLocalFrameCounter = 0;
    private FramePixelFormat mLocalVideoFormat = FramePixelFormat.Invalid;

    private bool mHasRemoteVideo = false;
    private int mRemoteVideoWidth = -1;
    private int mRemoteVideoHeight = -1;
    private int mRemoteFps = 0;
    private int mRemoteFrameCounter = 0;
    private FramePixelFormat mRemoteVideoFormat = FramePixelFormat.Invalid;

    private float mFpsTimer = 0;

    private string mPrefix = "CallAppUI_";
    private static readonly string PREF_AUDIO = "audio";
    private static readonly string PREF_VIDEO = "video";
    private static readonly string PREF_VIDEODEVICE = "videodevice";
    private static readonly string PREF_ROOMNAME = "roomname";
    private static readonly string PREF_IDEALWIDTH = "idealwidth";
    private static readonly string PREF_IDEALHEIGHT = "idealheight";
    private static readonly string PREF_IDEALFPS = "idealfps";
    private static readonly string PREF_REJOIN = "rejoin";
    private static readonly string PREF_LOCALVIDEO = "localvideo";

    protected virtual void Awake()
    {
        mApp = GetComponent<CallApp>();

        if (Application.isMobilePlatform == false)
            uLoudspeakerButton.gameObject.SetActive(false);
        mPrefix += this.gameObject.name + "_";
        LoadSettings();
    }

    protected virtual void Start()
    {
        if (this.uVideoOverlay != null)
        {
            this.uVideoOverlay.gameObject.SetActive(false);
        }
    }


    private void SaveSettings()
    {
        PlayerPrefsSetBool(mPrefix + PREF_AUDIO, uAudioToggle.isOn);
        PlayerPrefsSetBool(mPrefix + PREF_VIDEO, uVideoToggle.isOn);
        PlayerPrefs.SetString(mPrefix + PREF_VIDEODEVICE, GetSelectedVideoDevice());
        PlayerPrefs.SetString(mPrefix + PREF_ROOMNAME, uRoomNameInputField.text);
        PlayerPrefs.SetString(mPrefix + PREF_IDEALWIDTH, uIdealWidth.text);
        PlayerPrefs.SetString(mPrefix + PREF_IDEALHEIGHT, uIdealHeight.text);
        PlayerPrefs.SetString(mPrefix + PREF_IDEALFPS, uIdealFps.text);
        PlayerPrefsSetBool(mPrefix + PREF_REJOIN, uRejoinToggle.isOn);
        PlayerPrefsSetBool(mPrefix + PREF_LOCALVIDEO, uLocalVideoToggle.isOn);
        PlayerPrefs.Save();
    }

    private string mStoredVideoDevice = null;

    /// <summary>
    /// Loads the ui state from last use
    /// </summary>
    private void LoadSettings()
    {

        uAudioToggle.isOn = PlayerPrefsGetBool(mPrefix + PREF_AUDIO, true);
        uVideoToggle.isOn = PlayerPrefsGetBool(mPrefix + PREF_VIDEO, true);
        //can't select this immediately because we don't know if it is valid yet
        mStoredVideoDevice = PlayerPrefs.GetString(mPrefix + PREF_VIDEODEVICE, null);
        uRoomNameInputField.text = PlayerPrefs.GetString(mPrefix + PREF_ROOMNAME, uRoomNameInputField.text);
        uIdealWidth.text = PlayerPrefs.GetString(mPrefix + PREF_IDEALWIDTH, "320");
        uIdealHeight.text = PlayerPrefs.GetString(mPrefix + PREF_IDEALHEIGHT, "240");
        uIdealFps.text = PlayerPrefs.GetString(mPrefix + PREF_IDEALFPS, "30");
        uRejoinToggle.isOn = PlayerPrefsGetBool(mPrefix + PREF_REJOIN, false);
        uLocalVideoToggle.isOn = PlayerPrefsGetBool(mPrefix + PREF_LOCALVIDEO, true);
    }

    private static bool PlayerPrefsGetBool(string name, bool defval)
    {
        int def = 0;
        if (defval)
            def = 1;
        return PlayerPrefs.GetInt(name, def) == 1 ? true : false;
    }

    private static void PlayerPrefsSetBool(string name, bool value)
    {
        PlayerPrefs.SetInt(name, value ? 1 : 0);
    }

    private string GetSelectedVideoDevice()
    {
        if (uVideoDropdown.value <= 0 || uVideoDropdown.value >= uVideoDropdown.options.Count)
        {
            return null;
        }
        else
        {
            string devname = uVideoDropdown.options[uVideoDropdown.value].text;
            return devname;
        }
    }

    private static int TryParseInt(string value, int defval)
    {
        int result;
        if (int.TryParse(value, out result) == false)
        {
            result = defval;
        }
        return result;
    }

    private void SetupCallApp()
    {
        mApp.SetVideoDevice(GetSelectedVideoDevice());
        mApp.SetAudio(uAudioToggle.isOn);
        mApp.SetVideo(uVideoToggle.isOn);

        int width = TryParseInt(uIdealWidth.text, 320);
        int height = TryParseInt(uIdealHeight.text, 240);
        int fps = TryParseInt(uIdealFps.text, 320);
        mApp.SetIdealResolution(width, height);
        mApp.SetIdealFps(fps);
        mApp.SetAutoRejoin(uRejoinToggle.isOn);
        mApp.SetShowLocalVideo(uLocalVideoToggle.isOn);
        mApp.SetupCall();
        EnsureLength();
        Append("Trying to listen on address " + uRoomNameInputField.text);
        mApp.Join(uRoomNameInputField.text);
    }
    
    public void ToggleSettings()
    {
        uMainSetupPanel.gameObject.SetActive(!uMainSetupPanel.gameObject.activeSelf);
        uSettingsPanel.gameObject.SetActive(!uSettingsPanel.gameObject.activeSelf);
    }

    public void ToggleSetup()
    {
        uSetupPanel.gameObject.SetActive(!uSetupPanel.gameObject.activeSelf);
    }

    /// <summary>
    /// Updates the texture based on the given frame update.
    /// 
    /// Returns true if a complete new texture was created
    /// </summary>
    /// <param name="tex"></param>
    /// <param name="frame"></param>
    protected bool UpdateTexture(ref Texture2D tex, RawFrame frame, FramePixelFormat format)
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
            Debug.Log("Creating new texture with resolution " + frame.Width + "x" + frame.Height + " Format:" + format);

            //so far only ABGR is really supported. this will change later
            if (format == FramePixelFormat.ABGR)
            {
                tex = new Texture2D(frame.Width, frame.Height, TextureFormat.RGBA32, false);
            }
            else
            {
                Debug.LogWarning("YUY2 texture is set. This is only for testing");
                tex = new Texture2D(frame.Width, frame.Height, TextureFormat.YUY2, false);
            }
            tex.wrapMode = TextureWrapMode.Clamp;
        }
        //copy image data into the texture and apply
        //Watch out the RawImage has the top pixels in the top row but
        //unity has the top pixels in the bottom row. Result is an image that is
        //flipped. Fixing this here would waste a lot of CPU power thus
        //the UI will simply set scale.Y of the UI element to -1 to reverse this.
        tex.LoadRawTextureData(frame.Buffer);
        tex.Apply();
        return newTextureCreated;
    }

    /// <summary>
    /// Updates the local video. If the frame is null it will hide the video image
    /// </summary>
    /// <param name="frame"></param>
    public virtual void UpdateLocalTexture(RawFrame frame, FramePixelFormat format)
    {
        if (uLocalVideoImage != null)
        {
            if (frame != null)
            {
                UpdateTexture(ref mLocalVideoTexture, frame, format);
                uLocalVideoImage.texture = mLocalVideoTexture;
                if (uLocalVideoImage.gameObject.activeSelf == false)
                {
                    uLocalVideoImage.gameObject.SetActive(true);
                }
                //apply rotation
                //watch out uLocalVideoImage should be scaled -1 X to make the local camera appear mirrored
                //it should also be scaled -1 Y because Unity reads the image from bottom to top
                uLocalVideoImage.transform.rotation = Quaternion.Euler(0, 0, frame.Rotation);

                mHasLocalVideo = true;
                mLocalFrameCounter++;
                mLocalVideoWidth = frame.Width;
                mLocalVideoHeight = frame.Height;
                mLocalVideoFormat = format;
            }
            else
            {
                //app shutdown. reset values
                mHasLocalVideo = false;
                uLocalVideoImage.texture = null;
                uLocalVideoImage.transform.rotation = Quaternion.Euler(0, 0, 0);
                uLocalVideoImage.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Updates the remote video. If the frame is null it will hide the video image.
    /// </summary>
    /// <param name="frame"></param>
    public virtual void UpdateRemoteTexture(RawFrame frame, FramePixelFormat format)
    {
        if (uRemoteVideoImage != null)
        {
            if (frame != null)
            {
                UpdateTexture(ref mRemoteVideoTexture, frame, format);
                uRemoteVideoImage.texture = mRemoteVideoTexture;
                //watch out: due to conversion from WebRTC to Unity format the image is flipped (top to bottom)
                //this also inverts the rotation
                uRemoteVideoImage.transform.rotation = Quaternion.Euler(0, 0, frame.Rotation * -1);
                mHasRemoteVideo = true;
                mRemoteVideoWidth = frame.Width;
                mRemoteVideoHeight = frame.Height;
                mRemoteVideoFormat = format;
                mRemoteFrameCounter++;
            }
            else
            {
                mHasRemoteVideo = false;
                uRemoteVideoImage.texture = uNoCameraTexture;
                uRemoteVideoImage.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
        }
    }

    /// <summary>
    /// toggle audio on / off
    /// </summary>
    /// <param name="state"></param>
    public void AudioToggle(bool state)
    {
        //moved. this is done during SetupCallApp
    }

    /// <summary>
    /// toggle video on / off
    /// </summary>
    /// <param name="state"></param>
    public void VideoToggle(bool state)
    {

    }


    /// <summary>
    /// Updates the dropdown menu based on the current video devices and toggle status
    /// </summary>
    public void UpdateVideoDropdown()
    {
        uVideoDropdown.ClearOptions();
        uVideoDropdown.AddOptions(new List<string>(mApp.GetVideoDevices()));
        uVideoDropdown.interactable = mApp.CanSelectVideoDevice();

        //restore the stored selection if possible
        if(uVideoDropdown.interactable  && mStoredVideoDevice != null)
        {
            int index = 0;
            foreach(var opt in uVideoDropdown.options)
            {
                if(opt.text == mStoredVideoDevice)
                {
                    uVideoDropdown.value = index;
                }
                index++;
            }
        }
    }
    public void VideoDropdownOnValueChanged(int index)
    {
        //moved to SetupCallApp
    }


    /// <summary>
    /// Adds a new message to the message view
    /// </summary>
    /// <param name="text"></param>
    public void Append(string text)
    {
        if (uMessageOutput != null)
        {
            uMessageOutput.AddTextEntry(text);
        }
        Debug.Log("Chat output: " + text);
    }

    private void SetFullscreen(bool value)
    {
        mFullscreen = value;
        if (mFullscreen)
        {
            uVideoPanel.SetParent(uVideoBaseFullscreen, false);
            uChatPanel.SetParent(uChatBaseFullscreen, false);
            uInCallBase.gameObject.SetActive(false);
            uFullscreenPanel.gameObject.SetActive(true);
        }
        else
        {
            uVideoPanel.GetComponent<RectTransform>().SetParent(uVideoBase, false);
            uChatPanel.GetComponent<RectTransform>().SetParent(uChatBase, false);
            uInCallBase.gameObject.SetActive(true);
            uFullscreenPanel.gameObject.SetActive(false);
        }
    }
    public void Fullscreen()
    {

        bool newValues = !mFullscreen;

        //just in case: make sure fullscreen button is ignored if in setup mode
        if (newValues == true && uSetupPanel.gameObject.activeSelf)
            return;
        SetFullscreen(newValues);

        transform.SetAsLastSibling();
    }

    public void ShowOverlay()
    {
        if(this.uVideoOverlay == null)
        {
            Debug.LogError("VideoOverlay transform is missing.");
            return;
        }
        if(this.uVideoOverlay.gameObject.activeSelf)
        {
            this.uVideoOverlay.gameObject.SetActive(false);
            mVideoOverlayTimeout = 0;
        }
        else
        {
            this.uVideoOverlay.gameObject.SetActive(true);
            mVideoOverlayTimeout = sDefaultOverlayTimeout;
        }
    }
    /// <summary>
    /// Shows the setup screen or the chat + video
    /// </summary>
    /// <param name="showSetup">true Shows the setup. False hides it.</param>
    public void SetGuiState(bool showSetup)
    {
        uSetupPanel.gameObject.SetActive(showSetup);

        uSendMessageButton.interactable = !showSetup;
        uShutdownButton.interactable = !showSetup;
        uMessageInputField.interactable = !showSetup;

        //this is going to hide the textures until it is updated with a new frame update
        UpdateLocalTexture(null, FramePixelFormat.Invalid);
        UpdateRemoteTexture(null, FramePixelFormat.Invalid);
        SetFullscreen(false);
    }

    /// <summary>
    /// Join button pressed. Tries to join a room.
    /// </summary>
    public void JoinButtonPressed()
    {
        SaveSettings();
        SetupCallApp();
    }

    private void EnsureLength()
    {
        if (uRoomNameInputField.text.Length > CallApp.MAX_CODE_LENGTH)
        {
            uRoomNameInputField.text = uRoomNameInputField.text.Substring(0, CallApp.MAX_CODE_LENGTH);
        }
    }

    public string GetRoomname()
    {
        EnsureLength();
        return uRoomNameInputField.text;
    }

    /// <summary>
    /// This is called if the send button
    /// </summary>
    public void SendButtonPressed()
    {
        //get the message written into the text field
        string msg = uMessageInputField.text;
        SendMsg(msg);
    }

    /// <summary>
    /// User either pressed enter or left the text field
    /// -> if return key was pressed send the message
    /// </summary>
    public void InputOnEndEdit()
    {
        if (Input.GetKey(KeyCode.Return))
        {
            string msg = uMessageInputField.text;
            SendMsg(msg);
        }
    }

    /// <summary>
    /// Sends a message to the other end
    /// </summary>
    /// <param name="msg"></param>
    private void SendMsg(string msg)
    {
        if (String.IsNullOrEmpty(msg))
        {
            //never send null or empty messages. webrtc can't deal with that
            return;
        }

        Append(msg);
        mApp.Send(msg);

        //reset UI
        uMessageInputField.text = "";
        uMessageInputField.Select();
    }



    /// <summary>
    /// Shutdown button pressed. Shuts the network down.
    /// </summary>
    public void ShutdownButtonPressed()
    {
        mApp.ResetCall();
    }

    public void OnVolumeChanged(float value)
    {
        mApp.SetRemoteVolume(value);
    }

    public void OnSpeakerButtonPressed()
    {
        bool speakerState = mApp.GetLoudspeakerStatus();
        mApp.SetLoudspeakerStatus(!speakerState);
    }



    protected virtual void Update()
    {
        if(mVideoOverlayTimeout > 0)
        {
            string local = "Local:";
            if (mHasLocalVideo == false)
            {
                local += "no video";
            }
            else
            {
                local += mLocalVideoWidth + "x" + mLocalVideoHeight + Enum.GetName(typeof(FramePixelFormat), mLocalVideoFormat) + " FPS:" + mLocalFps;
            }
            string remote = "Remote:";
            if (mHasRemoteVideo == false)
            {
                remote += "no video";
            }
            else
            {
                remote += mRemoteVideoWidth + "x" + mRemoteVideoHeight + Enum.GetName(typeof(FramePixelFormat), mRemoteVideoFormat) + " FPS:" + mRemoteFps;
            }

            uOverlayInfo.text = local + "\n" + remote;
            mVideoOverlayTimeout -= Time.deltaTime;
            if(mVideoOverlayTimeout <= 0)
            {
                mVideoOverlayTimeout = 0;
                uVideoOverlay.gameObject.SetActive(false);
            }
        }

        float fpsTimeDif = Time.realtimeSinceStartup - mFpsTimer;
        if(fpsTimeDif > 1)
        {
            mLocalFps = Mathf.RoundToInt( mLocalFrameCounter / fpsTimeDif);
            mRemoteFps = Mathf.RoundToInt(mRemoteFrameCounter / fpsTimeDif);
            mFpsTimer = Time.realtimeSinceStartup;
            mLocalFrameCounter = 0;
            mRemoteFrameCounter = 0;
        }
    }
}
