using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WiimoteApi;

// Helper class to organize highlighting buttons
[System.Serializable]
public class WiimoteButtonMeshes {
    public Renderer D_Up;
    public Renderer D_Left;
    public Renderer D_Right;
    public Renderer D_Down;
    public Renderer A;
    public Renderer B;
    public Renderer Home;
    public Renderer Minus;
    public Renderer Plus;
    public Renderer One;
    public Renderer Two;

    public void HideAll(){
        D_Up.enabled = false;
        D_Left.enabled = false;
        D_Right.enabled = false;
        D_Down.enabled = false;
        A.enabled = false;
        B.enabled = false;
        Home.enabled = false;
        Minus.enabled = false;
        Plus.enabled = false;
        One.enabled = false;
        Two.enabled = false;
    }
    public void ShowPressedButtons(WiimoteApi.ButtonData buttons){
        D_Up.enabled = buttons.d_up;
        D_Left.enabled = buttons.d_left;
        D_Right.enabled = buttons.d_right;
        D_Down.enabled = buttons.d_down;
        A.enabled = buttons.a;
        B.enabled = buttons.b;
        Home.enabled = buttons.home;
        Minus.enabled = buttons.minus;
        Plus.enabled = buttons.plus;
        One.enabled = buttons.one;
        Two.enabled = buttons.two;
    }
}


// Example component to demonstrate wiimote functionality.
[DisallowMultipleComponent]
public class ExampleWiimoteComponent : MonoBehaviour
{
    // main wiimote api object
    [System.NonSerialized]
    public Wiimote wiimote = null;


    [Header("Connection")]

    [Tooltip("How often to check for the wiimote to be connected (in seconds).")]
    public float ScanFrequency = 1.0f;
    private float scanCountdown = 0.0f; // how long until the next wiimote scan

    [Tooltip("What data the wiimote should send back to us.  See InputDataType for more info.\nREPORT_BUTTONS_ACCEL_EXT16 seems to be a good default.")]
    public InputDataType InputType = InputDataType.REPORT_BUTTONS_ACCEL_EXT16;

    [Tooltip("How frequently to send data to the wiimote (in milliseconds).\n0 means default.\nLower values increase sound quality but get more unstable and use more cpu.")]
    [Min(0.0f)]
    public double SendRateMs = 0;


    [Header("Graphics")]

    [Tooltip("Main wiimote model to rotate (defaults to self)")]
    public Renderer WiimoteMesh;

    [Tooltip("Button meshes for demo display")]
    public WiimoteButtonMeshes ButtonMeshes = new WiimoteButtonMeshes();


    [Header("Sounds")]

    public AudioClip SoundToPlay;
    [Tooltip("Toggle this to play the sound!")]
    public bool PlaySound = false;
    private bool soundIsPlaying = false;

    [Tooltip("If the sound should loop.  Will not update any currently-playing sounds!")]
    public bool LoopSound = false;
    [Tooltip("Mutes/Unmutes the speaker and Pauses/Unpauses the current sound")]
    public bool PauseSound = false;
    private bool soundIsPaused = false;

    [Tooltip("Sound volume, 0-255.  Some high values can cause odd artifacts.  Does not affect currently-playing sounds.")]
    public byte Volume = 192;


    [Header("Misc")]

    [Tooltip("If wii-motion-plus should be activated to get gyroscope features.  Only activates on startup.")]
    public bool UseWiiMotionPlus = true;
    private bool wiiMotionPlusIsActivated = false;

    [Tooltip("Toggle this in the inspector!")]
    public bool Rumble = false;

    [Tooltip("Binary value of the LEDs to turn on")]
    [Range(0,15)]
    public int LEDs = 0b0001;
    private int prevLEDs = 1;

    [Tooltip("Indicator for the battery level")]
    public byte BatteryLevel;


    void Start(){
        if (WiimoteMesh == null)
            WiimoteMesh = GetComponent<Renderer>();
        WiimoteMesh.enabled = false;
        ButtonMeshes.HideAll();
    }

