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
  VR version of script for running the simulation and manipulating certain
  aspects of it. Added functions for controlling playback speed of animation
  (left and right on the touchpad of the right controller), rewinding/playing
  (up and down on the touchpad of the right controller) and for toggling the
  transparency of the brain and skull on and off (trigger on the back of the
  left controller). Some additional functions exist in the script but were not
  working as intended before user tests were held and therefore not used.
*/

public class VRSimulator : BaseSimulator
{
    private float densityVisSpeed = 0.2f;
    
    [Header("Marker")]
    public Material transparentMat;
    public Material solidMat;
    public Vector3 markerDist;
    public float markerSizeMin = 0.0001f;
    public float markerSizeMax = 0.1f;
    
    [Header("LEFT VR Settings")]
    public Transform leftVRController;
    public GameObject leftController;

    public float LleftSens = -0.8f;
    public float LrightSens = 0.7f;
    public float LupSens = 0.6f;
    public float LdownSens = -0.6f;
    private float preLtrackpad = 0;
    private float preMarkerBool = 0;

    [SerializeField] private InputActionReference spaceController = null;
    [SerializeField] private InputActionReference toggleTransparent = null; 
    [SerializeField] private InputActionReference LtrackClick = null;
    [SerializeField] private InputActionReference LPlacerMarker = null;

    [Header("Right VR Settings")]
    public Transform rightVRController;
    private bool transparencyMarkerEnabled;
    public float RleftSens = -0.7f;
    public float RrightSens = 0.7f;
    public float RupSens = 0.6f;
    public float RdownSens = -0.6f;
    private float prePlayPauseBool = 0;
    private float preRtrackpad = 0;    
    
    [SerializeField] private InputActionReference togglePlay = null;
    [SerializeField] private InputActionReference timeController = null;
    [SerializeField] private InputActionReference annotatePoint = null;
    [SerializeField] private InputActionReference RtrackClick = null;

    private bool transparencyToggleInProgress = false; // Todo: Is this actualy unused/superficient?

