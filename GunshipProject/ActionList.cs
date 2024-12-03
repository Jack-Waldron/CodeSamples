/*
///////////////////////////////////////////////////////////////////////////////
// This file details the contents of the ActionList structure, which executes
// and otherwise manages a list of given Actions.
///////////////////////////////////////////////////////////////////////////////
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionList
{
    List<Action> list;

    private Dictionary<int, int> blockedIDs; // GroupID, ID of Blocking Object
    int timeScaleChannel = 0;                // Allows for systems w/ separate scaling

    // Start is called before the first frame update
    public void Start(int pTimeScaleChannel = 0)
    {
        list = new List<Action>();
        blockedIDs = new Dictionary<int, int>(); 
        timeScaleChannel = pTimeScaleChannel;
    }

    // Update is called once per frame
    public void Update()
    {
        // Adjust this list's dt factor
        float dt = Time.deltaTime;
        if(timeScaleChannel > 0)
            dt *= TimeScaleManager.globalScale;
        if(timeScaleChannel > 1)
            dt *= TimeScaleManager.enemyScale;

        for(int i = 0; i < list.Count; i++)
        {
            // Erase bad actions
            if(list[i].markedForErase)
            {
                list.RemoveAt(i);
                i--;
                continue;
            }

            // Ensure action isn't in a currently blocked group
            bool matchesBlockedGroup = false;
            foreach(int ID in list[i].groupIDs)
            {
                if(blockedIDs.ContainsKey(ID) && blockedIDs[ID] != list[i].actionID)
                    matchesBlockedGroup = true;
            }
            if(matchesBlockedGroup)
                continue;

            // Process delay and continue before running action
            if(list[i].delayLeft > 0.0f)
            {
                list[i].delayLeft -= dt;
                
                if(list[i].delayLeft < 0.0f)
                {
                    list[i].timePassed = -(list[i].delayLeft);
                    list[i].delayLeft = 0.0f;
                }

                continue;
            }

            // Run action (if complete, remove it from the list)
            if(list[i].ActionUpdate(dt) == false)
            {
                list.RemoveAt(i);
                i--;
                continue;
            }

            // If blocking, determine how it blocks and act accordingly
            if(list[i].blocking)
            {
                if(list[i].groupIDs.Count > 0)
                {
                    foreach(int ID in list[i].groupIDs)
                    { // No need to worry about duplicates, would've skipped already
                        blockedIDs.Add(ID, list[i].actionID);
                    }

                    list[i].firstUpdate = false;
                }
                else
                    break; // If not in a group, skip everything
            }
        }

        blockedIDs.Clear(); // Ensures blocked groups are always accurate to each update
    }

    public Action this[int index]
    {
        get
        {
            return list[index];
        }
    }

    public void ClearAll()
    {
        list.Clear();
        blockedIDs.Clear();
    }

    public int AddAction(Action act)
    {
        list.Add(act);
        return list.Count - 1;
    }

    public int AddActionToFront(Action act)
    {
        list.Insert(1, act);
        return 0;
    }

    public int CurrCount()
    {
        return list.Count;
    }

    public void EditActionDuration(int index, float newDuration)
    {
        list[index].duration = newDuration;
    }

    public void ClearActionsOfObject(GameObject thing)
    {
        for(int i = 0; i < list.Count; i++)
        {
            if(list[i].actionObject == thing)
            {
                list.RemoveAt(i);
                i--;
            }
        }
    }

    public void ClearActionsOfType(string type)
    {
        for(int i = 0; i < list.Count; i++)
        {
            if(list[i].actionName == type)
            {
                list.RemoveAt(i);
                i--;
            }
        }
    }

    // Used to combine status effects of the same kind (burns, etc.) w/o doubling effect
    public void RefreshActionOfType(string type, Action action)
    {
        // Try to find an existing action of the same type and transfer duration
        for(int i = 0; i < list.Count; i++)
        {
            if(list[i].actionName == type)
            {
                list[i].timePassed = 0.0f;
                list[i].duration = action.duration;
                return;
            }
        }

        // Can't find an existing action; just add it
        list.Add(action);
    }
}
