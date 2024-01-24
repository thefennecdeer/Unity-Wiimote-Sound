using UnityEngine;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.Threading;
using System.Runtime.InteropServices;
using WiimoteApi.Internal;

namespace WiimoteApi
{
	/// Helper class responsible for queuing data to/from the wiimote on the separate thread.
	/// Each wiimote has its own instantiation of this class and the thread just checks them all.
	internal class WiimoteDataSender {
		public bool should_exit = false;

		internal IntPtr hidapi_wiimote;

		private ConcurrentQueue<byte[]> write_queue = new ConcurrentQueue<byte[]>();
		private ConcurrentQueue<byte[]> read_queue = new ConcurrentQueue<byte[]>();
		private byte[] read_data = null;

		internal WiimoteDataSender(IntPtr handle){
			hidapi_wiimote = handle;
		}


		/// \brief Sends RAW DATA to the given bluetooth HID device.  This is essentially a wrapper around HIDApi.
		/// \param hidapi_wiimote The HIDApi device handle to write to.
		/// \param data The data to write.
		/// \sa Wiimote::SendWithType(OutputDataType, byte[])
		/// \warning DO NOT use this unless you absolutely need to bypass the given Wiimote communication functions.
		///          Use the functionality provided by Wiimote instead.
		
		internal int SendRaw(byte[] data){
			write_queue.Enqueue(data);
			return 0;
		}

		/// \brief Attempts to recieve RAW DATA to the given bluetooth HID device.  This is essentially a wrapper around HIDApi.
		/// \param hidapi_wiimote The HIDApi device handle to write to.
		/// \param buf The data to write.
		/// \sa Wiimote::ReadWiimoteData()
		/// \warning DO NOT use this unless you absolutely need to bypass the given Wiimote communication functions.
		///          Use the functionality provided by Wiimote instead.
		internal int ReadRaw(out byte[] data){
			if (read_queue.TryDequeue(out data))
				return data.Length;
			else
				return 0;
		}

		internal void UpdateOnThread(){
			// read
			while (true){
				if (read_data == null){
					read_data = new byte[22];
				}
				int res = HIDapi.hid_read(hidapi_wiimote, read_data, new UIntPtr(Convert.ToUInt32(read_data.Length)));
				if (res > 0){
					if (res != read_data.Length){
						Array.Resize(ref read_data, res);
					}
					read_queue.Enqueue(read_data);
					read_data = null;
				} else {
					break;
				}
			}

			// write
			if (write_queue.TryDequeue(out byte[] data)){
				int res = HIDapi.hid_write(hidapi_wiimote, data, new UIntPtr(Convert.ToUInt32(data.Length)));
				if (res == -1)
					Debug.LogError("HidAPI reports error " + res + " on write: " + Marshal.PtrToStringUni(HIDapi.hid_error(hidapi_wiimote)));
				else if (WiimoteManager.Debug_Messages)
					Debug.Log("Sent " + res + "b: [" + data[0].ToString("X").PadLeft(2, '0') + "] " + BitConverter.ToString(data, 1));
			}

			// todo: audio
		}
	}
}
