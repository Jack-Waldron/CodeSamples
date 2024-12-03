/*
///////////////////////////////////////////////////////////////////////////////
// This file details the implementation of the BossDecision action, which
// acts as a callback for the Boss's DecideAction function.
///////////////////////////////////////////////////////////////////////////////
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossDecision : Action
{
    public BossDecision(float pDelayLeft = 0.0f, 
                        float pDuration = 1.0f, 
                        GameObject pActionObject = null, 
                        bool pBlocking = false,
                        EaseType pEaseType = EaseType.Linear)
    {
        actionName = "BossDecision";

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
        // Duration doesn't matter here
        actionObject.GetComponent<Boss>().DecideAction();

        return false;
    }
}