    void Update()
    {
        // wait for connection
        if (wiimote == null){
            scanCountdown -= Time.deltaTime;
            if (scanCountdown <= 0.0f){
                scanCountdown = ScanFrequency;
                if (WiimoteManager.FindWiimotes())
                    wiimote = WiimoteManager.Wiimotes[0];
            }
            if (wiimote == null)
                return;
            // else new wiimote connected!

            WiimoteMesh.enabled = true;

            // set initial settings

            // wiimote.SendRateMs = 11; // in real code you can set a good value here if you find one better than the default.  Note the wii-motion-plus remotes must be 6.6667 for audio to work
            wiimote.SendDataReportMode(InputType);
            SetLEDs(); // need to call this or the wiimote will blink forever


            if (UseWiiMotionPlus){
                wiimote.DeactivateWiiMotionPlus(); // RequestIdentifyWiiMotionPlus() fails if it's already activated (eg from last play run)
                wiimote.RequestIdentifyWiiMotionPlus(); // note: this may affect nunchuck behaviour?
            }

            wiimote.SendStatusInfoRequest();

            // wiimote.SetupSpeaker();  // this is called automatically when you first play a sound.
            // no harm in calling it early or multiple times though.
            // if audio starts degrading over time try calling it occasionally and it might help

        } // end wiimote setup


        // debug: adjust send rate so you can play around and find what works for your machine
        if (SendRateMs <= 1.0)
            SendRateMs = wiimote.SendRateMs;
        else if (SendRateMs != wiimote.SendRateMs){
            wiimote.SendRateMs = SendRateMs;
            PlaySound = false; // stuff playing using the old rate sounds wrong
        }


        // gather inputs
        bool rotatedByWiiMotionPlus = false;
        while (wiimote.ReadWiimoteData() > 0)
        {
            // The wiimote apparently sends inputs every 95ms so there may be multiple per frame.
            // For stuff that doesn't accumulate well (eg. rotation deltas or super fast button presses),
            //   make sure to process it inside this loop

            // gyroscope
            MotionPlusData motionplus = wiimote.MotionPlus;
            if (UseWiiMotionPlus && motionplus != null){
                Vector3 pitch_yaw_roll = motionplus.GetAngularVelocity();
                pitch_yaw_roll *= 0.5f; // idk why the calibration isn't right lol
                WiimoteMesh.transform.Rotate(pitch_yaw_roll, Space.Self);
                rotatedByWiiMotionPlus = true;
            }
        } // end per-input updates

        // use acceleration to roughly orient the controller
        // (obviously you can use acceleration for other stuff too)
        if (!rotatedByWiiMotionPlus){
            Vector3 accel = wiimote.Accel.GetAccelVector(); // direction of motion (or gravity)
            var rot = new Quaternion();
            rot.SetFromToRotation(Vector3.down, new Vector3(-accel.x, accel.y, -accel.z));
            WiimoteMesh.transform.localRotation = rot;
        }

        // activate wii motion plus if it's available
        if (UseWiiMotionPlus && !wiiMotionPlusIsActivated && wiimote.wmp_attached){
            wiiMotionPlusIsActivated = true;
            wiimote.ActivateWiiMotionPlus();
        }

        // show battery level in inspector
        StatusData status = wiimote.Status;
        BatteryLevel = status.battery_level; // status is only updated after SendStatusInfoRequest()

        // buttons
        ButtonData buttons = wiimote.Button;
        ButtonMeshes.ShowPressedButtons(buttons);

        // rumble
        if (Rumble != wiimote.RumbleOn){
            wiimote.RumbleOn = Rumble;
            wiimote.SendStatusInfoRequest(); // need to send any message to update the wiimote
        }

        // player LEDs
        if (LEDs != prevLEDs){
            prevLEDs = LEDs;
            SetLEDs();
        }

        // play sounds
        if (PlaySound != soundIsPlaying){
            soundIsPlaying = PlaySound;
            if (soundIsPlaying){
                wiimote.PlaySound(SoundToPlay, LoopSound, Volume);
                PauseSound = false;
            } else {
                wiimote.StopSound();
            }
        }

        // mute/pause sounds
        if (PauseSound != soundIsPaused){
            soundIsPaused = PauseSound;
            wiimote.SendSpeakerMuted(soundIsPaused);
            // in addition to the wiimote's built-in mute function,
            // the library will stop sending audio data when it's muted
        }

    } // end Update()


    // Important to release the wiimote on exit!
    void OnDestroy(){
        if (wiimote != null){
            wiimote.Dispose();
            wiimote = null;
        }
    }


    private void SetLEDs(){
        wiimote.SendPlayerLED((LEDs & 1) != 0, (LEDs & 2) != 0, (LEDs & 4) != 0, (LEDs & 8) != 0); 
    }


    // this is called by PlaySound() but you can call it again any time if the
    //   wiimote internal state starts acting up
    [ContextMenu("Init Speaker")]
    void InitSpeaker(){ wiimote?.SetupSpeaker(Volume); }

    /* // speaker enable/disable kinda does the same as mute/unmute and is automatically handled in SetupSpeaker()
    [ContextMenu("Enable Speaker")]
    void EnableSpeaker(){ wiimote?.SendSpeakerEnabled(true); }
    [ContextMenu("Disable Speaker")]
    void DisableSpeaker(){ wiimote?.SendSpeakerEnabled(false); }
    */


    // gyroscope calibration (prevents drift)
    [ContextMenu("Zero Wii Motion Plus")]
    void ZeroWiiMotionPlus(){  wiimote?.MotionPlus?.SetZeroValues(); }

    // acceleration direction calibration
    [ContextMenu("Calibrate A Button Up")]
    void CalibrateFlat(){  wiimote?.Accel?.CalibrateAccel(AccelCalibrationStep.A_BUTTON_UP); }
    [ContextMenu("Calibrate Expansion Up")]
    void CalibrateExtUp(){  wiimote?.Accel?.CalibrateAccel(AccelCalibrationStep.EXPANSION_UP); }
    [ContextMenu("Calibrate Left Side Up")]
    void CalibrateLeftUp(){  wiimote?.Accel?.CalibrateAccel(AccelCalibrationStep.LEFT_SIDE_UP); }
}
