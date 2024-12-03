/*
///////////////////////////////////////////////////////////////////////////////
// This file details the implementation of the project's UIManager system,
// which contains a dedicated ActionList for UI animations/behaviors as well
// as various UI-related utility functions.
///////////////////////////////////////////////////////////////////////////////
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    private ActionList list;

    [Header("References/Prefabs")]
    public GameObject cam;
    public GameObject textPrefab;
    public GameObject tutorial;

    // On/off toggle for tutorial pop-up
    private bool tutorialActive;

    // Start is called before the first frame update
    void Start()
    {
        cam = FindObjectsOfType<Camera>()[0].gameObject;

        list = new ActionList();
        list.Start();

        tutorialActive = true;
    }

    // Update is called once per frame
    void Update()
    {
        list.Update();

        // Quick Exit Application
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            #if UNITY_STANDALONE
                Application.Quit();
            #endif
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

        // Moves tutorial window on/off screen
        if(Input.GetKeyDown(KeyCode.Semicolon))
        {
            if(tutorialActive)
            {
                list.AddAction(new Translate(tutorial.transform.position, new Vector3(-13.8f, 2.2665f, 0.0f), 
                                             0.0f, 1.0f, tutorial, false, Action.EaseType.SlowExitFastEntry));
                tutorialActive = false;
            }
            else
            {
                list.AddAction(new Translate(tutorial.transform.position, new Vector3(-5.9765f, 2.2665f, 0.0f), 
                                             0.0f, 1.0f, tutorial, false, Action.EaseType.SlowExitFastEntry));
                tutorialActive = true;
            }
        }
    }

    // This function refers to the "Drone Base" object encountered in the project' drone scene
    public void BaseScreenShake(GameObject baseObject)
    {
        // Action duration matches corresponding hitstop for destroying base
        Action waitAction = new EmptyAction(0.0f, 1.5f, null, true);
        waitAction.AddGroupID(5);
        list.AddAction(waitAction);

        // Base immediately destroyed after hitstop ends
        Action destroyAction = new DestroySelf(0.0f, 0.1f, baseObject, false);
        destroyAction.AddGroupID(5);
        list.AddAction(destroyAction);

        // Apply random screen shake motions
        Vector3 rollingPos = new Vector3(0.0f, 0.0f, -10.0f);
        Vector3 newPos;
        for(int i = 0; i < 18; i++) // arbitrary number of movements (6/second for 3 seconds)
        {
            newPos = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.25f, 0.25f), -10.0f);

            Action moveAction = new Translate(rollingPos, newPos, 0.0f, 0.05f, cam, true);
            moveAction.AddGroupID(5);
            list.AddAction(moveAction);

            rollingPos = newPos;
        }

        // Return camera to center after screen shake concludes
        Action restoreAction = new Translate(newPos, new Vector3(0.0f, 0.0f, -10.0f), 0.0f, 0.1f, cam, false);
        restoreAction.AddGroupID(5);
        list.AddAction(restoreAction);
    }

    // Used on boss component destruction
    public void MinorScreenShake()
    {
        // Matches small hitstop
        Action waitAction = new EmptyAction(0.0f, 1.0f, null, true);
        waitAction.AddGroupID(5);
        list.AddAction(waitAction);

        // Short number of screen shake motions
        Vector3 rollingPos = new Vector3(0.0f, 0.0f, -10.0f);
        Vector3 newPos;
        for(int i = 0; i < 6; i++) // arbitrary number of movements (6/second for 3 seconds)
        {
            newPos = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.25f, 0.25f), -10.0f);

            Action moveAction = new Translate(rollingPos, newPos, 0.0f, 0.05f, cam, true);
            moveAction.AddGroupID(5);
            list.AddAction(moveAction);

            rollingPos = newPos;
        }

        // Return camera to center after screen shake concludes
        Action restoreAction = new Translate(newPos, new Vector3(0.0f, 0.0f, -10.0f), 0.0f, 0.1f, cam, false);
        restoreAction.AddGroupID(5);
        list.AddAction(restoreAction);
    }

    // Creates a small particle-like piece of text that flies off in a given direction
    public void KnockedAwayText(Vector3 position, Vector3 entry, float delay, string message)
    {
        GameObject hitText = Instantiate(textPrefab, position, Quaternion.identity);
        hitText.transform.GetChild(0).GetChild(0).gameObject.GetComponent<TMP_Text>().text = message;

        // Determines direction of text movement based on the given "incoming" direction/entry
        entry.Normalize();
        entry = Vector3.RotateTowards(entry, 
                                      position + entry + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.4f, 0.4f), 0.0f),
                                      90.0f, 0.0f);

        // Main actions
        list.AddAction(new Translate(position, position + entry, delay, 1.0f, hitText, false, Action.EaseType.FastExitSlowEntry));
        list.AddAction(new Fade(1.0f, 0.0f, delay + 0.8f, 0.2f, hitText, false));
        list.AddAction(new Scale(1.0f, 1.2f, 1.0f, 1.2f, delay + 0.8f, 0.2f, hitText, false, Action.EaseType.FastExitSlowEntry));
        list.AddAction(new DestroySelf(delay + 1.0f, 0.0f, hitText, false));
    }

    // Spawns text that expands and fades after a given amount of time
    public void GrowFadeText(Vector3 position, float delay, string message)
    {
        GameObject hitText = Instantiate(textPrefab, position, Quaternion.identity);
        hitText.transform.GetChild(0).GetChild(0).gameObject.GetComponent<TMP_Text>().text = message;

        // Main actions (unlike KnockedAwayText, this remains in place)
        list.AddAction(new Fade(1.0f, 0.0f, delay + 0.1f, 0.9f, hitText, false, Action.EaseType.FastExitSlowEntry));
        list.AddAction(new Scale(1.0f, 4.0f, 1.0f, 4.0f, delay, 1.0f, hitText, false));
        list.AddAction(new DestroySelf(delay + 1.0f, 0.0f, hitText, false));
    }

    // Preset action sequence for the "Plasma Eagle" title display boss intro
    public void BossEntranceText()
    {
        GameObject plasmaText = Instantiate(textPrefab, new Vector3(-11.0f, 0.5f, 0.0f), Quaternion.identity);
        TMP_Text pTextComp = plasmaText.transform.GetChild(0).GetChild(0).gameObject.GetComponent<TMP_Text>();
        pTextComp.text = "PLASMA";
        pTextComp.fontSize = 1.0f;
        pTextComp.fontStyle = FontStyles.Bold;

        GameObject eagleText = Instantiate(textPrefab, new Vector3(11.0f, -0.5f, 0.0f), Quaternion.identity);
        TMP_Text eTextComp = eagleText.transform.GetChild(0).GetChild(0).gameObject.GetComponent<TMP_Text>();
        eTextComp.text = "EAGLE";
        eTextComp.fontSize = 1.0f;
        eTextComp.fontStyle = FontStyles.Bold;

        // Plasma (move right fast, then right steady, then fade out)
        list.AddAction(new Translate(new Vector3(-11.0f, 0.5f, 0.0f), new Vector3(-3.0f, 0.5f, 0.0f),
                                     0.0f, 1.0f, plasmaText, false, Action.EaseType.SlowExitFastEntry));
        list.AddAction(new Translate(new Vector3(-3.0f, 0.5f, 0.0f), new Vector3(0.0f, 0.5f, 0.0f),
                                     1.0f, 2.0f, plasmaText, false));
        list.AddAction(new Fade(1.0f, 0.0f, 3.0f, 1.0f, plasmaText, false, Action.EaseType.SlowExitFastEntry));
        list.AddAction(new Scale(1.0f, 2.0f, 1.0f, 2.0f, 3.0f, 1.0f, plasmaText, false, Action.EaseType.FastExitSlowEntry));
        list.AddAction(new Translate(new Vector3(0.0f, 0.5f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f),
                                     3.0f, 1.0f, plasmaText, false, Action.EaseType.FastExitSlowEntry));

        // Eagle (move left fast, then left steady, then fade out)
        list.AddAction(new Translate(new Vector3(11.0f, -0.5f, 0.0f), new Vector3(2.0f, -0.5f, 0.0f),
                                     1.0f, 1.0f, eagleText, false, Action.EaseType.SlowExitFastEntry));
        list.AddAction(new Translate(new Vector3(2.0f, -0.5f, 0.0f), new Vector3(0.0f, -0.5f, 0.0f),
                                     2.0f, 1.0f, eagleText, false));
        list.AddAction(new Fade(1.0f, 0.0f, 3.0f, 1.0f, eagleText, false, Action.EaseType.SlowExitFastEntry));
        list.AddAction(new Scale(1.0f, 2.0f, 1.0f, 2.0f, 3.0f, 1.0f, eagleText, false, Action.EaseType.FastExitSlowEntry));
        list.AddAction(new Translate(new Vector3(0.0f, -0.5f, 0.0f), new Vector3(0.0f, -1.0f, 0.0f),
                                     3.0f, 1.0f, eagleText, false, Action.EaseType.FastExitSlowEntry));
    }
}
