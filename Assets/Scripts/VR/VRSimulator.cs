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
    
    [Header("VR Settings")]
    public Transform leftVRController;

    public float leftSens = -0.8f;
    public float rightSens = 0.7f;
    public float upSens = 0.6f;
    public float downSens = -0.6f;
    
    [SerializeField] private InputActionReference timeController = null;
    [SerializeField] private InputActionReference transparencyController = null;
    [SerializeField] private InputActionReference toggleTransparent = null;
    [SerializeField] private InputActionReference annotatePoint = null;

    private bool transparencyToggleInProgress = false; // Todo: Is this actualy unused/superficient?

    override protected void handleInput()
    {
        // Handle VR input
        Vector2 timeInputVal = timeController.action.ReadValue<Vector2>();
        Vector2 transparencyInputVal = transparencyController.action.ReadValue<Vector2>();
        float toggleVal = toggleTransparent.action.ReadValue<float>();

        if (timeInputVal != Vector2.zero) // If there is input controlling the playback
        {
            if (applySpaceTimeDensity)
            {
                if (timeInputVal.x > rightSens) // If there is input to increase the left visbility cutoff
                {
                    Vector2 visWindow = volObjScript.GetVisibilityWindow();
                    visWindow.x += densityVisSpeed * Time.deltaTime;
                    if (visWindow.x > visWindow.y) visWindow.x = visWindow.y;
                    volObjScript.SetVisibilityWindow(visWindow);
                }
                else if (timeInputVal.x < leftSens) // If there is input to decrease the left visbility window cutoff
                {
                    Vector2 visWindow = volObjScript.GetVisibilityWindow();
                    visWindow.x -= densityVisSpeed * Time.deltaTime;
                    if (visWindow.x < 0.0f) visWindow.x = 0;
                    volObjScript.SetVisibilityWindow(visWindow);
                }

                if (timeInputVal.y > upSens) // If there is input to increase the right visibility window cutoff
                {
                    Vector2 visWindow = volObjScript.GetVisibilityWindow();
                    visWindow.y += densityVisSpeed * Time.deltaTime;
                    if (visWindow.y > 1.0f) visWindow.y = 1;
                    volObjScript.SetVisibilityWindow(visWindow);
                }
                else if (timeInputVal.y < downSens) // If there is input to decrease the right visibility window cutoff
                {
                    Vector2 visWindow = volObjScript.GetVisibilityWindow();
                    visWindow.y -= densityVisSpeed * Time.deltaTime;
                    if (visWindow.y < visWindow.x) visWindow.y = visWindow.x;
                    volObjScript.SetVisibilityWindow(visWindow);
                }
            }
            else
            {
                if (timeInputVal.x > rightSens) // If there is input to switch to forward playback
                {
                    rewind = false;
                    forward = true;
                }
                else if (timeInputVal.x < leftSens) // If there is input to rewind
                {
                    rewind = true;
                    forward = false;
                }

                if (timeInputVal.y > upSens) // If there is input to increase the playback speed
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
                else if (timeInputVal.y < downSens) // If there is input to reduce the playback speed
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


                    Debug.Log(playBackSpeed);
                }
            }
            
            //Todo: The input values might need to be adjusted based on hardware
            if (timeInputVal.x > 0.7) // If there is input to switch to forward playback
            {
                rewind = false;
                forward = true;
            }
            else if (timeInputVal.x < -0.8f) // If there is input to rewind
            {
                rewind = true;
                forward = false;
            }

            if (timeInputVal.y > 0.6f) // If there is input to increase the playback speed
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
            else if (timeInputVal.y < -0.6f) // If there is input to reduce the playback speed
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


                Debug.Log(playBackSpeed);
            }

            //Debug.Log("Forward: " + forward + "| Backward: " + rewind + "| Playback speed: " + playBackSpeed);

        }
        Debug.Log("b " + toggleVal);

        if (toggleVal > 0.8f && !transparencyToggleInProgress) //If we got input to toggle transparency
        {
            Debug.Log(toggleVal);
            transparencyToggleInProgress = true;
            ToggleTransparency(); // Toggle transparency instantly
        }
        else if (toggleVal < 0.1f && transparencyToggleInProgress) // If we no longer have input to toggle transparency
        {
            transparencyToggleInProgress = false;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Invoke(nameof(RestartScene), 1f); // Soft restart the scene (method in this script)
        }
    }
}
