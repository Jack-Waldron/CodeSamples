using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KrillHookshot : KrillConstruct
{
    // Visual element references
    public GameObject lineVFXMesh;
    public FillWithKrill fwk;
    public DisplayVoxelMesh dvm;

    // Arrival impact damage properties
    public float impactDamage = 2.0f;
    public float impactHitstun = 1.0f;

    // Wind-up properties
    public float assembleTime = 0.25f;

    // "Following" state properties
    public float followDuration = 0.2f;
    private float followLeft = 0.0f;
    private bool firstUpdate = false;

    // "Pulling" state properties
    private Vector3 startPullLocation;
    public float pullDuration = 0.2f;
    private float pullLeft = 0.0f;

    // Wind-down properties
    public float recoilTime = 0.25f;

    // Krill model line VFX properties
    public float lineSpacing = 0.5f;
    private float lineSpacingLeft;
    private List<GameObject> lineMeshes;

    // Reference to object pulled to enemy (player)
    // (Could also pull swarm entity in earlier versions)
    [HideInInspector] public GameObject pullTarget; 

    // Main references/properties
    private int stage = 0;
    private Transform tf;
    private Vector3 targetPos;
    private AI.Flocking.BoidManager bm;
    public AudioClip pullClip;
    public AudioClip fireClip;
    public AudioClip hitClip;

    // Start is called before the first frame update
    void Start()
    {
        lineMeshes = new List<GameObject>();

        tf = transform;
        targetPos = Vector3.zero;
        lineSpacingLeft = lineSpacing;
        firstUpdate = false;

        if(pullTarget.GetComponent<AI.Flocking.BoidManager>())
            bm = pullTarget.GetComponent<AI.Flocking.BoidManager>();
    }

    // Update is called once per frame
    void Update()
    {
        // Automatically destroys entity if active for too long
        backupTimer -= Time.deltaTime;
        if(backupTimer <= 0.0f)
            Destroy(gameObject);

        // Since the construct's behavior only progresses linearly, this switch-based behavior logic was ideal
        switch(stage)
        {
            case 0: // Assembly/Wind-up: hook model spawns
                assembleTime -= Time.deltaTime;
                if (assembleTime <= 0.0f) // After assembly,
                {
                    AudioManager.Instance.PlayOneShot(fireClip);
                    stage++;

                    // Proper distance/travel duration are determined
                    if(target)
                    {
                        float dist = Vector3.Distance(pullTarget.transform.position, target.transform.position);
                        
                        /*
                         * Initially, travel duration had a direct linear relationship with distance from target; 
                         * however, my team and I couldn't find a hook speed that felt good to use at all ranges. 
                         * Solution was to create three regions based on two distance thresholds; travel duration 
                         * is locked to that region's constant value, with duration increasing in the farther regions.
                         * 
                         * This created consistency that made the ability feel good to use without creating jarring
                         * behavior at extreme ranges.
                         */
                        if(dist < 4.0f)
                        {
                            followDuration /= 3.0f;
                            pullDuration /= 3.0f;
                        }
                        else if(dist < 10.0f)
                        {
                            followDuration /= 2.0f;
                            pullDuration /= 2.0f;
                        }
                    }

                    // Relevant player inputs are disabled
                    PlayerController.Instance.DisableControl(PlayerController.ControlType.Game, "move", followDuration + pullDuration + recoilTime);
                    PlayerController.Instance.DisableControl(PlayerController.ControlType.Game, "dodge", followDuration + pullDuration + recoilTime);
                    PlayerController.Instance.DisableControl(PlayerController.ControlType.Game, "attack", followDuration + pullDuration + recoilTime);
                    PlayerController.Instance.DisableControl(PlayerController.ControlType.Solo, "c_hookshot", followDuration + pullDuration + recoilTime);
                    pullTarget.transform.rotation = Quaternion.LookRotation(target.transform.position - pullTarget.transform.position);
                }
                break;
            case 1: // Following state: hook entity moves towards designated target
                if(target)
                    targetPos = target.transform.position; // Position updated in case enemy moves

                // Lerp to current target
                Vector3 oldHookPos = tf.position;
                tf.position = Vector3.Lerp(pullTarget.transform.position, targetPos, followLeft / followDuration);
                followLeft += Time.deltaTime;

                // Once hook crosses a certain distance threshold, spawn a new krill VFX model
                lineSpacingLeft -= Vector3.Distance(oldHookPos, tf.position);
                if(lineSpacingLeft <= 0.0f)
                {
                    GameObject newObj = GameObject.Instantiate(lineVFXMesh);
                    newObj.transform.position = tf.position;
                    newObj.transform.rotation = tf.rotation;
                    newObj.transform.Rotate(new Vector3(0, 90, 0));
                    lineMeshes.Add(newObj);

                    lineSpacingLeft = lineSpacing;
                }

                // Rotate player to match hook's follow direction
                Vector3 moveDiff = new Vector3(targetPos.x, 0.0f, targetPos.z) 
                                    - new Vector3(tf.position.x, 0.0f, tf.position.z);
                tf.rotation = Quaternion.LookRotation(moveDiff);
                pullTarget.transform.rotation = tf.rotation;

                // Enable hook model visuals and boid model propogation during this stage
                if(!firstUpdate)
                {
                    fwk.enabled = true;
                    dvm.enabled = true;
                    firstUpdate = true;
                }

                // Once target is reached, advance to pulling stage
                if(followLeft >= followDuration)
                {
                    stage++;
                    AudioManager.Instance.PlayOneShot(pullClip);

                    lineSpacingLeft = 0;
                    startPullLocation = pullTarget.transform.position;
                }
                break;
            case 2: // Pulling state: player is moved to hook/enemy position
                if(!target) // Early abort if enemy dies prematurely
                {
                    stage++;
                    break;
                }

                // Keep hook locked onto enemy position
                tf.position = target.transform.position;
                
                // Lerp player position towards hook
                Vector3 oldKrillPos = pullTarget.transform.position;
                pullTarget.transform.position = Vector3.Lerp(startPullLocation, target.transform.position, pullLeft / pullDuration);
                pullLeft += Time.deltaTime;

                // Erase krill VFX models once player cross same distance thresholds as before
                lineSpacingLeft += Vector3.Distance(oldKrillPos, pullTarget.transform.position);
                while(lineSpacingLeft >= lineSpacing)
                {
                    if(lineMeshes.Count != 0)
                    {
                        GameObject obj = lineMeshes[0];
                        lineMeshes.RemoveAt(0);
                        Destroy(obj);
                    }

                    lineSpacingLeft -= lineSpacing;
                }

                // Match player rotation to travel direction
                pullTarget.transform.rotation = Quaternion.LookRotation(target.transform.position - pullTarget.transform.position);

                // On arrival, create hurtbox and advance to wind-down
                if(pullLeft >= pullDuration)
                {
                    stage++;
                    AudioManager.Instance.PlayOneShot(hitClip);

                    // Damage was originally 4, pre-value doubling
                    DamageData ddata = new DamageData(impactDamage, true, impactHitstun);
                    if(pullTarget.GetComponent<Player>().overkrill)
                        ddata.fromOverkrill = true;
                    var box = DamageBox.GenerateDamageBox(ddata, Vector3.zero, transform, 0.2f,
                        new Vector3(1.0f, 1.0f, 1.0f), LayerMask.NameToLayer("PlayerHit"));
                }
                break;
            case 3: // Wind-down/recoil: destroy hook and VFX, and align player
                // After initial delay, finish ability sequence
                recoilTime -= Time.deltaTime;
                if(recoilTime <= 0.0f)
                {
                    // Rotate player towards enemy
                    Quaternion quat = new Quaternion();
                    quat.eulerAngles = new Vector3(0.0f, pullTarget.transform.rotation.eulerAngles.y, 0.0f);
                    pullTarget.transform.rotation = quat;
                    pullTarget.GetComponent<Player>().LookAt(target);

                    // Destroy ability-related objects
                    foreach(GameObject obj in lineMeshes)
                        Destroy(obj);
                    Destroy(gameObject);
                }
                break;
        }
    }
}