    override protected void handleInput()
    {
        // Handle VR input LEFT
        Vector2 timeInputVal = timeController.action.ReadValue<Vector2>();

        float playPauseBool = togglePlay.action.ReadValue<float>();
        float Rtrackpad = RtrackClick.action.ReadValue<float>();
        float placerMarkerBool = LPlacerMarker.action.ReadValue<float>();

        // Handle VR input RIGHT
        Vector2 spaceInputVal = spaceController.action.ReadValue<Vector2>();
        float toggleVal = toggleTransparent.action.ReadValue<float>();
        float Ltrackpad = LtrackClick.action.ReadValue<float>();
        
        // Start/Stop the User Study
        if (playPauseBool==1 && prePlayPauseBool==0)
        {
            if (paused == true)
            {
                paused = false;
            }
            else
            {
                paused = true;
            }
            
        }
        prePlayPauseBool = playPauseBool;
        
        if (timeInputVal != Vector2.zero && Rtrackpad == 1 && preRtrackpad == 0 &&
            timeInputVal.y < RupSens && timeInputVal.y > RdownSens && timeInputVal.x < RrightSens && timeInputVal.x > RleftSens)
        {
            if (paused)
            {
                paused = false;
            } else
            {
                paused = true;
            }
        }
        
        if (timeInputVal != Vector2.zero) // If there is input controlling the playback
        {
            if (applySpaceTimeDensity)
            {
                if (timeInputVal.x > RrightSens) // If there is input to increase the left visbility cutoff
                {
                    Vector2 visWindow = volObjScript.GetVisibilityWindow();
                    visWindow.x += densityVisSpeed * Time.deltaTime;
                    if (visWindow.x > visWindow.y) visWindow.x = visWindow.y;
                    volObjScript.SetVisibilityWindow(visWindow);
                }
                else if (timeInputVal.x < RleftSens) // If there is input to decrease the left visbility window cutoff
                {
                    Vector2 visWindow = volObjScript.GetVisibilityWindow();
                    visWindow.x -= densityVisSpeed * Time.deltaTime;
                    if (visWindow.x < 0.0f) visWindow.x = 0;
                    volObjScript.SetVisibilityWindow(visWindow);
                }

                if (timeInputVal.y > RupSens) // If there is input to increase the right visibility window cutoff
                {
                    Vector2 visWindow = volObjScript.GetVisibilityWindow();
                    visWindow.y += densityVisSpeed * Time.deltaTime;
                    if (visWindow.y > 1.0f) visWindow.y = 1;
                    volObjScript.SetVisibilityWindow(visWindow);
                }
                else if (timeInputVal.y < RdownSens) // If there is input to decrease the right visibility window cutoff
                {
                    Vector2 visWindow = volObjScript.GetVisibilityWindow();
                    visWindow.y -= densityVisSpeed * Time.deltaTime;
                    if (visWindow.y < visWindow.x) visWindow.y = visWindow.x;
                    volObjScript.SetVisibilityWindow(visWindow);
                }
            }
            else
            {
                if (timeInputVal.x > RrightSens) // If there is input to switch to forward playback
                {
                    rewind = false;
                    forward = true;
                }
                else if (timeInputVal.x < RleftSens) // If there is input to rewind
                {
                    rewind = true;
                    forward = false;
                }

                if (timeInputVal.y > RupSens) // If there is input to increase the playback speed
                {
                    if (playBackSpeed < maxPlaybackSpeed)
                    {
                        if (paused)
                        {
                            paused = false;
                        }
                        playBackSpeed += maxPlaybackSpeed / 2f * timeInputVal.y * Time.deltaTime;
                    }
                }
                else if (timeInputVal.y < RdownSens) // If there is input to reduce the playback speed
                {
                    if (playBackSpeed > 1f)
                    {
                        playBackSpeed -= maxPlaybackSpeed / 2f * -timeInputVal.y * Time.deltaTime;
                    }
                    else
                    {
                        paused = true;
                    }

                    if (playBackSpeed < 0.1f && !paused)
                    {
                        playBackSpeed = 0.5f;
                    }


                    //Debug.Log(playBackSpeed);
                }
            }

            //Debug.Log("Forward: " + forward + "| Backward: " + rewind + "| Playback speed: " + playBackSpeed);

        }
        //Debug.Log("b " + toggleVal);

        // Start/Stop the User Study
        if (toggleVal > 0.8f && !transparencyToggleInProgress) //If we got input to toggle
        {
            //Debug.Log(toggleVal);
            transparencyToggleInProgress = true;

            if (!collectionStarted) startCollectingData();
            else endCollectData();
        }
        else if (toggleVal < 0.1f && transparencyToggleInProgress) // If we no longer have input to toggle
        {
            transparencyToggleInProgress = false;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Invoke(nameof(RestartScene), 1f); // Soft restart the scene (method in this script)
        }
        
        if (marker.activeSelf == false) //Init function for marker
        {

            marker.SetActive(true);
            marker.transform.position = leftController.transform.position + leftController.transform.rotation * new Vector3(0, 0, 0.05f);
            transparencyMarkerEnabled = false;
            ToggleMarkerTransparency();
        }
        if (transparencyMarkerEnabled) // Update markers position
        {
            
            marker.transform.position = leftController.transform.position + leftController.transform.rotation * markerDist;
        }
        //Debug.Log(spaceInputVal);
        if (placerMarkerBool != 0 && preMarkerBool == 0) // Switch between holding and placing the marker
        {
            ToggleMarkerTransparency();
        }
        preMarkerBool = placerMarkerBool;



        if (spaceInputVal != Vector2.zero && preLtrackpad == 0 && Ltrackpad == 1) // If there is input controlling the playback
        {


            if (spaceInputVal.x > LrightSens)
            {
                
                marker.transform.localScale += 0.001f * new Vector3(1, 1, 1);
                
            }
            else if (spaceInputVal.x < LleftSens)
            {
                marker.transform.localScale -= 0.001f * new Vector3(1, 1, 1);
            }

            if (marker.transform.localScale.x < 0.001)
            {

                marker.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            }
            else if (marker.transform.localScale.x > markerSizeMax)
            {
                marker.transform.localScale = new Vector3(markerSizeMax, markerSizeMax, markerSizeMax);
            }



        }
        preLtrackpad = Ltrackpad;
    }
    
    void ToggleMarkerTransparency()
    {
        Renderer markerRendererR = marker.GetComponent<Renderer>();
        if (!transparencyMarkerEnabled)
        {
            //markerDist = this.transform.position - marker.transform.position;
            //marker.transform.position = this.transform.position + transform.rotation * markerDist;
            markerDist = leftController.transform.position - marker.transform.position;
            markerDist = new Vector3(0, 0, markerDist.magnitude);
            markerTransparent();
            transparencyMarkerEnabled = true;
        }
        else if (transparencyMarkerEnabled)
        {
            markerSolid();
            transparencyMarkerEnabled = false;

        }
    }


    void markerTransparent()
    {
        Renderer skullRendererE = marker.GetComponent<Renderer>();
        skullRendererE.material = transparentMat;
    }
    void markerSolid()
    {
        Renderer skullRendererE = marker.GetComponent<Renderer>();
        skullRendererE.material = solidMat;
    }
}
