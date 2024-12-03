/*
///////////////////////////////////////////////////////////////////////////////
// The files within this sample represent either examples of or the direct
// implementations for core systems used to develop my solo "Gunship Project"
// in Unity. This file details the contents of the basic "Action" class, which
// is used to more easily program certain object behaviors.
///////////////////////////////////////////////////////////////////////////////
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Action
{
    // Different curves for progress functions
    public enum EaseType 
    {
        Linear, 
        FastExitSlowEntry, 
        SlowExitFastEntry,
        SlowEndFastMid
    };

    // Main action id properties
    public string actionName;
    public int actionID;
    public static int IDTracker = 0;

    // Core properties
    public float delayLeft;         // Time before action is performed
    public float timePassed;        // Curr time spent performing action
    public float duration;          // Max time before action is done
    public GameObject actionObject; // Object action is performed on
    public bool blocking;           // Blocks following actions from being performed
    public EaseType easeType;       // Interpolation curve for action progress

    public List<int> groupIDs;  // Used to block certain categories of actions
    public bool markedForErase; // Used by ActionList to delete irrelevant actions
    public bool firstUpdate;    // Used by actions that pull info on first update

    public Action(float pDelayLeft = 0.0f, 
                  float pDuration = 1.0f, 
                  GameObject pActionObject = null, 
                  bool pBlocking = false,
                  EaseType pEaseType = EaseType.Linear)
    {
        actionName = "null";

        actionID = IDTracker++;

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

    virtual public bool ActionUpdate(float dt)
    {
        timePassed += dt;

        return timePassed < duration;
    }

    public float Percent()
    {
        switch(easeType)
        {
            case EaseType.Linear:
                return timePassed / duration;
            case EaseType.FastExitSlowEntry:
                return Mathf.Pow(timePassed / duration, 0.33f);
            case EaseType.SlowExitFastEntry:
                return Mathf.Pow(timePassed / duration, 4);
            case EaseType.SlowEndFastMid:
                if(timePassed < duration / 2)
                    return Mathf.Pow(timePassed / duration, 8) * 128;
                else
                    return (Mathf.Pow((timePassed / duration) - 1, 8) * -128) + 1;
            default: // Linear if all else fails
                return timePassed / duration;
        }
    }

    public void AddGroupID(int ID)
    {
        if(!groupIDs.Contains(ID))
            groupIDs.Add(ID);
    }
}
