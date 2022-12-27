using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.UI;
using JetBrains.Annotations;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/*
    Base script for running the simulation.
    Offers functionality for reading in data, animating the data and toggling the transparency of
    the brain and skull. Adapted from the VRSimulator written by Marcus Holmberg, Måns Nyman, Léa Prémont
    and Mahmoud Sherzad.
*/
public class BaseSimulator : MonoBehaviour
{
    protected  class Data
    {
        // The path to the tsv file the data was loaded from
        public string path;

        // Position and size of data
        public int index;
        public int fileSize;

        // The reference markers instantiated for this data
        public GameObject cathTop;
        public GameObject cathTL;
        public GameObject cathTR;
        public GameObject cathBL;
        public GameObject cathBR;
        public GameObject skullTL;
        public GameObject skullTR;
        public GameObject skullBL;
        public GameObject skullBR;
        public GameObject skullBrow;

        // References to the barycenters of all of the markers
        public GameObject cathCenter;
        public GameObject skullCenter;
        public Quaternion cathCenterRot;
        public Quaternion skullCenterRot;

        public Vector3 cathRight = Vector3.one;
        public Vector3 cathUp = Vector3.one;

        //arrays with data from each row
        public float[] field, time;
        public float[,] headTopLeft, headTopRight, headBottomLeft, headBottomRight, headHole, headBrow,
                cathTip, cathTopLeft, cathTopRight, cathBottomLeft, cathBottomRight, cathEnd;

        //coordinates for 3D objects transform update
        public float x1, x2, x3, x4, x5, x6, x7, x8, x9, x10,
            y1, y2, y3, y4, y5, y6, y7, y8, y9, y10,
            z1, z2, z3, z4, z5, z6, z7, z8, z9, z10;

        public bool moreData;
    }

    [Header("Reference Markers Prefab")]
    public GameObject markers;

    [Header("GUI")]
    public Text[] FrameStuff;
    public Slider slider; //slider to control the animation speed

    [Header("Simulation")]
    public string[] paths; //"Assets/Recordings/catheter005.txt"; // List of paths to be loaded
    public float maxPlaybackSpeed = 50f;
    protected float timeToCall;
    protected float timeDelay = 1.0f; //the code will be run every 2 seconds
    protected const string separator = "\t"; //tab separation string
    protected int index = 0;
    protected bool paused;
    protected bool rewind;
    protected bool forward;
    protected float playBackSpeed = 1f;
    protected float timer = 0;
    protected bool transparencyEnabled;
    protected List<Data> dataList;

    [Header("Rendered Representation")] //TODO: Move into data?
    public GameObject phantomSkull;
    public GameObject phantomBrain;
    protected Material solidSkullMat;
    protected Material solidBrainMat;

    public Material transparentSkullMat;
    public Material transparentBrainMat;

    //Custom transform coordinates for the skull
    protected Dictionary<String, Vector3> skullOffsetPos = new Dictionary<String, Vector3> {
        {"Assets/Recordings/catheter001.txt",new Vector3(-0.939999998f,-14.1099997f,5.55000019f)}, //file : cathether001 NOT WELL ALIGNED
        {"Assets/Recordings/catheter002.txt", new Vector3(-1.88f,-13.8599997f,4.67000008f) }, //file : cathether002 NOT WELL ALIGNED
        {"Assets/Recordings/catheter003.txt", new Vector3(-1.10000002f,-14.1099997f,6.32000017f)}, //file : cathether003
        {"Assets/Recordings/catheter004.txt",new Vector3(-1.28999996f,-13.4799995f,6.07999992f) }, //file : cathether004 NOT WELL ALIGNED
        {"Assets/Recordings/catheter005.txt",new Vector3(-1.08000004f,-13.6199999f,6.5f) }, //file : cathether005
        {"Assets/Recordings/catheter006.txt",new Vector3(-0.639999986f,-12.6899996f,5.57000017f) }, //file : cathether006
        {"Assets/Recordings/catheter007.txt",new Vector3(-0.850000024f,-14.1099997f,5.6500001f) } //file : cathether007
        };
    protected Dictionary<String, Vector3> skullOffsetRot = new Dictionary<String, Vector3> {
        {"Assets/Recordings/catheter001.txt",new Vector3(38.116478f,177.862823f,358.404968f)}, //file : cathether001
        {"Assets/Recordings/catheter002.txt", new Vector3(42.3742065f,181.589996f,3.92515182f) }, //file : cathether002
        {"Assets/Recordings/catheter003.txt", new Vector3(43.9130974f,177.666306f,358.909271f) }, //file : cathether003
        {"Assets/Recordings/catheter004.txt",new Vector3(42.3742065f,181.589996f,3.92515182f) }, //file : cathether004
        {"Assets/Recordings/catheter005.txt",new Vector3(42.3742065f,181.589996f,3.92515182f) }, //file : cathether005
        {"Assets/Recordings/catheter006.txt",new Vector3(42.3742104f,181.589996f,5.4209547f) }, //file : cathether006
        {"Assets/Recordings/catheter007.txt",new Vector3(41.510006f,177.755005f,359.040009f) } //file : cathether007
        };

