/*
///////////////////////////////////////////////////////////////////////////////
// This file details the implementation of the basic Translate action, which
// causes an object to interpolate their position between two given endpoints.
///////////////////////////////////////////////////////////////////////////////
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Translate : Action
{
    Vector3 start;
    Vector3 end;

    public Translate(Vector3 pStart,
                    Vector3 pEnd,
                    float pDelayLeft = 0.0f, 
                    float pDuration = 1.0f, 
                    GameObject pActionObject = null, 
                    bool pBlocking = false,
                    EaseType pEaseType = EaseType.Linear)
    {
        actionName = "Translate";
        
        start = pStart;
        end = pEnd;

        delayLeft = pDelayLeft;
        timePassed = 0.0f;
        duration = pDuration;
        actionObject = pActionObject;
        blocking = pBlocking;
        easeType = pEaseType;
        
        groupIDs = new List<int>();
        markedForErase = false;
        firstUpdate = true;
    }

    override public bool ActionUpdate(float dt)
    {
        timePassed += dt;

        // Interpolation is linear by default (Percent == dt); easeType affects
        // how the interpolation value changes with regards to action duration
        actionObject.transform.position = start + (end - start) * Percent();

        if(timePassed < duration)
            return true;
        else
        {
            // Ensures specific set position is exactly reached
            actionObject.transform.position = end; 

            return false;
        }
        
    }
}
