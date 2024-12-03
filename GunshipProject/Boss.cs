/*
///////////////////////////////////////////////////////////////////////////////
// This file details the implementation of the project's "Plasma Eagle" boss
// enemy, which utilizes a variety of different attack sequences and idle
// animations.
///////////////////////////////////////////////////////////////////////////////
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss : MonoBehaviour
{
    public ActionList list;

    // References
    [HideInInspector] public Gunship player;
    private UIManager uiComp;
    [HideInInspector] public TimeScaleManager tsmComp;

    [Header("Prefabs")]
    public GameObject rectPrefab;
    public GameObject circlePrefab;
    public GameObject wavePrefab;
    public GameObject laserPrefab;

    private bool firstUpdate;
    private bool deathTriggered;

    // Start is called before the first frame update
    void Start()
    {
        list = new ActionList();
        list.Start(2); // Set to enemy time scale channel

        // Ensures parent reference hierarchy is correctly established
        ApplyParentBoss(transform, this);

        player = FindObjectOfType<Gunship>();
        uiComp = GameObject.Find("UIManager").GetComponent<UIManager>();
        tsmComp = GameObject.Find("TimeScaleManager").GetComponent<TimeScaleManager>();

        firstUpdate = true;
        deathTriggered = false;

        // Ensures boss starts off in idle, then performs some random attack
        IdleCycle();
        list.AddAction(new BossDecision(4.0f, 0.0f, this.gameObject));
    }

    // Update is called once per frame
    void Update()
    {
        // Ensures reference is maintained across player instances
        if(player == null)
            player = FindObjectOfType<Gunship>();

        // Plays boss intro text animation
        if(firstUpdate)
        {
            uiComp.BossEntranceText();
            firstUpdate = false;
        }

        list.Update();

        // Testing actions for telegraphing geometry
        /*
        if(Input.GetKeyDown(KeyCode.T))
        {
            GameObject circle = Instantiate(circlePrefab, Vector3.zero, Quaternion.identity);

            Action growAction = new Scale(0.1f, 3.0f, 0.1f, 3.0f, 0.0f, 2.0f, circle, false, Action.EaseType.Linear);
            list.AddAction(growAction);

            Action satAction = new Saturate(0.0f, 0.9f, 0.0f, 2.0f, circle, false, Action.EaseType.Linear);
            list.AddAction(satAction);

            Action destroyAction = new DestroySelf(2.0f, 0.0f, circle);
            list.AddAction(destroyAction);

            IdleCycle();
        }
        */
    }

    public void OnDeath()
    {   
        // OnDeath condition can be met multiple times before resolution finishes
        if(!deathTriggered)
        {
            list.ClearAll();

            // Boss death should cause the player to take some minor damage
            if(player != null)
            {
                player.HandleDamage(6, false, true);
                uiComp.KnockedAwayText(player.transform.position, player.transform.position - transform.position, 0.0f, "Hit!");
            }

            // Destruction visual elements
            tsmComp.Hitstop(1.5f);
            uiComp.BaseScreenShake(this.gameObject);
            uiComp.GrowFadeText(Vector3.zero, 0.0f, "BOSS DESTROYED!!!");

            deathTriggered = true;
        }
    }

    // Ensures that each individual object that makes up the boss has a reference to here
    void ApplyParentBoss(Transform curr, Boss parentRef)
    {
        foreach(Transform child in curr)
        {
            Wing wingComp = child.GetComponent<Wing>();
            Talon talonComp = child.GetComponent<Talon>();
            Head headComp = child.GetComponent<Head>();
            Torso torsoComp = child.GetComponent<Torso>();

            if(wingComp != null)
                wingComp.parentBoss = this;
            if(talonComp != null)
                talonComp.parentBoss = this;
            if(headComp != null)
                headComp.parentBoss = this;
            if(torsoComp != null)
                torsoComp.parentBoss = this;

            if(child.GetComponent<Feather>() == null)
                ApplyParentBoss(child.transform, parentRef);
        }
    }

    // 4 sec.
    void IdleCycle()
    {
        IdleHead();
        IdleTalons();
        IdleLeftWing();
        IdleRightWing();
    }

    // Group 1 for head motions
    void IdleHead()
    {
        // Avoids animation if head is destroyed
        if(transform.GetChild(0).Find("NeckBase") != null)
        {
            Vector3 temp1;
            Vector3 temp2;

            // Base ---------------------------------------------------------------
            GameObject neckBase = transform.GetChild(0).Find("NeckBase").gameObject;

            temp1 = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.25f, 0.25f), 0.0f);
            list.AddAction(new LocalTranslate(neckBase.transform.localPosition, temp1, 0.0f, 1.0f, neckBase));

            temp2 = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.25f, 0.25f), 0.0f);
            list.AddAction(new LocalTranslate(temp1, temp2, 1.0f, 1.0f, neckBase));

            temp1 = new Vector3(Random.Range(-0.25f, 0.25f), Random.Range(-0.25f, 0.25f), 0.0f);
            list.AddAction(new LocalTranslate(temp2, temp1, 2.0f, 1.0f, neckBase));

            list.AddAction(new LocalTranslate(temp1, neckBase.transform.localPosition, 3.0f, 1.0f, neckBase));
            // --------------------------------------------------------------------

            // Segment 1 ----------------------------------------------------------
            GameObject neck1 = neckBase.transform.GetChild(0).gameObject;

            temp1 = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.15f, 0.15f), 0.0f);
            list.AddAction(new LocalTranslate(neck1.transform.localPosition, temp1, 0.0f, 1.0f, neck1));

            temp2 = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.15f, 0.15f), 0.0f);
            list.AddAction(new LocalTranslate(temp1, temp2, 1.0f, 1.0f, neck1));

            temp1 = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.15f, 0.15f), 0.0f);
            list.AddAction(new LocalTranslate(temp2, temp1, 2.0f, 1.0f, neck1));

            list.AddAction(new LocalTranslate(temp1, neck1.transform.localPosition, 3.0f, 1.0f, neck1));
            // --------------------------------------------------------------------

            // Segment 2 ----------------------------------------------------------
            GameObject neck2 = neck1.transform.GetChild(0).gameObject;

            temp1 = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), 0.0f);
            list.AddAction(new LocalTranslate(neck2.transform.localPosition, temp1, 0.0f, 1.0f, neck2));

            temp2 = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), 0.0f);
            list.AddAction(new LocalTranslate(temp1, temp2, 1.0f, 1.0f, neck2));

            temp1 = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), 0.0f);
            list.AddAction(new LocalTranslate(temp2, temp1, 2.0f, 1.0f, neck2));

            list.AddAction(new LocalTranslate(temp1, neck2.transform.localPosition, 3.0f, 1.0f, neck2));
            // --------------------------------------------------------------------

            // Head ---------------------------------------------------------------
            GameObject head = neck2.transform.GetChild(0).gameObject;

            temp1 = new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f), 0.0f);
            list.AddAction(new LocalTranslate(head.transform.localPosition, temp1, 0.0f, 1.0f, head));

            temp2 = new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f), 0.0f);
            list.AddAction(new LocalTranslate(temp1, temp2, 1.0f, 1.0f, head));

            temp1 = new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f), 0.0f);
            list.AddAction(new LocalTranslate(temp2, temp1, 2.0f, 1.0f, head));

            temp2 = new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f), 0.0f);
            list.AddAction(new LocalTranslate(temp1, head.transform.localPosition, 3.0f, 1.0f, head));
            // --------------------------------------------------------------------
        }
    }

    // Group 2 for left wing motions
    void IdleLeftWing()
    {
        // Avoids animation if left wing is completely destroyed (individual feathers don't matter)
        if(transform.GetChild(0).Find("LeftWing") != null)
        {
            Vector3 temp1;
            Vector3 temp2;

            // Wing ---------------------------------------------------------------
            GameObject wing = transform.GetChild(0).Find("LeftWing").gameObject;
            temp1 = wing.transform.localPosition + new Vector3(0.0f, 0.25f, 0.0f);
            temp2 = wing.transform.localPosition + new Vector3(0.0f, -0.25f, 0.0f);

            list.AddAction(new LocalTranslate(wing.transform.localPosition, temp1, 0.0f, 1.0f, wing, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslate(temp1, temp2, 1.0f, 2.0f, wing, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(temp2, wing.transform.localPosition, 3.0f, 1.0f, wing, false, Action.EaseType.SlowExitFastEntry));
            // --------------------------------------------------------------------
        }
    }

    // Group 3 for right wing motions
    void IdleRightWing()
    {
        // Avoids animation if right wing is completely destroyed (individual feathers don't matter)
        if(transform.GetChild(0).Find("RightWing") != null)
        {
            Vector3 temp1;
            Vector3 temp2;

            // Wing ---------------------------------------------------------------
            GameObject wing = transform.GetChild(0).Find("RightWing").gameObject;
            temp1 = wing.transform.localPosition + new Vector3(0.0f, -0.25f, 0.0f);
            temp2 = wing.transform.localPosition + new Vector3(0.0f, 0.25f, 0.0f);

            list.AddAction(new LocalTranslate(wing.transform.localPosition, temp1, 0.0f, 1.0f, wing, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslate(temp1, temp2, 1.0f, 2.0f, wing, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(temp2, wing.transform.localPosition, 3.0f, 1.0f, wing, false, Action.EaseType.SlowExitFastEntry));
        }
    }

    // Group 4 for talons motions
    void IdleTalons()
    {
        Vector3 temp1;

        // Avoids animation if left talon is destroyed
        Transform leftBaseT = transform.GetChild(0).Find("LeftLegBase");
        if(leftBaseT != null)
        {
            // Left Base ----------------------------------------------------------
            GameObject leftBase = leftBaseT.gameObject;
            temp1 = leftBase.transform.localPosition + new Vector3(0.0f, 0.1f, 0.0f);

            list.AddAction(new LocalTranslate(leftBase.transform.localPosition, temp1, 0.0f, 1.0f, leftBase, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(temp1, leftBase.transform.localPosition, 1.0f, 1.0f, leftBase, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(leftBase.transform.localPosition, temp1, 2.0f, 1.0f, leftBase, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(temp1, leftBase.transform.localPosition, 3.0f, 1.0f, leftBase, false, Action.EaseType.SlowEndFastMid));
            // --------------------------------------------------------------------

            // Left Segment 1 -----------------------------------------------------
            GameObject left1 = leftBase.transform.GetChild(0).gameObject;
            temp1 = left1.transform.localPosition + new Vector3(0.0f, 0.2f, 0.0f);

            list.AddAction(new LocalTranslate(left1.transform.localPosition, temp1, 0.0f, 1.0f, left1, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(temp1, left1.transform.localPosition, 1.0f, 1.0f, left1, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(left1.transform.localPosition, temp1, 2.0f, 1.0f, left1, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(temp1, left1.transform.localPosition, 3.0f, 1.0f, left1, false, Action.EaseType.SlowEndFastMid));
            // --------------------------------------------------------------------

            // Left Segment 2 -----------------------------------------------------
            GameObject left2 = left1.transform.GetChild(0).gameObject;
            temp1 = left2.transform.localPosition + new Vector3(0.0f, 0.4f, 0.0f);

            list.AddAction(new LocalTranslate(left2.transform.localPosition, temp1, 0.0f, 1.0f, left2, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(temp1, left2.transform.localPosition, 1.0f, 1.0f, left2, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(left2.transform.localPosition, temp1, 2.0f, 1.0f, left2, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(temp1, left2.transform.localPosition, 3.0f, 1.0f, left2, false, Action.EaseType.SlowEndFastMid));
            // --------------------------------------------------------------------

            // Left Talon ---------------------------------------------------------
            GameObject leftTalon = left2.transform.GetChild(0).gameObject;
            temp1 = leftTalon.transform.localPosition + new Vector3(0.0f, 0.5f, 0.0f);

            list.AddAction(new LocalTranslate(leftTalon.transform.localPosition, temp1, 0.0f, 1.0f, leftTalon, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(temp1, leftTalon.transform.localPosition, 1.0f, 1.0f, leftTalon, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(leftTalon.transform.localPosition, temp1, 2.0f, 1.0f, leftTalon, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(temp1, leftTalon.transform.localPosition, 3.0f, 1.0f, leftTalon, false, Action.EaseType.SlowEndFastMid));
            // --------------------------------------------------------------------
        }

        // Avoids animation if right talon is destroyed
        Transform rightBaseT = transform.GetChild(0).Find("RightLegBase"); 
        if(rightBaseT != null)
        {
            // Right Base ---------------------------------------------------------
            GameObject rightBase = rightBaseT.gameObject;
            temp1 = rightBase.transform.localPosition + new Vector3(0.0f, 0.1f, 0.0f);

            list.AddAction(new LocalTranslate(rightBase.transform.localPosition, temp1, 0.0f, 1.0f, rightBase, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(temp1, rightBase.transform.localPosition, 1.0f, 1.0f, rightBase, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(rightBase.transform.localPosition, temp1, 2.0f, 1.0f, rightBase, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(temp1, rightBase.transform.localPosition, 3.0f, 1.0f, rightBase, false, Action.EaseType.SlowEndFastMid));
            // --------------------------------------------------------------------

            // Right Segment 1 ----------------------------------------------------
            GameObject right1 = rightBase.transform.GetChild(0).gameObject;
            temp1 = right1.transform.localPosition + new Vector3(0.0f, 0.2f, 0.0f);

            list.AddAction(new LocalTranslate(right1.transform.localPosition, temp1, 0.0f, 1.0f, right1, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(temp1, right1.transform.localPosition, 1.0f, 1.0f, right1, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(right1.transform.localPosition, temp1, 2.0f, 1.0f, right1, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(temp1, right1.transform.localPosition, 3.0f, 1.0f, right1, false, Action.EaseType.SlowEndFastMid));
            // --------------------------------------------------------------------

            // Right Segment 2 ----------------------------------------------------
            GameObject right2 = right1.transform.GetChild(0).gameObject;
            temp1 = right2.transform.localPosition + new Vector3(0.0f, 0.4f, 0.0f);

            list.AddAction(new LocalTranslate(right2.transform.localPosition, temp1, 0.0f, 1.0f, right2, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(temp1, right2.transform.localPosition, 1.0f, 1.0f, right2, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(right2.transform.localPosition, temp1, 2.0f, 1.0f, right2, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(temp1, right2.transform.localPosition, 3.0f, 1.0f, right2, false, Action.EaseType.SlowEndFastMid));
            // --------------------------------------------------------------------

            // Right Talon --------------------------------------------------------
            GameObject rightTalon = right2.transform.GetChild(0).gameObject;
            temp1 = rightTalon.transform.localPosition + new Vector3(0.0f, 0.5f, 0.0f);

            list.AddAction(new LocalTranslate(rightTalon.transform.localPosition, temp1, 0.0f, 1.0f, rightTalon, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(temp1, rightTalon.transform.localPosition, 1.0f, 1.0f, rightTalon, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(rightTalon.transform.localPosition, temp1, 2.0f, 1.0f, rightTalon, false, Action.EaseType.SlowEndFastMid));
            list.AddAction(new LocalTranslate(temp1, rightTalon.transform.localPosition, 3.0f, 1.0f, rightTalon, false, Action.EaseType.SlowEndFastMid));
            // --------------------------------------------------------------------
        }
    }

    public void DecideAction()
    {
        Transform lw = transform.GetChild(0).Find("LeftWing");
        Transform rw = transform.GetChild(0).Find("RightWing");
        Transform nb = transform.GetChild(0).Find("NeckBase");
        Transform llb = transform.GetChild(0).Find("LeftLegBase");
        Transform rlb = transform.GetChild(0).Find("RightLegBase");

        // Abort if no actions can even be made
        if(!lw && !rw && !nb && !llb && !rlb)
            return;

        bool valid = false;
        while(!valid)
        {
            // System randomly selects attack, then determines limb status
            // (I would reverse this if I had another chance to work on it,
            // but it doesn't currently break anything w/ the demo)
            int attack = Random.Range(0, 4);
            switch(attack)
            {
                case 0: // Attempt to a wing for a slash attack
                    if(lw && rw)
                    {
                        if(Random.Range(0, 2) == 1)
                        {
                            LeftWingSlash(); 
                            valid = true;
                        }
                        else
                        {
                            RightWingSlash();
                            valid = true;
                        }
                    }
                    else if(lw)
                    {
                        LeftWingSlash();
                        valid = true;
                    }
                    else if(rw)
                    {
                        RightWingSlash();
                        valid = true;
                    }
                    break;
                case 1: // Attempt to use the head for a slam attack
                    if(nb)
                    {
                        HeadBlast();
                        valid = true;
                    }
                    break;
                case 2: // Attempt to use the talon(s) for a grab attack
                    if(llb || rlb)
                    {
                        TalonGrab();
                        valid = true;
                    }
                    break;
                case 3: // Attempt to use head for a laser attack (talons aren't required)
                    if(nb)
                    {
                        LaserShot();
                        valid = true;
                    }
                    break;
                default:
                    break;
            }
        }
    }

    // 8 sec.
    void LeftWingSlash()
    {
        Debug.Log("LeftWingSlash");

        Vector3 temp1;
        Vector3 temp2;
        GameObject wingBase = transform.GetChild(0).Find("LeftWing").gameObject;

        // Wing moves in
        temp1 = wingBase.transform.localPosition + new Vector3(0.25f, 0.0f, 0.0f);
        list.AddAction(new LocalTranslate(wingBase.transform.localPosition, temp1, 0.0f, 1.0f, wingBase, false, Action.EaseType.FastExitSlowEntry));

        // Wing juts out/spreads out feathers
        temp2 = wingBase.transform.localPosition + new Vector3(-0.1f, 0.0f, 0.0f);
        list.AddAction(new LocalTranslate(temp1, temp2, 1.0f, 1.0f, wingBase, false, Action.EaseType.SlowExitFastEntry));
        JutOut(wingBase.transform.GetChild(0), -0.5f, 1.0f);
        JutOut(wingBase.transform.GetChild(1), -0.5f, 1.0f);
        JutOut(wingBase.transform.GetChild(2), -0.5f, 1.0f);

        // Wing winds up
        temp1 = temp2 + new Vector3(0.0f, 0.5f, 0.0f);
        list.AddAction(new LocalTranslate(temp2, temp1, 2.0f, 1.0f, wingBase, false, Action.EaseType.SlowExitFastEntry));
        list.AddAction(new LocalRotate(0.0f, -90.0f, 2.0f, 1.0f, wingBase, false, Action.EaseType.SlowExitFastEntry));

        // (Create wind-up telegraph for attack)
        list.AddAction(new SpawnShape(circlePrefab, new Vector3(0.5202f, 2.8118f, 0.0f), 
                                                    Vector3.zero,
                                                    new Vector3(18.74713f, 12.40117f, 0.0f),
                                                    true, null, 2.5f, 1.5f, wingBase));
        list.AddAction(new SpawnShape(rectPrefab, new Vector3(6.557f, 1.16f, 0.0f), 
                                                  new Vector3(0.0f, 0.0f, 6.832f),
                                                  new Vector3(10.22726f, 22.46575f, 0.0f),
                                                  false, null, 2.5f, 1.5f, wingBase));

        // Wing strikes
        temp2 = temp1 + new Vector3(1.0f, -1.0f, 0.0f);
        list.AddAction(new LocalTranslate(temp1, temp2, 3.0f, 1.0f, wingBase, false, Action.EaseType.SlowExitFastEntry));
        list.AddAction(new LocalRotate(-90.0f, 95.0f, 3.0f, 1.0f, wingBase, false, Action.EaseType.SlowExitFastEntry));

        // Wing returns (after a brief delay)
        list.AddAction(new LocalTranslate(temp2, wingBase.transform.localPosition, 6.0f, 2.0f, wingBase, false, Action.EaseType.SlowEndFastMid));
        list.AddAction(new LocalRotate(95.0f, 0.0f, 6.0f, 2.0f, wingBase, false, Action.EaseType.SlowEndFastMid));
        JutIn(wingBase.transform.GetChild(0), -0.5f, 6.0f);
        JutIn(wingBase.transform.GetChild(1), -0.5f, 6.0f);
        JutIn(wingBase.transform.GetChild(2), -0.5f, 6.0f);

        // --------------------------------------------------------------------
        // --------------------------------------------------------------------

        // Ensures other boss appendages remain idle during this attack
        for(int i = 0; i < 2; i++)
        {
            IdleHead();
            IdleTalons();
            IdleRightWing();

            // Use locks to ensure idle anims are uninterrupted
            list.AddAction(new EmptyAction(0.0f, 4.0f, null, true));
        }

        // Determine next attack after this one finishes
        list.AddAction(new BossDecision(0.0f, 0.0f, this.gameObject));
    }

    // 8 sec.
    void RightWingSlash()
    {
        Debug.Log("RightWingSlash");

        Vector3 temp1;
        Vector3 temp2;
        GameObject wingBase = transform.GetChild(0).Find("RightWing").gameObject;

        // Wing moves in
        temp1 = wingBase.transform.localPosition + new Vector3(-0.25f, 0.0f, 0.0f);
        list.AddAction(new LocalTranslate(wingBase.transform.localPosition, temp1, 0.0f, 1.0f, wingBase, false, Action.EaseType.FastExitSlowEntry));

        // Wing juts out/spreads out feathers
        temp2 = wingBase.transform.localPosition + new Vector3(0.1f, 0.0f, 0.0f);
        list.AddAction(new LocalTranslate(temp1, temp2, 1.0f, 1.0f, wingBase, false, Action.EaseType.SlowExitFastEntry));
        JutOut(wingBase.transform.GetChild(0), 0.5f, 1.0f);
        JutOut(wingBase.transform.GetChild(1), 0.5f, 1.0f);
        JutOut(wingBase.transform.GetChild(2), 0.5f, 1.0f);

        // Wing winds up
        temp1 = temp2 + new Vector3(0.0f, 0.5f, 0.0f);
        list.AddAction(new LocalTranslate(temp2, temp1, 2.0f, 1.0f, wingBase, false, Action.EaseType.SlowExitFastEntry));
        list.AddAction(new LocalRotate(0.0f, 90.0f, 2.0f, 1.0f, wingBase, false, Action.EaseType.SlowExitFastEntry));

        // (Create wind-up telegraph for attack)
        list.AddAction(new SpawnShape(circlePrefab, new Vector3(-0.5202f, 2.8118f, 0.0f), 
                                                    Vector3.zero,
                                                    new Vector3(18.74713f, 12.40117f, 0.0f),
                                                    true, null, 2.5f, 1.5f, wingBase));
        list.AddAction(new SpawnShape(rectPrefab, new Vector3(-6.557f, 1.16f, 0.0f), 
                                                  new Vector3(0.0f, 0.0f, -6.832f),
                                                  new Vector3(10.22726f, 22.46575f, 0.0f),
                                                  false, null, 2.5f, 1.5f, wingBase));

        // Wing strikes
        temp2 = temp1 + new Vector3(-1.0f, -1.0f, 0.0f);
        list.AddAction(new LocalTranslate(temp1, temp2, 3.0f, 1.0f, wingBase, false, Action.EaseType.SlowExitFastEntry));
        list.AddAction(new LocalRotate(90.0f, -95.0f, 3.0f, 1.0f, wingBase, false, Action.EaseType.SlowExitFastEntry));

        // Wing returns (after a brief delay)
        list.AddAction(new LocalTranslate(temp2, wingBase.transform.localPosition, 6.0f, 2.0f, wingBase, false, Action.EaseType.SlowEndFastMid));
        list.AddAction(new LocalRotate(-95.0f, 0.0f, 6.0f, 2.0f, wingBase, false, Action.EaseType.SlowEndFastMid));
        JutIn(wingBase.transform.GetChild(0), 0.5f, 6.0f);
        JutIn(wingBase.transform.GetChild(1), 0.5f, 6.0f);
        JutIn(wingBase.transform.GetChild(2), 0.5f, 6.0f);

        // --------------------------------------------------------------------
        // --------------------------------------------------------------------

        // Ensures other boss appendages remain idle during this attack
        for(int i = 0; i < 2; i++)
        {
            IdleHead();
            IdleTalons();
            IdleLeftWing();
            
            // Use locks to ensure idle anims are uninterrupted
            list.AddAction(new EmptyAction(0.0f, 4.0f, null, true));
        }

        // Determine next attack after this one finishes
        list.AddAction(new BossDecision(0.0f, 0.0f, this.gameObject));
    }

    // Causes chain of child feather segments to increase their spacing
    void JutOut(Transform curr, float offset, float delay)
    {
        list.AddAction(new LocalTranslate(curr.localPosition, curr.localPosition + new Vector3(offset, 0.0f, 0.0f),
                                          delay, 1.0f, curr.gameObject, false, Action.EaseType.SlowExitFastEntry));

        foreach(Transform child in curr)
        {
            Feather featherComp = child.GetComponent<Feather>();
            if(featherComp != null)
                JutOut(child, offset, delay);
        }
    }

    // Causes chain of child feather segments to reduce their spacing back to normal
    void JutIn(Transform curr, float offset, float delay)
    {
        list.AddAction(new LocalTranslate(curr.localPosition + new Vector3(offset, 0.0f, 0.0f), curr.localPosition,
                                          delay, 1.0f, curr.gameObject, false, Action.EaseType.SlowExitFastEntry));

        foreach(Transform child in curr)
        {
            Feather featherComp = child.GetComponent<Feather>();
            if(featherComp != null)
                JutIn(child, offset, delay);
        }
    }

    // 8 sec.
    void HeadBlast()
    {
        Debug.Log("HeadBlast");

        GameObject neckBase = transform.GetChild(0).Find("NeckBase").gameObject;
        GameObject neck1 = neckBase.transform.GetChild(0).gameObject;
        GameObject neck2 = neck1.transform.GetChild(0).gameObject;
        GameObject head = neck2.transform.GetChild(0).gameObject;

        // Reel head back in anticipation
        Vector3 temp1 = neckBase.transform.localPosition + new Vector3(0.0f, 0.2f, 0.0f);
        list.AddAction(new LocalTranslate(neckBase.transform.localPosition, temp1, 0.0f, 2.0f, neckBase, false, Action.EaseType.FastExitSlowEntry));
        list.AddAction(new LocalTranslate(neck1.transform.localPosition, temp1, 0.0f, 2.0f, neck1, false, Action.EaseType.FastExitSlowEntry));
        list.AddAction(new LocalTranslate(neck2.transform.localPosition, temp1, 0.0f, 2.0f, neck2, false, Action.EaseType.FastExitSlowEntry));
        list.AddAction(new LocalTranslate(head.transform.localPosition, temp1, 0.0f, 2.0f, head, false, Action.EaseType.FastExitSlowEntry));

        // (Create direction telegraph shape)
        Action bridgeAction = new BridgeTargets(player.transform, head.transform.GetChild(0), 0.0f, 2.0f, null);
        list.AddAction(new SpawnShape(rectPrefab, Vector3.zero, Vector3.zero, new Vector3(0.1f, 1.0f, 1.0f),
                                                  true, bridgeAction, 0.0f, 2.0f, head));

        // Leap towards last known player location
        if(player)
        {
            list.AddAction(new TranslateToTarget(player.transform, 0.25f, 2.0f, 1.0f, neckBase, false, Action.EaseType.Linear));
            list.AddAction(new TranslateToTarget(player.transform, 0.5f, 2.0f, 1.0f, neck1, false, Action.EaseType.Linear));
            list.AddAction(new TranslateToTarget(player.transform, 0.75f, 2.0f, 1.0f, neck2, false, Action.EaseType.Linear));
            list.AddAction(new TranslateToTarget(player.transform, 1.0f, 2.0f, 1.0f, head, false, Action.EaseType.Linear));
        }
        else 
        {
            // Use a basic default location in case the player dies
            list.AddAction(new LocalTranslate(temp1, temp1 + new Vector3(0.0f, -1.0f, 0.0f), 2.0f, 1.0f, neckBase, false, Action.EaseType.Linear));

            Vector temp2 = neck1.transform.localPosition + new Vector3(0.0f, 0.15f, 0.0f);
            list.AddAction(new LocalTranslate(temp2, temp2 + new Vector3(0.0f, -1.0f, 0.0f), 2.0f, 1.0f, neck1, false, Action.EaseType.Linear));

            Vector temp3 = neck2.transform.localPosition + new Vector3(0.0f, 0.1f, 0.0f);
            list.AddAction(new LocalTranslate(temp3, temp3 + new Vector3(0.0f, -1.0f, 0.0f), 2.0f, 1.0f, neck2, false, Action.EaseType.Linear));

            Vector temp4 = head.transform.localPosition + new Vector3(0.0f, 0.05f, 0.0f);
            list.AddAction(new LocalTranslate(temp4, temp4 + new Vector3(0.0f, -1.0f, 0.0f), 2.0f, 1.0f, head, false, Action.EaseType.Linear));
        }

        // (Create AOE telegraph shape)
        list.AddAction(new SpawnShapeAtTarget(circlePrefab, player.transform, 
                                                            Vector3.zero,
                                                            new Vector3(8.0f, 8.0f, 1.0f),
                                                            true, null, 2.0f, 1.0f, head));
        
        // Create a shockwave that matches the telegraph (and then hold for a moment)
        list.AddAction(new SpawnShockwave(wavePrefab, 3.0f, 0.0f, head));

        // Return
        list.AddAction(new LocalTranslateToPoint(neckBase.transform.localPosition, 5.0f, 3.0f, neckBase, false, Action.EaseType.FastExitSlowEntry));
        list.AddAction(new LocalTranslateToPoint(neck1.transform.localPosition, 5.0f, 3.0f, neck1, false, Action.EaseType.FastExitSlowEntry));
        list.AddAction(new LocalTranslateToPoint(neck2.transform.localPosition, 5.0f, 3.0f, neck2, false, Action.EaseType.FastExitSlowEntry));
        list.AddAction(new LocalTranslateToPoint(head.transform.localPosition, 5.0f, 3.0f, head, false, Action.EaseType.FastExitSlowEntry));

        // --------------------------------------------------------------------
        // --------------------------------------------------------------------

        // Ensures other boss appendages remain idle during this attack
        for(int i = 0; i < 2; i++)
        {
            IdleTalons();
            IdleLeftWing();
            IdleRightWing();

            // Use locks to ensure idle anims are uninterrupted
            list.AddAction(new EmptyAction(0.0f, 4.0f, null, true));
        }

        // Determine next attack after this one finishes
        list.AddAction(new BossDecision(0.0f, 0.0f, this.gameObject));
    }

    // 16 sec. (Unimplemented/Cut from final demo)
    void TorsoMortar()
    {
        Debug.Log("TorsoMortar");
        
        // Ensures other boss appendages remain idle during this attack
        for(int i = 0; i < 4; i++)
        {
            IdleTalons();
            IdleLeftWing();
            IdleRightWing();

            // Use locks to ensure idle anims are uninterrupted
            list.AddAction(new EmptyAction(0.0f, 4.0f, null, true));
        }

        // Determine next attack after this one finishes
        list.AddAction(new BossDecision(0.0f, 0.0f, this.gameObject));
    }

    // 12 sec.
    void TalonGrab()
    {
        Debug.Log("TalonGrab");

        Vector3 temp1;
        Vector3 temp2;
        Vector3 temp3;
        Vector3 temp4;
        Transform leftBaseT = transform.GetChild(0).Find("LeftLegBase");
        Transform rightBaseT = transform.GetChild(0).Find("RightLegBase");

        // Don't perform grab with left talon if it's destroyed
        if(leftBaseT)
        {
            GameObject leftBase = transform.GetChild(0).Find("LeftLegBase").gameObject;
            GameObject left1 = leftBase.transform.GetChild(0).gameObject;
            GameObject left2 = left1.transform.GetChild(0).gameObject;
            GameObject leftTalon = left2.transform.GetChild(0).gameObject;

            // Ready talon (wind-up backwards slightly)
            temp1 = leftBase.transform.localPosition + new Vector3(0.0f, 0.2f, 0.0f);
            list.AddAction(new LocalTranslate(leftBase.transform.localPosition, temp1, 0.0f, 2.0f, leftBase, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslate(left1.transform.localPosition, temp1, 0.0f, 2.0f, left1, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslate(left2.transform.localPosition, temp1, 0.0f, 2.0f, left2, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslate(leftTalon.transform.localPosition, temp1, 0.0f, 2.0f, leftTalon, false, Action.EaseType.FastExitSlowEntry));

            // (Create directional telegraph)
            Action bridgeAction = new BridgeTargets(player.transform, leftTalon.transform, 0.0f, 2.0f, null);
            list.AddAction(new SpawnShape(rectPrefab, Vector3.zero, Vector3.zero, new Vector3(0.1f, 1.0f, 1.0f),
                                                      true, bridgeAction, 0.0f, 2.0f, leftTalon));

            // Leap towards last known player location, sticking them on contact
            if(player)
            {
                list.AddAction(new SetGrabbing(true, 2.0f, 0.0f, leftTalon));

                list.AddAction(new TranslateToTarget(player.transform, 0.25f, 2.0f, 1.0f, leftBase, false, Action.EaseType.Linear));
                list.AddAction(new TranslateToTarget(player.transform, 0.5f, 2.0f, 1.0f, left1, false, Action.EaseType.Linear));
                list.AddAction(new TranslateToTarget(player.transform, 0.75f, 2.0f, 1.0f, left2, false, Action.EaseType.Linear));
                list.AddAction(new TranslateToTarget(player.transform, 1.0f, 2.0f, 1.0f, leftTalon, false, Action.EaseType.Linear));
            }
            else
            {
                // Move to a default location in case the player is dead
                list.AddAction(new LocalTranslate(temp1, temp1 + new Vector3(0.0f, -1.0f, 0.0f), 2.0f, 1.0f, leftBase, false, Action.EaseType.Linear));

                temp2 = left1.transform.localPosition + new Vector3(0.0f, 0.15f, 0.0f);
                list.AddAction(new LocalTranslate(temp2, temp2 + new Vector3(0.0f, -1.0f, 0.0f), 2.0f, 1.0f, left1, false, Action.EaseType.Linear));

                temp3 = left2.transform.localPosition + new Vector3(0.0f, 0.1f, 0.0f);
                list.AddAction(new LocalTranslate(temp3, temp3 + new Vector3(0.0f, -1.0f, 0.0f), 2.0f, 1.0f, left2, false, Action.EaseType.Linear));

                temp4 = leftTalon.transform.localPosition + new Vector3(0.0f, 0.05f, 0.0f);
                list.AddAction(new LocalTranslate(temp4, temp4 + new Vector3(0.0f, -1.0f, 0.0f), 2.0f, 1.0f, leftTalon, false, Action.EaseType.Linear));
            }

            // Return (and unstick player)
            list.AddAction(new LocalTranslateToPoint(leftBase.transform.localPosition, 3.0f, 1.0f, leftBase, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslateToPoint(left1.transform.localPosition, 3.0f, 1.0f, left1, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslateToPoint(left2.transform.localPosition, 3.0f, 1.0f, left2, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslateToPoint(leftTalon.transform.localPosition, 3.0f, 1.0f, leftTalon, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new SetGrabbing(false, 3.0f, 0.0f, leftTalon));

            // (Check if the player was alive and grabbed; If so, pummel them while they're grabbed)
            if(player)
                list.AddAction(new TryPummel(this, 4.0f, 0.0f, leftTalon));
        }

        // Don't perform grab with right talon if it's destroyed
        if(rightBaseT)
        {
            GameObject rightBase = transform.GetChild(0).Find("RightLegBase").gameObject;
            GameObject right1 = rightBase.transform.GetChild(0).gameObject;
            GameObject right2 = right1.transform.GetChild(0).gameObject;
            GameObject rightTalon = right2.transform.GetChild(0).gameObject;

            // Ready talon (wind-up backwards slightly)
            temp1 = rightBase.transform.localPosition + new Vector3(0.0f, 0.2f, 0.0f);
            list.AddAction(new LocalTranslate(rightBase.transform.localPosition, temp1, 0.0f, 2.0f, rightBase, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslate(right1.transform.localPosition, temp1, 0.0f, 2.0f, right1, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslate(right2.transform.localPosition, temp1, 0.0f, 2.0f, right2, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslate(rightTalon.transform.localPosition, temp1, 0.0f, 2.0f, rightTalon, false, Action.EaseType.FastExitSlowEntry));

            // (Create directional telegraph)
            Action bridgeAction = new BridgeTargets(player.transform, rightTalon.transform, 0.0f, 2.0f, null);
            list.AddAction(new SpawnShape(rectPrefab, Vector3.zero, Vector3.zero, new Vector3(0.1f, 1.0f, 1.0f),
                                                      true, bridgeAction, 0.0f, 2.0f, rightTalon));

            // Leap towards last known player location, sticking them on contact
            if(player)
            {
                list.AddAction(new SetGrabbing(true, 2.0f, 0.0f, rightTalon));

                list.AddAction(new TranslateToTarget(player.transform, 0.25f, 2.0f, 1.0f, rightBase, false, Action.EaseType.Linear));
                list.AddAction(new TranslateToTarget(player.transform, 0.5f, 2.0f, 1.0f, right1, false, Action.EaseType.Linear));
                list.AddAction(new TranslateToTarget(player.transform, 0.75f, 2.0f, 1.0f, right2, false, Action.EaseType.Linear));
                list.AddAction(new TranslateToTarget(player.transform, 1.0f, 2.0f, 1.0f, rightTalon, false, Action.EaseType.Linear));
            }
            else
            {
                // Move to a default location in case the player is dead
                list.AddAction(new LocalTranslate(temp1, temp1 + new Vector3(0.0f, -1.0f, 0.0f), 2.0f, 1.0f, rightBase, false, Action.EaseType.Linear));

                temp2 = right1.transform.localPosition + new Vector3(0.0f, 0.15f, 0.0f);
                list.AddAction(new LocalTranslate(temp2, temp2 + new Vector3(0.0f, -1.0f, 0.0f), 2.0f, 1.0f, right1, false, Action.EaseType.Linear));

                temp3 = right2.transform.localPosition + new Vector3(0.0f, 0.1f, 0.0f);
                list.AddAction(new LocalTranslate(temp3, temp3 + new Vector3(0.0f, -1.0f, 0.0f), 2.0f, 1.0f, right2, false, Action.EaseType.Linear));

                temp4 = rightTalon.transform.localPosition + new Vector3(0.0f, 0.05f, 0.0f);
                list.AddAction(new LocalTranslate(temp4, temp4 + new Vector3(0.0f, -1.0f, 0.0f), 2.0f, 1.0f, rightTalon, false, Action.EaseType.Linear));
            }

            // Return (and unstick player)
            list.AddAction(new LocalTranslateToPoint(rightBase.transform.localPosition, 3.0f, 1.0f, rightBase, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslateToPoint(right1.transform.localPosition, 3.0f, 1.0f, right1, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslateToPoint(right2.transform.localPosition, 3.0f, 1.0f, right2, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslateToPoint(rightTalon.transform.localPosition, 3.0f, 1.0f, rightTalon, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new SetGrabbing(false, 3.0f, 0.0f, rightTalon));

            // (Check if the player was alive and grabbed; If so, pummel them while they're grabbed)
            if(player)
                list.AddAction(new TryPummel(this, 4.0f, 0.0f, rightTalon));
        }

        // --------------------------------------------------------------------
        // --------------------------------------------------------------------
        
        // Ensures other boss appendages remain idle during this attack
        for(int i = 0; i < 3; i++)
        {
            IdleHead();
            IdleLeftWing();
            IdleRightWing();

            // Use locks to ensure idle anims are uninterrupted
            list.AddAction(new EmptyAction(0.0f, 4.0f, null, true));
        }

        // Determine next attack after this one finishes
        list.AddAction(new BossDecision(0.0f, 0.0f, this.gameObject));
    }

    public void Pummel(GameObject talon)
    {
        GameObject leg2 = talon.transform.parent.gameObject;
        GameObject leg1 = leg2.transform.parent.gameObject;
        GameObject legBase = leg1.transform.parent.gameObject;

        // "Crush" the player
        // - (attack 1)
        list.AddActionToFront(new Scale(1.2625f, 3.0f * 1.2625f, 2.25875f, 3.0f * 2.25875f, 
                                        0.0f, 0.5f, talon, false, Action.EaseType.FastExitSlowEntry));
        list.AddActionToFront(new Scale(3.0f * 1.2625f, 1.2625f, 3.0f * 2.25875f, 2.25875f, 
                                        0.5f, 0.5f, talon, false, Action.EaseType.SlowExitFastEntry));
        list.AddActionToFront(new DamagePlayer(uiComp, 3, 1.0f, 0.0f, player.gameObject));
        // - (attack 2)
        list.AddActionToFront(new Scale(1.2625f, 3.0f * 1.2625f, 2.25875f, 3.0f * 2.25875f, 
                                        2.0f, 0.5f, talon, false, Action.EaseType.FastExitSlowEntry));
        list.AddActionToFront(new Scale(3.0f * 1.2625f, 1.2625f, 3.0f * 2.25875f, 2.25875f, 
                                        2.5f, 0.5f, talon, false, Action.EaseType.SlowExitFastEntry));
        list.AddActionToFront(new DamagePlayer(uiComp, 3, 3.0f, 0.0f, player.gameObject));
        // - (attack 3) 
        list.AddActionToFront(new Scale(1.2625f, 3.0f * 1.2625f, 2.25875f, 3.0f * 2.25875f, 
                                        4.0f, 0.5f, talon, false, Action.EaseType.FastExitSlowEntry));
        list.AddActionToFront(new Scale(3.0f * 1.2625f, 1.2625f, 3.0f * 2.25875f, 2.25875f, 
                                        4.5f, 0.5f, talon, false, Action.EaseType.SlowExitFastEntry));
        list.AddActionToFront(new DamagePlayer(uiComp, 3, 5.0f, 0.0f, player.gameObject));

        // Place talons away from the boss (and remove stuck debuff)
        list.AddActionToFront(new LocalTranslate(talon.transform.localPosition, talon.transform.localPosition + new Vector3(0.0f, -1.0f, 0.0f),
                                                 6.0f, 1.0f, talon, false, Action.EaseType.SlowExitFastEntry));
        list.AddActionToFront(new LocalTranslate(leg2.transform.localPosition, leg2.transform.localPosition + new Vector3(0.0f, -1.0f, 0.0f),
                                                 6.0f, 1.0f, leg2, false, Action.EaseType.SlowExitFastEntry));
        list.AddActionToFront(new LocalTranslate(leg1.transform.localPosition, leg1.transform.localPosition + new Vector3(0.0f, -1.0f, 0.0f),
                                                 6.0f, 1.0f, leg1, false, Action.EaseType.SlowExitFastEntry));
        list.AddActionToFront(new LocalTranslate(legBase.transform.localPosition, legBase.transform.localPosition + new Vector3(0.0f, -1.0f, 0.0f),
                                                 6.0f, 1.0f, legBase, false, Action.EaseType.SlowExitFastEntry));
        list.AddActionToFront(new EndStuck(7.0f, 0.0f, player.gameObject));

        // Retract talons back to body
        list.AddActionToFront(new LocalTranslate(talon.transform.localPosition + new Vector3(0.0f, -1.0f, 0.0f), talon.transform.localPosition,
                                                 7.0f, 1.0f, talon, false, Action.EaseType.FastExitSlowEntry));
        list.AddActionToFront(new LocalTranslate(leg2.transform.localPosition + new Vector3(0.0f, -1.0f, 0.0f), leg2.transform.localPosition,
                                                 7.0f, 1.0f, leg2, false, Action.EaseType.FastExitSlowEntry));
        list.AddActionToFront(new LocalTranslate(leg1.transform.localPosition + new Vector3(0.0f, -1.0f, 0.0f), leg1.transform.localPosition,
                                                 7.0f, 1.0f, leg1, false, Action.EaseType.FastExitSlowEntry));
        list.AddActionToFront(new LocalTranslate(legBase.transform.localPosition + new Vector3(0.0f, -1.0f, 0.0f), legBase.transform.localPosition,
                                                 7.0f, 1.0f, legBase, false, Action.EaseType.FastExitSlowEntry));

        // Toggles off internal flag
        list.AddActionToFront(new ClearGrabSuccess(8.0f, 0.0f, talon));
    }

    // 8 sec.
    void LaserShot()
    {
        Debug.Log("LaserShot");

        Vector3 temp1;

        // Neck segements
        GameObject neckBase = transform.GetChild(0).Find("NeckBase").gameObject;
        GameObject neck1 = neckBase.transform.GetChild(0).gameObject;
        GameObject neck2 = neck1.transform.GetChild(0).gameObject;
        GameObject head = neck2.transform.GetChild(0).gameObject;

        // Left talon segments
        Transform leftBaseT = transform.GetChild(0).Find("LeftLegBase");
        GameObject leftBase; 
        GameObject left1; 
        GameObject left2;
        GameObject leftTalon;
        if(leftBaseT)
        {
            leftBase = leftBaseT.gameObject;
            left1 = leftBase.transform.GetChild(0).gameObject;
            left2 = left1.transform.GetChild(0).gameObject;
            leftTalon = left2.transform.GetChild(0).gameObject;
        }

        // Right talon segments
        Transform rightBaseT = transform.GetChild(0).Find("RightLegBase");
        GameObject rightBase; 
        GameObject right1; 
        GameObject right2;
        GameObject rightTalon;
        if(rightBaseT)
        {
            rightBase = rightBaseT.gameObject;
            right1 = rightBase.transform.GetChild(0).gameObject;
            right2 = right1.transform.GetChild(0).gameObject;
            rightTalon = right2.transform.GetChild(0).gameObject;
        }

        // --------------------------------------------------------------------

        // Spin head... 
        list.AddAction(new LocalRotate(0.0f, 180.0f, 0.0f, 0.5f, neckBase));
        list.AddAction(new LocalRotate(180.0f, 360.0f, 0.5f, 0.5f, neckBase));
        list.AddAction(new LocalRotate(0.0f, 180.0f, 1.0f, 0.5f, neckBase));
        list.AddAction(new LocalRotate(180.0f, 360.0f, 1.5f, 0.5f, neckBase));
        
        // and send out talons to random locations
        if(leftBaseT)
        {
            // Add random location determination
            Vector3 randomDest = new Vector3(Random.Range(-7.0f, -2.0f), Random.Range(-1.0f, 1.0f), 0.0f);
            Vector3 diff = randomDest - leftTalon.transform.position;

            list.AddAction(new Translate(leftTalon.transform.position, randomDest,
                                         0.0f, 2.0f, leftTalon, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new Translate(left2.transform.position, leftTalon.transform.position + ((0.75f) * diff), 
                                         0.0f, 2.0f, left2, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new Translate(left1.transform.position, leftTalon.transform.position + ((0.5f) * diff), 
                                         0.0f, 2.0f, left1, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new Translate(leftBase.transform.position, leftTalon.transform.position + ((0.25f) * diff), 
                                         0.0f, 2.0f, leftBase, false, Action.EaseType.FastExitSlowEntry));
        }
        if(rightBaseT)
        {
            // Add random location determination
            Vector3 randomDest = new Vector3(Random.Range(2.0f, 7.0f), Random.Range(-1.0f, 1.0f), 0.0f);
            Vector3 diff = randomDest - rightTalon.transform.position;

            list.AddAction(new Translate(rightTalon.transform.position, randomDest,
                                         0.0f, 2.0f, rightTalon, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new Translate(right2.transform.position, rightTalon.transform.position + ((0.75f) * diff), 
                                         0.0f, 2.0f, right2, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new Translate(right1.transform.position, rightTalon.transform.position + ((0.5f) * diff), 
                                         0.0f, 2.0f, right1, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new Translate(rightBase.transform.position, rightTalon.transform.position + ((0.25f) * diff), 
                                         0.0f, 2.0f, rightBase, false, Action.EaseType.FastExitSlowEntry));
        }

        // Reel head back briefly in anticipation
        temp1 = neckBase.transform.localPosition + new Vector3(0.0f, 0.2f, 0.0f);
        list.AddAction(new LocalTranslate(neckBase.transform.localPosition, temp1, 2.0f, 1.0f, neckBase, false, Action.EaseType.FastExitSlowEntry));
        list.AddAction(new LocalTranslate(neck1.transform.localPosition, temp1, 2.0f, 1.0f, neck1, false, Action.EaseType.FastExitSlowEntry));
        list.AddAction(new LocalTranslate(neck2.transform.localPosition, temp1, 2.0f, 1.0f, neck2, false, Action.EaseType.FastExitSlowEntry));
        list.AddAction(new LocalTranslate(head.transform.localPosition, temp1, 2.0f, 1.0f, head, false, Action.EaseType.FastExitSlowEntry));

        // Move head forward, in the direction of a talon (or the player if no talons)
        bool aimLeft = true;
        bool aimRight = true;
        if(leftBaseT && rightBaseT)
        {
            if(Random.Range(0,2) == 0)
            {
                aimLeft = true;
                aimRight = false;
            }
            else
            {
                aimRight = true;
                aimLeft = false;
            }
        }
        if(aimLeft && leftBaseT)
        {
            aimLeft = true;
            aimRight = false;

            list.AddAction(new TranslateToTarget(leftTalon.transform, 0.4f, 3.0f, 1.0f, head, false, Action.EaseType.SlowExitFastEntry));
            list.AddAction(new TranslateToTarget(leftTalon.transform, 0.3f, 3.0f, 1.0f, neck2, false, Action.EaseType.SlowExitFastEntry));
            list.AddAction(new TranslateToTarget(leftTalon.transform, 0.2f, 3.0f, 1.0f, neck1, false, Action.EaseType.SlowExitFastEntry));
            list.AddAction(new TranslateToTarget(leftTalon.transform, 0.1f, 3.0f, 1.0f, neckBase, false, Action.EaseType.SlowExitFastEntry));

            Action bridgeAction = new BridgeTargets(head.transform, leftTalon.transform, 0.0f, 4.0f, null);
            list.AddAction(new SpawnShape(rectPrefab, Vector3.zero, Vector3.zero, new Vector3(0.1f, 1.0f, 1.0f),
                                          true, bridgeAction, 0.0f, 4.0f, leftTalon));
        }
        else if(aimRight && rightBaseT)
        {
            aimRight = true;
            aimLeft = false;

            list.AddAction(new TranslateToTarget(rightTalon.transform, 0.4f, 3.0f, 1.0f, head, false, Action.EaseType.SlowExitFastEntry));
            list.AddAction(new TranslateToTarget(rightTalon.transform, 0.3f, 3.0f, 1.0f, neck2, false, Action.EaseType.SlowExitFastEntry));
            list.AddAction(new TranslateToTarget(rightTalon.transform, 0.2f, 3.0f, 1.0f, neck1, false, Action.EaseType.SlowExitFastEntry));
            list.AddAction(new TranslateToTarget(rightTalon.transform, 0.1f, 3.0f, 1.0f, neckBase, false, Action.EaseType.SlowExitFastEntry));

            Action bridgeAction = new BridgeTargets(head.transform, rightTalon.transform, 0.0f, 4.0f, null);
            list.AddAction(new SpawnShape(rectPrefab, Vector3.zero, Vector3.zero, new Vector3(0.1f, 1.0f, 1.0f),
                                          true, bridgeAction, 0.0f, 4.0f, rightTalon));
        }
        else
        {
            aimLeft = false;
            aimRight = false;

            list.AddAction(new TranslateToTarget(player.transform, 0.4f, 3.0f, 1.0f, head, false, Action.EaseType.SlowExitFastEntry));
            list.AddAction(new TranslateToTarget(player.transform, 0.3f, 3.0f, 1.0f, neck2, false, Action.EaseType.SlowExitFastEntry));
            list.AddAction(new TranslateToTarget(player.transform, 0.2f, 3.0f, 1.0f, neck1, false, Action.EaseType.SlowExitFastEntry));
            list.AddAction(new TranslateToTarget(player.transform, 0.1f, 3.0f, 1.0f, neckBase, false, Action.EaseType.SlowExitFastEntry));

            Action bridgeAction = new BridgeTargets(head.transform, player.transform, 0.0f, 4.0f, null);
            list.AddAction(new SpawnShape(rectPrefab, Vector3.zero, Vector3.zero, new Vector3(0.1f, 1.0f, 1.0f),
                                          true, bridgeAction, 0.0f, 4.0f, head));
        }
        
        // "Launch a laser to the target"/Create a hurtbox bridging head and first target
        if(aimLeft)
            list.AddAction(new SpawnLaser(laserPrefab, leftTalon.transform, false, 4.0f, 0.0f, head));
        else if(aimRight)
            list.AddAction(new SpawnLaser(laserPrefab, rightTalon.transform, false, 4.0f, 0.0f, head));
        else
            list.AddAction(new SpawnLaser(laserPrefab, player.transform, true, 4.0f, 0.0f, head));

        // Create a hurtbox bridging first target to second target
        if(aimLeft)
        {
            if(rightBaseT)
            {
                aimLeft = false;
                aimRight = true;

                list.AddAction(new SpawnLaser(laserPrefab, rightTalon.transform, false, 4.5f, 0.0f, leftTalon));

                Action bridgeAction = new BridgeTargets(rightTalon.transform, leftTalon.transform, 0.0f, 4.0f, null);
                list.AddAction(new SpawnShape(rectPrefab, Vector3.zero, Vector3.zero, new Vector3(0.1f, 1.0f, 1.0f),
                                              true, bridgeAction, 0.0f, 4.0f, leftTalon));
            }
            else
            {
                list.AddAction(new SpawnLaser(laserPrefab, player.transform, true, 4.5f, 0.0f, leftTalon));

                Action bridgeAction = new BridgeTargets(player.transform, leftTalon.transform, 0.0f, 4.0f, null);
                list.AddAction(new SpawnShape(rectPrefab, Vector3.zero, Vector3.zero, new Vector3(0.1f, 1.0f, 1.0f),
                                              true, bridgeAction, 0.0f, 4.0f, leftTalon));
            }
        }
        else if(aimRight)
        {
            if(leftBaseT)
            {
                aimRight = false;
                aimLeft = true;

                list.AddAction(new SpawnLaser(laserPrefab, leftTalon.transform, false, 4.5f, 0.0f, rightTalon));

                Action bridgeAction = new BridgeTargets(rightTalon.transform, leftTalon.transform, 0.0f, 4.0f, null);
                list.AddAction(new SpawnShape(rectPrefab, Vector3.zero, Vector3.zero, new Vector3(0.1f, 1.0f, 1.0f),
                                              true, bridgeAction, 0.0f, 4.0f, leftTalon));
            }
            else
            {
                list.AddAction(new SpawnLaser(laserPrefab, player.transform, true, 4.5f, 0.0f, rightTalon));

                Action bridgeAction = new BridgeTargets(player.transform, rightTalon.transform, 0.0f, 4.0f, null);
                list.AddAction(new SpawnShape(rectPrefab, Vector3.zero, Vector3.zero, new Vector3(0.1f, 1.0f, 1.0f),
                                              true, bridgeAction, 0.0f, 4.0f, rightTalon));
            }
        }
        else
        {
            list.AddAction(new SpawnLaser(laserPrefab, player.transform, true, 4.5f, 0.0f, head));

            Action bridgeAction = new BridgeTargets(head.transform, player.transform, 0.0f, 4.0f, null);
            list.AddAction(new SpawnShape(rectPrefab, Vector3.zero, Vector3.zero, new Vector3(0.1f, 1.0f, 1.0f),
                                          true, bridgeAction, 0.0f, 4.0f, head));
        }

        // Create a hurtbox bridging second target to final target (always player)
        if(aimLeft)
        {
            list.AddAction(new SpawnLaser(laserPrefab, player.transform, true, 5.0f, 0.0f, leftTalon));

            Action bridgeAction = new BridgeTargets(player.transform, leftTalon.transform, 0.0f, 4.0f, null);
            list.AddAction(new SpawnShape(rectPrefab, Vector3.zero, Vector3.zero, new Vector3(0.1f, 1.0f, 1.0f),
                                          true, bridgeAction, 0.0f, 4.0f, leftTalon));
        }
        else if(aimRight)
        {
            list.AddAction(new SpawnLaser(laserPrefab, player.transform, true, 5.0f, 0.0f, rightTalon));

            Action bridgeAction = new BridgeTargets(player.transform, rightTalon.transform, 0.0f, 4.0f, null);
            list.AddAction(new SpawnShape(rectPrefab, Vector3.zero, Vector3.zero, new Vector3(0.1f, 1.0f, 1.0f),
                                          true, bridgeAction, 0.0f, 4.0f, rightTalon));
        }
        else
        {
            list.AddAction(new SpawnLaser(laserPrefab, player.transform, true, 5.0f, 0.0f, head));

            Action bridgeAction = new BridgeTargets(player.transform, head.transform, 0.0f, 4.0f, null);
            list.AddAction(new SpawnShape(rectPrefab, Vector3.zero, Vector3.zero, new Vector3(0.1f, 1.0f, 1.0f),
                                          true, bridgeAction, 0.0f, 4.0f, head));
        }

        // Move appendages back
        list.AddAction(new LocalTranslateToPoint(neckBase.transform.localPosition, 7.0f, 1.0f, neckBase, false, Action.EaseType.FastExitSlowEntry));
        list.AddAction(new LocalTranslateToPoint(neck1.transform.localPosition, 7.0f, 1.0f, neck1, false, Action.EaseType.FastExitSlowEntry));
        list.AddAction(new LocalTranslateToPoint(neck2.transform.localPosition, 7.0f, 1.0f, neck2, false, Action.EaseType.FastExitSlowEntry));
        list.AddAction(new LocalTranslateToPoint(head.transform.localPosition, 7.0f, 1.0f, head, false, Action.EaseType.FastExitSlowEntry));
        if(leftBaseT)
        {
            list.AddAction(new LocalTranslateToPoint(leftBase.transform.localPosition, 7.0f, 1.0f, leftBase, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslateToPoint(left1.transform.localPosition, 7.0f, 1.0f, left1, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslateToPoint(left2.transform.localPosition, 7.0f, 1.0f, left2, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslateToPoint(leftTalon.transform.localPosition, 7.0f, 1.0f, leftTalon, false, Action.EaseType.FastExitSlowEntry));
        }
        if(rightBaseT)
        {
            list.AddAction(new LocalTranslateToPoint(rightBase.transform.localPosition, 7.0f, 1.0f, rightBase, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslateToPoint(right1.transform.localPosition, 7.0f, 1.0f, right1, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslateToPoint(right2.transform.localPosition, 7.0f, 1.0f, right2, false, Action.EaseType.FastExitSlowEntry));
            list.AddAction(new LocalTranslateToPoint(rightTalon.transform.localPosition, 7.0f, 1.0f, rightTalon, false, Action.EaseType.FastExitSlowEntry));
        }

        // --------------------------------------------------------------------
        // --------------------------------------------------------------------
        
        // Ensures other boss appendages remain idle during this attack
        for(int i = 0; i < 2; i++)
        {
            IdleLeftWing();
            IdleRightWing();

            // Use locks to ensure idle anims are uninterrupted
            list.AddAction(new EmptyAction(0.0f, 4.0f, null, true));
        }

        // Determine next attack after this one finishes
        list.AddAction(new BossDecision(0.0f, 0.0f, this.gameObject));
    }
}
