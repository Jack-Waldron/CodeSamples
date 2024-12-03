//////////////////////////////////////////////////////////////////////////////
// In One in a Krillion, the Hookshot is a combat ability that allows for the
// player to target an enemy and pull themselves to its location. The code
// within the three included files makes up the this ability's implementation.
//////////////////////////////////////////////////////////////////////////////

/*****************************************************************************
  Filename:  AimHookshot.cs
  Author(s): Jack Waldron
  Date: 01/19/2024
  Copyright © 2024 DigiPen (USA) Corporation.
  Description:
    Handles hookshot targeting when button is held.
*****************************************************************************/
using System;
using System.Collections.Generic;
using AI.Flocking;
using UnityEngine;
using UnityEngine.InputSystem;

public class AimHookshot : PlayerAbility 
{
  // Core properties
  float windupTime;
  float windupMax;
  List<Enemy> targetList;
  int listIndex;

  // Aiming indicator properties
  GameObject testSphere;
  Vector3 aimIndicatorOffset;
  
  public AimHookshot(Player pRef, float _windupTime, 
                     PlayerController.ControlType c, string action)
    : base(0, pRef, c, PlayerController.ActionType.start, action)
  {
    windupTime = _windupTime;
    windupMax = _windupTime;

    // Indicator is positioned slightly above enemies
    aimIndicatorOffset = new Vector3(0, 2.0f, 0);
  }

  // Starts countdown for manual aiming mode
  public override void DoAction(InputAction.CallbackContext c) 
  {
    pRef.aimCharging = true;
  }

  // Collects initial list of enemy targets for manual aiming
  private void FindTargetList()
  {
    var enemyArray = UnityEngine.Object.FindObjectsOfType<Enemy>();
    List<Enemy> enemyList = new List<Enemy>(enemyArray);

    if(enemyList.Count == 0)
    {
      targetList = null;
      return;
    }

    enemyList.RemoveAll(CheckIfDead);
    enemyList.Sort(CompareAngles);
    targetList = enemyList;

    Enemy currBest = null;
    float bestAngle = 360.0f;
    foreach (Enemy enemy in enemyList)
    {
      // Used to prevent faulty targeting of tutorial dummy enemy
      if(enemy.gameObject.name == "Tutorial Fish(Clone)" && 
         Vector3.Distance(pRef.transform.position, enemy.transform.position) > 15.0f)
        continue;

      float currAngle = Vector3.SignedAngle(pRef.transform.forward, 
                                            enemy.transform.position - pRef.transform.position, 
                                            pRef.transform.up);
      if(Mathf.Abs(currAngle) < bestAngle)
      {
          currBest = enemy;
          bestAngle = currAngle;
      }
    }

    if(!currBest)
    {
      targetList = null;
      return;
    }

    listIndex = targetList.IndexOf(currBest);
    pRef.lastEnemyAttacked = currBest.gameObject;
  }

  private bool CheckIfDead(Enemy enemy)
  {
    return enemy.IsDead;
  }

  private int CompareAngles(Enemy x, Enemy y)
  {
    float xAngle = Vector3.SignedAngle(pRef.transform.forward, 
                                       x.transform.position - pRef.transform.position, 
                                       pRef.transform.up);
    float yAngle = Vector3.SignedAngle(pRef.transform.forward, 
                                       y.transform.position - pRef.transform.position, 
                                       pRef.transform.up);

    if(xAngle < yAngle)
      return 1;
    else
      return -1;
  }

  public override void Update() 
  {
    if(!pRef.aiming) // If in standard movement mode,
    {
      if(pRef.aimCharging) // If countdown for manual aiming is active,
      {
        windupTime -= Time.deltaTime;
        if(windupTime <= 0.0f)
        {
          // After countdown clears, get initial list to see if there are targets
          FindTargetList();
          if(targetList == null || targetList.Count == 0)
          {
            windupTime = windupMax;
            return;
          }

          // Activate manual aiming
          pRef.aiming = true;
          windupTime = windupMax;

          // Spawn/configure aiming indicator over first selected enemy
          Debug.Log("Aiming ready!");
          testSphere = GameObject.Instantiate(pRef.testSphere, 
                                   targetList[listIndex].transform.position + aimIndicatorOffset,
                                   Quaternion.identity);
          testSphere.transform.Rotate(90, 0, 0);
          pRef.aimIndicatorRef = testSphere;
        }
      }
    }
    else // Manual aiming mode
    {
      // Stops player in-place to allow for precise directional aiming
      // (Velocity must be non-zero for player model rotation to work)
      pRef.gameObject.GetComponent<Rigidbody>().velocity /= 1000.0f;

      // FindDefaultTarget returns enemy based primarily on smallest angle from look direction
      // (Overriding lastEnemyAttacked w/ this result stores it as the desired hookshot target)
      pRef.lastEnemyAttacked = pRef.FindDefaultTarget().gameObject;
      testSphere.transform.position = pRef.lastEnemyAttacked.transform.position + aimIndicatorOffset;
    }
  }
}

/*
//Excerpt from Player.cs for reference

public Enemy FindDefaultTarget()
{
    var EnemyList = FindObjectsOfType<Enemy>();

    Enemy currBest = null;
    float bestAngle = 360.0f;
    float bestDist = 100.0f;

    foreach (Enemy enemy in EnemyList)
    {
        if(enemy.IsDead)
            continue;
        if(enemy.gameObject.name == "Tutorial Fish(Clone)" && 
           (Vector3.Distance(transform.position, enemy.transform.position) > 30.0f))
            continue;

        float currAngle = Vector3.Angle(transform.forward, enemy.transform.position - transform.position);
        float currDist = (enemy.transform.position - transform.position).magnitude;

        // Should prioritize smaller look angle, but ideally won't aggressively jump to really far, smaller angle targets
        // Need to mess with modifier to bestDist to achieve this
        if((currAngle <= bestAngle) && (currDist <= (bestDist * 10.0f)))
        {
            currBest = enemy;
            bestAngle = currAngle;
            bestDist = currDist;
        }
    }

    return currBest;
}
*/