    protected virtual void init()
    {
        // Should be overriden if a specific implementation needs to initialise something
    }

    // Start is called before the first frame update
    void Start()
    {
        // Initialize Simulation parameters
        paused = false;
        rewind = false;
        forward = true;
        timeToCall = timeDelay;

        // Read in all specified data
        dataList = new List<Data>();
        foreach (String path in paths)
        {
            dataList.Add(extractData(path));
        }

        init();

        // Sets the skull and brain to have the solid material
        SetInitialColors();
    }

    protected virtual void handleInput()
    {
        // Should be overriden with the implementation specific controlls
    }

    protected void Update()
    {
        handleInput();

        // The slider visible in the scene displays current playback speed
        if (slider)
        {
            slider.value = playBackSpeed;
        }
    }

    // Simulate the data in specific data set. Should only be called after checking externaly that we have a new simulation frame and aren't paused!
    protected void simulateData(Data data)
    {
        // If the markers are set, we have data, we still have more data
        if (MarkerCheck(data) && data.fileSize > 0 && data.moreData)
        {

            //normalize positions
            Normalize(data);

            //update marker positions
            data.cathTop.transform.position = new Vector3(data.x1, data.y1, data.z1);
            data.cathTL.transform.position = new Vector3(data.x2, data.y2, data.z2);
            data.cathTR.transform.position = new Vector3(data.x3, data.y3, data.z3);
            data.cathBL.transform.position = new Vector3(data.x4, data.y4, data.z4);
            data.cathBR.transform.position = new Vector3(data.x5, data.y5, data.z5);
            data.skullTL.transform.position = new Vector3(data.x6, data.y6, data.z6);
            data.skullTR.transform.position = new Vector3(data.x7, data.y7, data.z7);
            data.skullBL.transform.position = new Vector3(data.x8, data.y8, data.z8);
            data.skullBR.transform.position = new Vector3(data.x9, data.y9, data.z9);
            data.skullBrow.transform.position = new Vector3(data.x10, data.y10, data.z10);

            if (rewind && data.index > 0)
            {
                data.index--;
            }

            if (forward)
            {
                data.index++;
            }

            if (data.index >= data.fileSize)
            {
                data.moreData = false; //stop simulation if eod is reached
                //Invoke(nameof(RestartScene), 1f); // Soft restart the scene (method in this script)
            }

            // Update timer for the next frame
            timer = 0f;
            timeToCall = timeDelay / playBackSpeed;

            // Place the reference model by the markers
            AlignModels(data);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        timer += Time.deltaTime;

        // If it's the next simulation frame, and we are not paused
        if (timer >= timeToCall && !paused)
        {
            // Simulate each data series
            foreach (Data data in dataList)
            {
                simulateData(data);
            }

            // Update the GUI
            if (FrameStuff[0])
            {
                FrameStuff[0].text = "Current frame: " + index;
            }
        }
    }

    private void extractDataMarkersHelper(Data data, GameObject ourMarkers)
    {
        int numChildren = ourMarkers.transform.childCount;
        for (int i = 0; i < numChildren; i++) {
            GameObject child = ourMarkers.transform.GetChild(i).gameObject;

            switch (child.name)
            {
                case "CathTop":
                    data.cathTop = child;
                    break;
                case "CathTL":
                    data.cathTL = child;
                    break;
                case "CathTR":
                    data.cathTR = child;
                    break;
                case "CathBL":
                    data.cathBL = child;
                    break;
                case "CathBR":
                    data.cathBR = child;
                    break;
                case "SkullTL":
                    data.skullTL = child;
                    break;
                case "SkullTR":
                    data.skullTR = child;
                    break;
                case "SkullBL":
                    data.skullBL = child;
                    break;
                case "SkullBR":
                    data.skullBR = child;
                    break;
                case "SkullBrow":
                    data.skullBrow = child;
                    break;
                case "CathCenter":
                    data.cathCenter = child;
                    //initialize offset for 3dmodels
                    data.cathCenterRot = data.cathCenter.transform.rotation;
                    break;
                case "SkullCenter":
                    data.skullCenter = child;
                    //initialize offset for 3dmodels
                    data.skullCenterRot = data.skullCenter.transform.rotation;
                    break;
                default:
                    Debug.Log("When reading " + data.path + ": Instantiated marker contained unknown child " + child.name);
                    break;
            }
        }
    }

    /*
     * Reads in the data available at path and returns an object holding the data
     * which can be used for playback.
     */ 
    protected Data extractData(string path)
    {
        Data data = new Data();

        data.path = path;
        data.index = 0;
        data.fileSize = 0;

        // Buffer the data at path in memory
        StreamReader sr = ReadFile(data.path); //read from file
        data.fileSize = FindSize(sr); //find size of file

        // Initialize the markers for this data
        GameObject ourMarkers = Instantiate(markers);
        extractDataMarkersHelper(data, ourMarkers);

        //initialize arrays
        data.field = data.time = new float[data.fileSize];
        data.headTopLeft = new float[data.fileSize, 3];
        data.headTopRight = new float[data.fileSize, 3];
        data.headBottomLeft = new float[data.fileSize, 3];
        data.headBottomRight = new float[data.fileSize, 3];
        data.headHole = new float[data.fileSize, 3];
        data.headBrow = new float[data.fileSize, 3];
        data.cathTip = new float[data.fileSize, 3];
        data.cathTopLeft = new float[data.fileSize, 3];
        data.cathTopRight = new float[data.fileSize, 3];
        data.cathBottomLeft = new float[data.fileSize, 3];
        data.cathBottomRight = new float[data.fileSize, 3];
        data.cathEnd = new float[data.fileSize, 3];

        //extract and distribute info
        sr.DiscardBufferedData();
        sr.BaseStream.Seek(0, SeekOrigin.Begin);
        Extract(sr, data); // Reads in all data and places it in the relevant arrays
        data.moreData = true;

        //close reader
        sr.Close();

        //set offset of skull depending on recording
        data.skullCenter.gameObject.transform.GetChild(0).transform.localPosition = skullOffsetPos[path];
        data.skullCenter.gameObject.transform.GetChild(0).transform.localEulerAngles = skullOffsetRot[path];

        return data;
    }

    //Method to display the models of catheter and skull according to the markers positions
    private void AlignModels(Data data)
    {
        //Align orientation of the catheter
        data.cathRight = data.cathTL.transform.position - data.cathTR.transform.position;
        data.cathUp = data.cathTR.transform.position - data.cathBR.transform.position - Vector3.Project(data.cathTR.transform.position - data.cathBR.transform.position, data.cathRight);
        data.cathCenter.transform.rotation = Quaternion.LookRotation(data.cathRight, data.cathUp);
        //Align positions at the barycenter
        data.cathCenter.transform.position = new Vector3((data.x1 + data.x2 + data.x3 + data.x4 + data.x5) / 5.0f, (data.y1 + data.y2 + data.y3 + data.y4 + data.y5) / 5.0f, (data.z1 + data.z2 + data.z3 + data.z4 + data.z5) / 5.0f);
        data.skullCenter.transform.position = new Vector3((data.x6 + data.x7 + data.x8 + data.x9 + data.x10) / 5.0f, (data.y6 + data.y7 + data.y8 + data.y9 + data.y10) / 5.0f, (data.z6 + data.z7 + data.z8 + data.z9 + data.z10) / 5.0f);

    }

    //method to normalize coordinates in Unity scene
    private void Normalize(Data data)
    {
        int index = data.index;

        //x coordinate
        data.x1 = data.cathTip[index, 0] / 1000.0f;
        data.x2 = data.cathTopLeft[index, 0] / 1000.0f;
        data.x3 = data.cathTopRight[index, 0] / 1000.0f;
        data.x4 = data.cathBottomLeft[index, 0] / 1000.0f;
        data.x5 = data.cathBottomRight[index, 0] / 1000.0f;
        data.x6 = data.headTopLeft[index, 0] / 1000.0f;
        data.x7 = data.headTopRight[index, 0] / 1000.0f;
        data.x8 = data.headBottomLeft[index, 0] / 1000.0f;
        data.x9 = data.headBottomRight[index, 0] / 1000.0f;
        data.x10 = data.headBrow[index, 0] / 1000.0f;

        //y coordinate
        data.y1 = data.cathTip[index, 1] / 1000.0f;
        data.y2 = data.cathTopLeft[index, 1] / 1000.0f;
        data.y3 = data.cathTopRight[index, 1] / 1000.0f;
        data.y4 = data.cathBottomLeft[index, 1] / 1000.0f;
        data.y5 = data.cathBottomRight[index, 1] / 1000.0f;
        data.y6 = data.headTopLeft[index, 1] / 1000.0f;
        data.y7 = data.headTopRight[index, 1] / 1000.0f;
        data.y8 = data.headBottomLeft[index, 1] / 1000.0f;
        data.y9 = data.headBottomRight[index, 1] / 1000.0f;
        data.y10 = data.headBrow[index, 1] / 1000.0f;

        //z coordinate
        data.z1 = data.cathTip[index, 2] / 1000.0f;
        data.z2 = data.cathTopLeft[index, 2] / 1000.0f;
        data.z3 = data.cathTopRight[index, 2] / 1000.0f;
        data.z4 = data.cathBottomLeft[index, 2] / 1000.0f;
        data.z5 = data.cathBottomRight[index, 2] / 1000.0f;
        data.z6 = data.headTopLeft[index, 2] / 1000.0f;
        data.z7 = data.headTopRight[index, 2] / 1000.0f;
        data.z8 = data.headBottomLeft[index, 2] / 1000.0f;
        data.z9 = data.headBottomRight[index, 2] / 1000.0f;
        data.z10 = data.headBrow[index, 2] / 1000.0f;
    }

    //function to check if objects assigned to markers are not null
    private bool MarkerCheck(Data data)
    {
        if (data.cathTop != null && data.cathTL != null && data.cathTR != null && data.cathBL != null && data.cathBR != null
            && data.skullBL != null && data.skullBR != null && data.skullTL != null && data.skullTR != null && data.skullBrow != null)
            return true;
        else return false;
    }

    //function to read the file with recorded MoCap data
    private StreamReader ReadFile(string path)
    {
        StreamReader reader = new StreamReader(path);
        string line = reader.ReadLine(); //first line = headers
        return reader;
    }

    //function to find the total number of lines in the file being read
    private int FindSize(StreamReader reader)
    {
        int i = 1;
        string line = reader.ReadLine();
        while (line != null)
        {
            i++;
            line = reader.ReadLine();
        }
        return i;
    }

    //method to extract coordinates from the file being read
    private void Extract(StreamReader reader, Data data)
    {
        string line;
        for (int i = 0; i < 1; i++)         // change to i<5 for catheter_008 (Alexander: I removed the extra header lines and used the regular header instead; no need to make this change)
            line = reader.ReadLine(); //skip headers
        line = reader.ReadLine(); //first line

        //extract info and distribute
        while (line != null && line != "") //interrupt at empty line or end of file
        {
            string[] temp = line.Split(separator.ToCharArray());
            //string[] temp = line.Split("\t");
            int runtimeField = Int32.Parse(temp[0]); //current array id

            //Debug.Log(runtimeField);

            //populate arrays
            data.field[runtimeField] = runtimeField + 1.0f;
            data.time[runtimeField] = runtimeField / 100.0f;


            //marker tree attached to the skull

            //float test = float.Parse(temp[2] + 36f);
            //Debug.Log(temp[2]);

            data.headTopLeft[runtimeField, 0] = float.Parse(temp[2], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //skull 1 x
            data.headTopLeft[runtimeField, 1] = float.Parse(temp[4], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //skull 1 y
            data.headTopLeft[runtimeField, 2] = float.Parse(temp[3], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //skull 1 z
            data.headTopRight[runtimeField, 0] = float.Parse(temp[5], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //skull 2 x
            data.headTopRight[runtimeField, 1] = float.Parse(temp[7], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //skull 2 y
            data.headTopRight[runtimeField, 2] = float.Parse(temp[6], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //skull 2 z
            data.headBottomLeft[runtimeField, 0] = float.Parse(temp[8], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //skull 3 x
            data.headBottomLeft[runtimeField, 1] = float.Parse(temp[10], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //skull 3 y
            data.headBottomLeft[runtimeField, 2] = float.Parse(temp[9], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //skull 3 z
            data.headBottomRight[runtimeField, 0] = float.Parse(temp[11], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //skull 4 x
            data.headBottomRight[runtimeField, 1] = float.Parse(temp[13], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //skull 4 y
            data.headBottomRight[runtimeField, 2] = float.Parse(temp[12], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //skull 4 z

            /* calibration marker on burr hole is always 0
            headHole[runtimeField, 0] = float.Parse(temp[14]); //burr hole x
            headHole[runtimeField, 1] = float.Parse(temp[16]); //burr hole y
            headHole[runtimeField, 2] = float.Parse(temp[15]); //burr hole z
            */

            //marker attached to the skull brow
            /* headBrow[runtimeField, 0] = float.Parse(temp[17], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //skull brow x
            headBrow[runtimeField, 1] = float.Parse(temp[19], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //skull brow y
            headBrow[runtimeField, 2] = float.Parse(temp[18], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //skull brow z

            //marker tree attached to the catheter
            cathTip[runtimeField, 0] = float.Parse(temp[20], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 1 x
            cathTip[runtimeField, 1] = float.Parse(temp[22], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 1 y
            cathTip[runtimeField, 2] = float.Parse(temp[21], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 1 z
            cathTopLeft[runtimeField, 0] = float.Parse(temp[23], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 2 x
            cathTopLeft[runtimeField, 1] = float.Parse(temp[25], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 2 y
            cathTopLeft[runtimeField, 2] = float.Parse(temp[24], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 2 z
            cathTopRight[runtimeField, 0] = float.Parse(temp[26], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 3 x
            cathTopRight[runtimeField, 1] = float.Parse(temp[28], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 3 y
            cathTopRight[runtimeField, 2] = float.Parse(temp[27], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 3 z
            cathBottomLeft[runtimeField, 0] = float.Parse(temp[29], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 4 x
            cathBottomLeft[runtimeField, 1] = float.Parse(temp[31], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 4 y
            cathBottomLeft[runtimeField, 2] = float.Parse(temp[30], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 4 z
            cathBottomRight[runtimeField, 0] = float.Parse(temp[32], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 5 x
            cathBottomRight[runtimeField, 1] = float.Parse(temp[34], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 5 y
            cathBottomRight[runtimeField, 2] = float.Parse(temp[33], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 5 z
            */
            data.headBrow[runtimeField, 0] = float.Parse(temp[14], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //skull brow x
            data.headBrow[runtimeField, 1] = float.Parse(temp[16], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //skull brow y
            data.headBrow[runtimeField, 2] = float.Parse(temp[15], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //skull brow z

            //marker tree attached to the catheter
            data.cathTip[runtimeField, 0] = float.Parse(temp[17], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 1 x
            data.cathTip[runtimeField, 1] = float.Parse(temp[19], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 1 y
            data.cathTip[runtimeField, 2] = float.Parse(temp[18], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 1 z
            data.cathTopLeft[runtimeField, 0] = float.Parse(temp[20], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 2 x
            data.cathTopLeft[runtimeField, 1] = float.Parse(temp[22], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 2 y
            data.cathTopLeft[runtimeField, 2] = float.Parse(temp[21], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 2 z
            data.cathTopRight[runtimeField, 0] = float.Parse(temp[23], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 3 x
            data.cathTopRight[runtimeField, 1] = float.Parse(temp[25], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 3 y
            data.cathTopRight[runtimeField, 2] = float.Parse(temp[24], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 3 z
            data.cathBottomLeft[runtimeField, 0] = float.Parse(temp[26], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 4 x
            data.cathBottomLeft[runtimeField, 1] = float.Parse(temp[28], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 4 y
            data.cathBottomLeft[runtimeField, 2] = float.Parse(temp[27], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 4 z
            data.cathBottomRight[runtimeField, 0] = float.Parse(temp[29], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 5 x
            data.cathBottomRight[runtimeField, 1] = float.Parse(temp[31], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 5 y
            data.cathBottomRight[runtimeField, 2] = float.Parse(temp[30], System.Globalization.CultureInfo.InvariantCulture.NumberFormat); //catheter 5 z

            /* calibration marker on catheter tip is always 0
            cathEnd[runtimeField, 0] = float.Parse(temp[32]); //catheter tip x
            cathEnd[runtimeField, 1] = float.Parse(temp[34]); //catheter tip y
            cathEnd[runtimeField, 2] = float.Parse(temp[33]); //catheter tip z
            */

            line = reader.ReadLine();
        }
    }

    protected void RestartScene()
    {
        Debug.Log("Restart");
        //SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        foreach(Data data in dataList)
        {
            data.index = 0;
            data.moreData = true;
        }

        timer = 0;
    }

    protected void ToggleTransparency()
    {
        Debug.Log("Transparency Enabled: " + transparencyEnabled);
        Renderer skullRenderer = phantomSkull.GetComponent<Renderer>();
        if (!transparencyEnabled)
        {
            SkullTransparent();
            BrainTransparent();
            transparencyEnabled = true;
        }
        else if (transparencyEnabled)
        {
            SkullSolid();
            BrainSolid();
            transparencyEnabled = false;
        }
    }
    protected void SetInitialColors()
    {
        Renderer skullRenderer = phantomSkull.GetComponent<Renderer>();
        solidSkullMat = skullRenderer.material;
        Renderer brainRenderer = phantomBrain.GetComponent<Renderer>();
        solidBrainMat = brainRenderer.material;
    }
    private void SkullTransparent()
    {
        Renderer skullRenderer = phantomSkull.GetComponent<Renderer>();
        skullRenderer.material = transparentSkullMat;
    }
    private void BrainTransparent()
    {
        Renderer brainRenderer = phantomBrain.GetComponent<Renderer>();
        brainRenderer.material = transparentBrainMat;
    }

    private void SkullSolid()
    {
        Renderer skullRenderer = phantomSkull.GetComponent<Renderer>();
        skullRenderer.material = solidSkullMat;
    }


    private void BrainSolid()
    {
        Renderer brainRenderer = phantomBrain.GetComponent<Renderer>();
        brainRenderer.material = solidBrainMat;
    }

    public int GetCurrentIndex()
    {
        return index;
    }
}
