using UnityEngine;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System;
using System.Threading;
using System.Runtime.InteropServices;
using WiimoteApi.Internal;

namespace WiimoteApi
{

    public static class WiimoteManager
    {
        private const ushort vendor_id_wiimote = 0x057e;
        private const ushort product_id_wiimote = 0x0306;
        private const ushort product_id_wiimoteplus = 0x0330;

        /// A list of all currently connected Wii Remotes.
        public static List<Wiimote> Wiimotes { get { return _Wiimotes; } }
        private static List<Wiimote> _Wiimotes = new List<Wiimote>();

        /// If true, WiimoteManager and Wiimote will write data reports and other debug
        /// messages to the console.  Any incorrect usages / errors will still be reported.
        public static bool Debug_Messages = false;

        /// The maximum time, in milliseconds, between data report writes.  This prevents
        /// WiimoteApi from attempting to write faster than most bluetooth drivers can handle.
        ///
        /// If you attempt to write at a rate faster than this, the extra write requests will
        /// be queued up and written to the Wii Remote after the delay is up.
        public static int MaxWriteFrequency = 5; // In ms

        /// Mechanism to notify the read/write thread about new wiimotes to keep track of
        private static ConcurrentQueue<WiimoteDataSender> NewWiimoteQueue;

        // ------------- RAW HIDAPI INTERFACE ------------- //

        /// \brief Attempts to find connected Wii Remotes, Wii Remote Pluses or Wii U Pro Controllers
        /// \return If any new remotes were found.
        public static bool FindWiimotes()
        {
            bool ret = _FindWiimotes(WiimoteType.WIIMOTE);
            ret = ret || _FindWiimotes(WiimoteType.WIIMOTEPLUS);
            return ret;
        }

        private static bool _FindWiimotes(WiimoteType type)
        {
            //if (hidapi_wiimote != IntPtr.Zero)
            //    HIDapi.hid_close(hidapi_wiimote);

            ushort vendor = 0;
            ushort product = 0;

            if (type == WiimoteType.WIIMOTE)
            {
                vendor = vendor_id_wiimote;
                product = product_id_wiimote;
            }
            else if (type == WiimoteType.WIIMOTEPLUS || type == WiimoteType.PROCONTROLLER)
            {
                vendor = vendor_id_wiimote;
                product = product_id_wiimoteplus;
            }

            IntPtr ptr = HIDapi.hid_enumerate(vendor, product);
            IntPtr cur_ptr = ptr;

            if (ptr == IntPtr.Zero)
                return false;

            hid_device_info enumerate = (hid_device_info)Marshal.PtrToStructure(ptr, typeof(hid_device_info));

            bool found = false;

            while (cur_ptr != IntPtr.Zero)
            {
                Wiimote remote = null;
                bool fin = false;
                foreach (Wiimote r in Wiimotes)
                {
                    if (fin)
                        continue;

                    if (r.hidapi_path.Equals(enumerate.path))
                    {
                        remote = r;
                        fin = true;
                    }
                }
                if (remote == null)
                {
                    IntPtr handle = HIDapi.hid_open_path(enumerate.path);

                    HIDapi.hid_set_nonblocking(handle, 1);

                    WiimoteType trueType = type;

                    // Wii U Pro Controllers have the same identifiers as the newer Wii Remote Plus except for product
                    // string (WHY nintendo...)
                    if (enumerate.product_string.EndsWith("UC"))
                        trueType = WiimoteType.PROCONTROLLER;

                    WiimoteDataSender dataSender = new WiimoteDataSender(handle);

                    remote = new Wiimote(handle, enumerate.path, trueType, dataSender);

                    if (Debug_Messages)
                        Debug.Log("Found New Remote: " + remote.hidapi_path);

                    Wiimotes.Add(remote);

                    remote.SendDataReportMode(InputDataType.REPORT_BUTTONS);
                    remote.SendStatusInfoRequest();

                    if (NewWiimoteQueue == null){
                        NewWiimoteQueue = new ConcurrentQueue<WiimoteDataSender>();
                        NewWiimoteQueue.Enqueue(dataSender);

                        Thread writeThread = new Thread(new ParameterizedThreadStart(SendThread));
                        writeThread.Start(NewWiimoteQueue);
                        // no need to save a reference to the thread
                    } else {
                        NewWiimoteQueue.Enqueue(dataSender);
                    }
                }

                cur_ptr = enumerate.next;
                if (cur_ptr != IntPtr.Zero)
                    enumerate = (hid_device_info)Marshal.PtrToStructure(cur_ptr, typeof(hid_device_info));
            }

            HIDapi.hid_free_enumeration(ptr);

            return found;
        }

        public static void SetWiimoteOrder(List<Wiimote> orderedWiimotes)
        {
            if(_Wiimotes.Count == orderedWiimotes.Count)
                _Wiimotes = orderedWiimotes;
        }

        /// \brief Disables the given \c Wiimote by closing its bluetooth HID connection.  Also removes the remote from Wiimotes
        /// \param remote The remote to cleanup
        public static void Cleanup(Wiimote remote)
        {
            if (remote != null)
            {
                remote.DataSender.should_exit = true;

                Wiimotes.Remove(remote);

                if (Wiimotes.Count == 0){
                    NewWiimoteQueue = null;
                }
            }
        }

        /// \return If any Wii Remotes are connected and found by FindWiimote
        public static bool HasWiimote()
        {
            return !(Wiimotes.Count <= 0 || Wiimotes[0] == null || Wiimotes[0].hidapi_handle == IntPtr.Zero);
        }

        
        private static void SendThread(object newWiimotesQueueObject)
        {
            ConcurrentQueue<WiimoteDataSender> newWiimotesQueue = (ConcurrentQueue<WiimoteDataSender>)newWiimotesQueueObject;
            List<WiimoteDataSender> wiimotes = new List<WiimoteDataSender>(1);
            long goalTimeMs = 0;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                // collect new wiimotes
                while (newWiimotesQueue.TryDequeue(out WiimoteDataSender newWiimote)){
                    wiimotes.Add(newWiimote);
                }

                // update all wiimotes
                for (int i = 0; i < wiimotes.Count; ++i){
                    var wiimote = wiimotes[i];
                    if (wiimote.should_exit){
                        HIDapi.hid_close(wiimote.hidapi_wiimote);
                        wiimote.hidapi_wiimote = (IntPtr)0;
                        wiimotes.RemoveAt(i);
                        i -= 1;
                        continue;
                    }

                    wiimote.UpdateOnThread();
                }

                if (wiimotes.Count == 0)
                    break;

                goalTimeMs += MaxWriteFrequency;
                Thread.Sleep((int)(goalTimeMs - timer.ElapsedMilliseconds));
            }

            Debug.Log("Send thread exiting");
        }

    }
} // namespace WiimoteApi