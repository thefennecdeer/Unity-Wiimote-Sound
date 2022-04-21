using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using WiimoteApi;

[System.Serializable]
public class StringEvent : UnityEvent<string>{}

public class WiimoteConnection : MonoBehaviour
{

    [SerializeField]
    [Tooltip("The type of data you want to recieve from the wiimotes")]
    private InputDataType inputDataType = InputDataType.REPORT_BUTTONS_ACCEL_EXT16;

    [SerializeField]
    private StringEvent feedBackTextChangeEvent = new StringEvent();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (WiimoteManager.HasWiimote())
        {
            LogWiiMoteData();
        }
    }

    [ContextMenu("FindWiimotes")]
    /// <summary>
    /// finds the number of wiimodes, if it matches the desired amount, it starts the calibration process
    /// </summary>
    public void FindWiimotes()
    {
        WiimoteManager.FindWiimotes();
        if (WiimoteManager.Wiimotes.Count == 0)
        {
            Log("Didn't find any wiimotes, make sure they are connected via bluetooth to the computer");
        }
        else
        {
            Log("Current number of Wiimotes connected: " + WiimoteManager.Wiimotes.Count);
            foreach (var wiimote in WiimoteManager.Wiimotes)
            {
                wiimote.SendDataReportMode(inputDataType);
                wiimote.SendPlayerLED(true, true, true, true); //set all the leds on to confirm connection
                Debug.Log("setting player LED");
            }
        }
    }

    [ContextMenu("StratWiimoteOrdering")]
    /// <summary>
    /// Activates the wiimote ordering process
    /// </summary>
    public void StartWiimoteOrdering()
    {
        StartCoroutine(SetWiimoteOrder());
    }

    /// <summary>
    /// Sets up de Wii motes with the correct reporting type and The player number
    /// </summary>
    IEnumerator SetWiimoteOrder()
    {
        Log("Please press the 'A' or '2' button in the order of the players");
        List<Wiimote> orderedWiimotes = new List<Wiimote>();

        while(orderedWiimotes.Count != WiimoteManager.Wiimotes.Count)
        {
            for (int i = 0; i < WiimoteManager.Wiimotes.Count; i++)
            {
                WiimoteManager.Wiimotes[i].ReadWiimoteData();

                //Debug.Log("Wiimote " + i + ", a button = " + WiimoteManager.Wiimotes[i].Button.a);

                if ((WiimoteManager.Wiimotes[i].Button.a || WiimoteManager.Wiimotes[i].Button.two) && !orderedWiimotes.Contains(WiimoteManager.Wiimotes[i]))
                {
                    orderedWiimotes.Add(WiimoteManager.Wiimotes[i]);
                    WiimoteManager.Wiimotes[i].SetWiimodeNr(orderedWiimotes.Count);
                    Log("Player " + orderedWiimotes.Count + " is set, " + (WiimoteManager.Wiimotes.Count - orderedWiimotes.Count) + " players remaining...");
                }
            }
            yield return null;
        }

        Log("Wiimote order set");
        WiimoteManager.SetWiimoteOrder(orderedWiimotes);
    }

    public void LogWiiMoteData()
    {
        for (int i = 0; i < WiimoteManager.Wiimotes.Count; i++)
        {
            int ret;

            do
            {
                ret = WiimoteManager.Wiimotes[i].ReadWiimoteData();

                if (ret > 0)
                {
                    ButtonData buttonData = WiimoteManager.Wiimotes[i].Button;

                    if (buttonData.a)  Debug.Log(i + ": A");
                    if (buttonData.b) Debug.Log(i + ": B");
                    if (buttonData.one) Debug.Log(i + ": 1");
                    if (buttonData.two) Debug.Log(i + ": 2");

                    //Vector3 RawData = new Vector3(WiimoteManager.Wiimotes[i].Accel.accel[0], WiimoteManager.Wiimotes[i].Accel.accel[1], WiimoteManager.Wiimotes[i].Accel.accel[2]);

                    Debug.Log(i + ": " + WiimoteManager.Wiimotes[i].Accel.GetAccelVector());
                    //Debug.Log(i + ":raw  " + RawData);
                }

            }
            while (ret > 0);
        }
    }

    private void Log(string value)
    {
        Debug.Log(value);
        feedBackTextChangeEvent.Invoke(value);
    }

    /// <summary>
    /// Clear the wiimotes so they can be reconnected later
    /// </summary>
    private void OnApplicationQuit()
    {
        int wiiMoteNr = WiimoteManager.Wiimotes.Count;

        for (int i = 0; i < wiiMoteNr; i++)
        {
            WiimoteManager.Cleanup(WiimoteManager.Wiimotes[0]);
        }

    }
}
