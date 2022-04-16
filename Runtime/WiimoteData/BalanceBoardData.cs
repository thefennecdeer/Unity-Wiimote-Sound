using UnityEngine;
using WiimoteApi.Util;
using System;


// Added Wii Balance Board : Jelle Vermandere 2020

namespace WiimoteApi
{
    public class BalanceBoardData : WiimoteData
    {
        //measure values

        public int topLeft { get { return _topLeft; } }
        private int _topLeft;

        public int topRight { get { return _topRight; } }
        private int _topRight;

        public int bottomLeft { get { return _bottomLeft; } }
        private int _bottomLeft;

        public int bottomRight { get { return _bottomRight; } }
        private int _bottomRight;

        public Vector2 offset { get { return _offset; } }
        private Vector2 _offset;

        private int topLeftRaw = 0;
        private int topRightRaw = 0;
        private int bottomLeftRaw = 0;
        private int bottomRightRaw = 0;

        private int _topLeft0 = 0;
        private int _topRight0 = 0;
        private int _bottomLeft0 = 0;
        private int _bottomRight0 = 0;

        private int _topLeftHigh = 1;
        private int _topRightHigh = 1;
        private int _bottomLeftHigh = 1;
        private int _bottomRightHigh = 1;

        //Future update?
        /*
        //calibration values

        private int _topLeft0;
        private int _topRight0;
        private int _bottomLeft0;
        private int _bottomRight0;

        private int _topLeft17;
        private int _topRight17;
        private int _bottomLeft17;
        private int _bottomRight17;

        private int _topLeft34;
        private int _topRight34;
        private int _bottomLeft34;
        private int _bottomRight34;
        */


        public byte[] rawdata;

        public BalanceBoardData(Wiimote owner) : base(owner)
        {

        }

        public override bool InterpretData(byte[] data)
        {
            if (data == null || data.Length < 11)
            {
                _topLeft = 0;
                _topRight = 0;
                _bottomLeft = 0;
                _bottomRight = 0;

                return false;
            }

            rawdata = data;

            topRightRaw      = data[1];
            topRightRaw     |= data[0] << 8;
            bottomRightRaw   = data[3];
            bottomRightRaw  |= data[2] << 8;
            topLeftRaw       = data[5];
            topLeftRaw      |= data[4] << 8;
            bottomLeftRaw    = data[7];
            bottomLeftRaw   |= data[6] << 8;

            _topLeft        = Mathf.RoundToInt(25 * (topLeftRaw - _topLeft0)          / (float)(_topLeftHigh - _topLeft0));
            _topRight       = Mathf.RoundToInt(25 * (topRightRaw - _topRight0)        / (float)(_topRightHigh - _topRight0));
            _bottomLeft     = Mathf.RoundToInt(25 * (bottomLeftRaw - _bottomLeft0)    / (float)(_bottomLeftHigh - _bottomLeft0));
            _bottomRight    = Mathf.RoundToInt(25 * (bottomRightRaw - _bottomRight0)  / (float)(_bottomRightHigh - _bottomRight0));

            _offset = new Vector2((_topRight + _bottomRight) - (_topLeft + _bottomLeft), (_topLeft + _topRight) - (_bottomLeft + _bottomRight));

            return true;
        }

        public void CalibrateLow()
        {
            _topLeft0       = topLeftRaw;
            _topRight0      = topRightRaw;
            _bottomLeft0    = bottomLeftRaw;
            _bottomRight0   = bottomRightRaw;
        }

        public void CalibrateHigh()
        {
            _topLeftHigh    = topLeftRaw;
            _topRightHigh    = topRightRaw;
            _bottomLeftHigh = bottomLeftRaw;
            _bottomRightHigh = bottomRightRaw;
        }
    }
}
