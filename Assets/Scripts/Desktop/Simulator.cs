using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.UI;
using JetBrains.Annotations;

/*
  Desktop version of script for running the simulation and manipulating certain
  aspects of it. Added functions for controlling playback speed of animation
  (up and down arrow keys), rewinding/playing (left and right arrows keys) and
  for toggling the transparency of the brain and skull on and off (Q key). Some
  additional functions exist in the script but were not working as intended
  before user tests were held and therefore not used.
*/


public class Simulator : BaseSimulator
{

    override protected void handleInput()
    {

        // Toggle transparency on and off with the Q key
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleTransparency();
        }
        if (transparencyEnabled) // Check if we want to adjust the transparency (Disabled?)
        {
            if (Input.GetKey(KeyCode.Mouse0))
            {
                // AdjustTransparency(10);
            }
            if (Input.GetKey(KeyCode.Mouse1))
            {
                //AdjustTransparency(-10);
            }
        }

        // Right arrow key plays the animation forward in time
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            rewind = false;
            forward = true;
        }
        // Left arrow key rewinds the animation
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            rewind = true;
            forward = false;
        }

        // Down arrow key reduces playback speed of the animation
        if (Input.GetKey(KeyCode.DownArrow))
        {
            if (playBackSpeed > 0f)
            {
                playBackSpeed -= maxPlaybackSpeed / 2f * Time.deltaTime;
            }
            else
            {
                paused = true;
            }

            if (playBackSpeed < 0.1f && !paused)
            {
                playBackSpeed = 0.5f;
            }
        }
        // Up arrow key increases playback speed of the animation
        else if (Input.GetKey(KeyCode.UpArrow))
        {
            if (playBackSpeed < maxPlaybackSpeed)
            {
                if (paused)
                {
                    paused = false;
                }
                playBackSpeed += maxPlaybackSpeed / 2f * Time.deltaTime;
            }
        }

        // R key restarts scene
        if (Input.GetKeyDown(KeyCode.R))
        {
            Invoke(nameof(RestartScene), 1f); // Soft restart the scene (method in this script)
        }
    }
}
