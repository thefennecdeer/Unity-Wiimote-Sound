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
		internal IntPtr hidapi_wiimote;
		internal bool should_exit = false;

		internal double SendRateMs = WiimoteManager.DefaultSendRateMs;


		private ConcurrentQueue<byte[]> write_queue = new ConcurrentQueue<byte[]>();
		private ConcurrentQueue<byte[]> read_queue = new ConcurrentQueue<byte[]>();


		private bool rumble = false;
		private byte[] read_data = null;

		private object sound_lock = new object();
		private byte[] sound_to_play = null;
		private bool loop_sound = false;
		private bool muted = true;
		private int sample_index = 0;
		private byte[] current_sound = null;
		private byte[] sample_buf = new byte[22] {(byte)OutputDataType.SPEAKER_DATA, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0};


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

		internal void SendSound(byte[] adpcm_samples, bool loop){
			lock(sound_lock) {
				if (sound_to_play == adpcm_samples)
					sample_index = 0; // restart current clip.  todo: this isn't protected by the lock so it may cause a hitch!
				sound_to_play = adpcm_samples;
				loop_sound = loop;
			}
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
				if (data[0] == (byte)OutputDataType.SPEAKER_MUTE){
					muted = (data[1] & 0x04) != 0; // remember any mute/unmute requests
				}
				rumble = (data[1] & 1) != 0;
				int res = HIDapi.hid_write(hidapi_wiimote, data, new UIntPtr(Convert.ToUInt32(data.Length)));
				if (res == -1)
					Debug.LogError("HidAPI reports error " + res + " on write: " + Marshal.PtrToStringUni(HIDapi.hid_error(hidapi_wiimote)));
				else if (WiimoteManager.Debug_Messages)
					Debug.Log("Sent " + res + "b: [" + data[0].ToString("X").PadLeft(2, '0') + "] " + BitConverter.ToString(data, 1));

				//return;
			}

			// sound
			byte[] audio;
			bool should_loop;
			lock(sound_lock) {
				audio = sound_to_play;
				should_loop = loop_sound;
			}
			if (audio == null){
				current_sound = null;
				sample_index = 0;
			} else {
				if (current_sound != audio){
					// new sound
					current_sound = audio;
					sample_index = 0;
				}

				if (!muted){
					int audio_len = audio.Length;
					int i = 0;
					for (; i < 20; ++i){
						if (sample_index >= audio_len){
							if (should_loop)
								sample_index = 0;
							else {
								lock(sound_lock){
									if (sound_to_play == audio){
										sound_to_play = null;
									}
								}
								current_sound = null;
								sample_index = 0;
								break;
							}
						}
						sample_buf[2 + i] = audio[sample_index++];
					}
					sample_buf[1] = (byte)(i << 3);
					if (rumble)
						sample_buf[1] |= 1;

					int res = HIDapi.hid_write(hidapi_wiimote, sample_buf, new UIntPtr(Convert.ToUInt32(sample_buf.Length)));
					if (res == -1)
						Debug.LogError("HidAPI reports error " + res + " on write: " + Marshal.PtrToStringUni(HIDapi.hid_error(hidapi_wiimote)));
				}
			}
		}
	}
}
