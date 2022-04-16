using WiimoteApi;
using WiimoteApi.Util;

namespace WiimoteApi
{
    public class MotionPlusClassicControllerData : WiimoteData
    {
    //motion plus

        /// The rotational velocity in the Pitch direction of the Wii Remote, as
        /// reported by the Wii Motion Plus.  Measured in degrees per second.
        ///
        /// \note The Wii Remote sends updates at a frequency of 95Hz.  So, one way
        ///       of finding the change in degrees over the previous report is to divide
        ///       this value by 95.
        public float PitchSpeed { get { return _PitchSpeed; } }
        private float _PitchSpeed = 0;

        private int _PitchSpeedRaw = 0;

        /// The rotational velocity in the Yaw direction of the Wii Remote, as
        /// reported by the Wii Motion Plus.  Measured in degrees per second.
        ///
        /// \note The Wii Remote sends updates at a frequency of 95Hz.  So, one way
        ///       of finding the change in degrees over the previous report is to divide
        ///       this value by 95.
        public float YawSpeed { get { return _YawSpeed; } }
        private float _YawSpeed = 0;

        private int _YawSpeedRaw = 0;

        /// The rotational velocity in the Roll direction of the Wii Remote, as
        /// reported by the Wii Motion Plus.  Measured in degrees per second.
        ///
        /// \note The Wii Remote sends updates at a frequency of 95Hz.  So, one way
        ///       of finding the change in degrees over the previous report is to divide
        ///       this value by 95.
        public float RollSpeed { get { return _RollSpeed; } }
        private float _RollSpeed;

        private int _RollSpeedRaw = 0;

        /// If true, the Wii Motion Plus reports that it is in "slow" mode in the
        /// Pitch direction.  This means that it is more precise as it doesn't have
        /// to report higher values.  If false often, it is more likely that the Wii Motion
        /// Plus will "fall out of sync" with the real world.
        public bool PitchSlow { get { return _PitchSlow; } }
        private bool _PitchSlow = false;

        /// If true, the Wii Motion Plus reports that it is in "slow" mode in the
        /// Yaw direction.  This means that it is more precise as it doesn't have
        /// to report higher values.  If false often, it is more likely that the Wii Motion
        /// Plus will "fall out of sync" with the real world.
        public bool YawSlow { get { return _YawSlow; } }
        private bool _YawSlow = false;

        /// If true, the Wii Motion Plus reports that it is in "slow" mode in the
        /// Roll direction.  This means that it is more precise as it doesn't have
        /// to report higher values.  If false often, it is more likely that the Wii Motion
        /// Plus will "fall out of sync" with the real world.
        public bool RollSlow { get { return _RollSlow; } }
        private bool _RollSlow = false;

        /// If true, the Wii Motion Plus reports that an extension is connected in its
        /// extension port.
        public bool ExtensionConnected { get { return _ExtensionConnected; } }
        private bool _ExtensionConnected = false;

        private int _PitchZero = 8063;
        private int _YawZero = 8063;
        private int _RollZero = 8063;

        // I would like to say that this was calculated or something, but honestly this was created
        // simply through trial and error.  I am going to tweak this constant to see if I can get it
        // any better in the future.  Realistically this value is the result of the Analog/Digital converter
        // in the Wii Motion Plus along with the analog output of the gyros, but the documentation is so
        // shitty that I don't even care anymore.
        private const float MagicCalibrationConstant = 0.05f;

    //classic controller

        /// Classic Controller left stick analog values.  This is a size-2 array [X,Y]
        /// of RAW (unprocessed) stick data.  These values are in the range 0-63
        /// in both X and Y.
        ///
        /// \sa GetLeftStick01()
        public ReadOnlyArray<byte> lstick { get { return _lstick_readonly; } }
        private ReadOnlyArray<byte> _lstick_readonly;
        private byte[] _lstick;

        /// Classic Controller right stick analog values.  This is a size-2 array [X,Y]
        /// of RAW (unprocessed) stick data.  These values are in the range 0-31
        /// in both X and Y.
        /// 
        /// \note The Right analog stick reports one less bit of precision than the left
        ///       stick (the left stick is in the range 0-63 while the right is 0-31).
        ///
        /// \sa GetRightStick01()
        public ReadOnlyArray<byte> rstick { get { return _rstick_readonly; } }
        private ReadOnlyArray<byte> _rstick_readonly;
        private byte[] _rstick;

        /// Classic Controller left trigger analog value.  This is RAW (unprocessed) analog
        /// data.  It is in the range 0-31 (with 0 being unpressed and 31 being fully pressed).
        ///
        /// \sa rtrigger_range, ltrigger_switch, ltrigger_switch
        public byte ltrigger_range { get { return _ltrigger_range; } }
        private byte _ltrigger_range;

        /// Classic Controller right trigger analog value.  This is RAW (unprocessed) analog
        /// data.  It is in the range 0-31 (with 0 being unpressed and 31 being fully pressed).
        ///
        /// \sa ltrigger_range, rtrigger_switch, rtrigger_switch
        public byte rtrigger_range { get { return _rtrigger_range; } }
        private byte _rtrigger_range;

        /// Button: Left trigger (bottom out switch)
        /// \sa rtrigger_switch, rtrigger_range, ltrigger_range
        public bool ltrigger_switch { get { return _ltrigger_switch; } }
        private bool _ltrigger_switch;

        /// Button: Right trigger (button out switch)
        /// \sa ltrigger_switch, ltrigger_range, rtrigger_range
        public bool rtrigger_switch { get { return _rtrigger_switch; } }
        private bool _rtrigger_switch;

        /// Button: A
        public bool a { get { return _a; } }
        private bool _a;

        /// Button: B
        public bool b { get { return _b; } }
        private bool _b;

        /// Button: X
        public bool x { get { return _x; } }
        private bool _x;

        /// Button: Y
        public bool y { get { return _y; } }
        private bool _y;

        /// Button: + (plus)
        public bool plus { get { return _plus; } }
        private bool _plus;

        /// Button: - (minus)
        public bool minus { get { return _minus; } }
        private bool _minus;

        /// Button: home
        public bool home { get { return _home; } }
        private bool _home;

        /// Button:  ZL
        public bool zl { get { return _zl; } }
        private bool _zl;

        /// Button: ZR
        public bool zr { get { return _zr; } }
        private bool _zr;

        /// Button: D-Pad Up
        public bool dpad_up { get { return _dpad_up; } }
        private bool _dpad_up;

        /// Button: D-Pad Down
        public bool dpad_down { get { return _dpad_down; } }
        private bool _dpad_down;

        /// Button: D-Pad Left
        public bool dpad_left { get { return _dpad_left; } }
        private bool _dpad_left;

        /// Button: D-Pad Right
        public bool dpad_right { get { return _dpad_right; } }
        private bool _dpad_right;


        public MotionPlusClassicControllerData(Wiimote owner) : base(owner)
        {
            _lstick = new byte[2];
            _lstick_readonly = new ReadOnlyArray<byte>(_lstick);

            _rstick = new byte[2];
            _rstick_readonly = new ReadOnlyArray<byte>(_rstick);

        }

        public byte[] rawData = new byte[0]; // the raw data is also stored for debugging


        public override bool InterpretData(byte[] data)
        {
            rawData = data;

            if (data == null || data.Length < 6)
            {
                rawData = new byte[0];
                return false;
            }


            // the byte to check weither the data is from the wmp or the classic controller 
            if ((data[5] & 0x02) == 0x02) // if 1 > WMP
            {
                _YawSpeedRaw     = data[0];
                _YawSpeedRaw    |= (data[3] & 0xfc) << 6;
                _RollSpeedRaw    = data[1];
                _RollSpeedRaw   |= (data[4] & 0xfc) << 6;
                _PitchSpeedRaw   = data[2];
                _PitchSpeedRaw  |= (data[5] & 0xfc) << 6;

                _YawSlow    = (data[3] & 0x02) == 0x02;
                _PitchSlow  = (data[3] & 0x01) == 0x01;
                _RollSlow   = (data[4] & 0x02) == 0x02;

                _ExtensionConnected = (data[4] & 0x01) == 0x01;

                _PitchSpeed = (float)(_PitchSpeedRaw - _PitchZero) * MagicCalibrationConstant;
                _YawSpeed   = (float)(_YawSpeedRaw - _YawZero) * MagicCalibrationConstant;
                _RollSpeed  = (float)(_RollSpeedRaw - _RollZero) * MagicCalibrationConstant;

                // At high speeds, the Wii Remote Reports with less precision to reach higher values.
                // The multiplier is 2000 / 440 when in fast mode.
                // http://wiibrew.org/wiki/Wiimote/Extension_Controllers/Wii_Motion_Plus
                if (!PitchSlow)
                    _PitchSpeed *= 2000f / 440f;
                if (!YawSlow)
                    _YawSpeed *= 2000f / 440f;
                if (!RollSlow)
                    _RollSpeed *= 2000f / 440f;

            }

            else // if 0 > classic controller values
            {
                _lstick[0] = (byte)(data[0] & 0x3e); //changed, has one less significant nr
                _lstick[1] = (byte)(data[1] & 0x3e); //changed, has one less significant nr

                _rstick[0] = (byte)(((data[0] & 0xc0) >> 3) |
                                    ((data[1] & 0xc0) >> 5) |
                                    ((data[2] & 0x80) >> 7));
                _rstick[1] = (byte)(data[2] & 0x1f);

                _ltrigger_range = (byte)(((data[2] & 0x60) >> 2) |
                                    ((data[3] & 0xe0) >> 5));

                _rtrigger_range = (byte)(data[3] & 0x1f);

                // Bit is zero when pressed, one when up.  This is really weird so I reverse
                // the bit with !=
                _dpad_right         = (data[4] & 0x80) != 0x80;
                _dpad_down          = (data[4] & 0x40) != 0x40;
                _ltrigger_switch    = (data[4] & 0x20) != 0x20;
                _minus              = (data[4] & 0x10) != 0x10;
                _home               = (data[4] & 0x08) != 0x08;
                _plus               = (data[4] & 0x04) != 0x04;
                _rtrigger_switch    = (data[4] & 0x02) != 0x02;

                _zl                 = (data[5] & 0x80) != 0x80;
                _b                  = (data[5] & 0x40) != 0x40;
                _y                  = (data[5] & 0x20) != 0x20;
                _a                  = (data[5] & 0x10) != 0x10;
                _x                  = (data[5] & 0x08) != 0x08;
                _zr                 = (data[5] & 0x04) != 0x04;
                _dpad_left          = (data[1] & 0x01) != 0x01; //changed, moved to another place
                _dpad_up            = (data[0] & 0x01) != 0x01; //changed, moved to another place
            }

            return true;
        }

        /// Returns the left stick analog values in the range 0-1.
        ///
        /// \warning This does not take into account zero points or deadzones.  Likewise it does not guaruntee that 0.5f
        ///			 is the zero point.  You must do these calibrations yourself.
        public float[] GetLeftStick01()
        {
            float[] ret = new float[2];
            for (int x = 0; x < 2; x++)
            {
                ret[x] = lstick[x];
                ret[x] /= 63;
            }
            return ret;
        }

        /// Returns the right stick analog values in the range 0-1.
        ///
        /// \note The Right stick has half of the precision of the left stick due to how the Wiimote reports data.  The
        /// 	  right stick is therefore better for less precise input.
        ///
        /// \warning This does not take into account zero points or deadzones.  Likewise it does not guaruntee that 0.5f
        ///			 is the zero point.  You must do these calibrations yourself.
        public float[] GetRightStick01()
        {
            float[] ret = new float[2];
            for (int x = 0; x < 2; x++)
            {
                ret[x] = rstick[x];
                ret[x] /= 31;
            }
            return ret;
        }

        /// Calibrates the zero values of the Wii Motion Plus in the Pitch, Yaw, and Roll directions.
        /// The Wii Remote should be in a flat, motionless position when calibrating (for example, face
        /// down on a flat surface).
        ///
        /// A good idea here is to reference the Accelerometer values of the Wii Remote to make sure that
        /// your simulated rotation is consistent with the actual rotation of the remote.
        public void SetZeroValues()
        {
            _PitchZero = _PitchSpeedRaw;
            _YawZero = _YawSpeedRaw;
            _RollZero = _RollSpeedRaw;

            _PitchSpeedRaw = 0;
            _YawSpeedRaw = 0;
            _RollSpeedRaw = 0;
            _PitchSpeed = 0;
            _YawSpeed = 0;
            _RollSpeed = 0;
        }
    }
}