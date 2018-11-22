using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinerTool : PlayerTool {

    public Transform aimRig;
    public TextMesh text;

    public AudioClip cdReadyClip, mineReadyClip, mineClip; 

    private bool active;
    private int mineAmmo;
    private float timer;

    private bool canMine;
    Vector3 entryPoint;
    private Vector3 exitPoint;
    private Vector3 mineDirection;
    private float cost;

    private const float CHARGE_TIME = 2f;
    private const float COOLDOWN = 5f;
    private const float MAX_ENTRY_DIST = 2f;
    private const float MAX_MINE_DIST = 100f;
    private const float CHECK_INTERVAL = 1f;

    /*
     * Miner tool replaces geomod feature (temporarily)
     * Conditions for mining teleportation are:
     * 
     * Player must have collected an explosive weapon
     * Player must be within specific range of entry point
     * Along the (anti)normal, there must be an exit point within range
     * The exit point must have enough space to put down the player
     * The path from entry to exit must not contain points with level hardness 100
     * Player must be able to pay ammo based on integral of level hardness over distance
     */

    public override void DoUpdate(bool mainFire, bool alt) {
        if(timer < 0f) {
            string time = UFUtils.GetShortFormat(-timer, 3);
            string duration = UFUtils.GetShortFormat(CHARGE_TIME, 3);
            text.text = "Cl-\ndwn\n" + time + "\n/" + duration;
            timer += Time.deltaTime;
            if(timer > 0f) {
                timer = 0f;
                sound.PlayOneShot(cdReadyClip);
            }
            aimRig.gameObject.SetActive(false);
        }
        else if(active) {

            bool previousCanMine = canMine;
            canMine = false;
            CheckMine();
            if(mainFire && canMine) {
                timer += Time.deltaTime;
                if(timer > CHARGE_TIME) {
                    Mine();
                    timer = -COOLDOWN;
                }
            }
            else
                timer = 0f;

            if(canMine && !previousCanMine && !mainFire)
                sound.PlayOneShot(mineReadyClip);
        }
        else {
            text.text = "Nd\nEqp.\n\nFnd\nexpl\nwep";
            aimRig.gameObject.SetActive(false);
        }
    }

    private void CheckMine() {
        aimRig.gameObject.SetActive(true);
        RaycastHit hit = Raycast(aimRig);
        entryPoint = hit.point;
        mineDirection = fpCamera.transform.forward;

        if(hit.collider == null) {
            text.text = "No\nTar-\nget";
            return;
        }

        text.text = "Non-\nmin-\nable";

        if(hit.collider.name != "StaticVisible")
            return;

        if(hit.collider.GetComponent<UFLiquid>() != null)
            return;

        if(hit.collider.transform.parent != null) {
            switch(hit.collider.transform.parent.name) {
            case "Destructible":
            case "Scrollers":
            case "Moving geometry":
            case "PortalGeometry":
            return;
            }
        }

        int baseHardness = UFLevel.geo.GetHardness(entryPoint);
        if(baseHardness >= 100) {
            text.text = "Ind-\nest-\nruct-\nible";
            return;
        }
        cost += baseHardness;

        if(hit.distance > MAX_ENTRY_DIST) {
            string dist = UFUtils.GetShortFormat(hit.distance, 3);
            string maxDist = UFUtils.GetShortFormat(MAX_ENTRY_DIST, 3);
            text.text = "Too\nfar\n\n" + dist + "\n/" + maxDist;
            return;
        }

        text.text = "rdy";
        FindMinePath();
    }

    private void FindMinePath() {
        cost = 0f;

        for(float x = CHECK_INTERVAL; x <= MAX_MINE_DIST; x += CHECK_INTERVAL) {
            Vector3 probe = entryPoint + mineDirection * x;
            int hardness = UFLevel.geo.GetHardness(probe);

            if(hardness >= 100) {
                text.text = "Path\nobs-\ntrc-\nted";
                return;
            }

            cost += hardness;

            Ray backRay = new Ray(probe, -mineDirection);
            RaycastHit hit;
            if(Physics.Raycast(backRay, out hit, CHECK_INTERVAL, targetMask)) {
                exitPoint = hit.point;
                CheckMinePath();
                return;
            }
                
        }

        //could not find exit point
        text.text = "No\nexit\npnt";
    }

    private void CheckMinePath() {

        //check for obstructions
        Vector3[] checkPoints = new Vector3[] {
            new Vector3(0f, 1f, .5f), new Vector3(0f, 0f, .8f), new Vector3(0f, -0f, .5f),
            new Vector3(.5f, 0f, .5f),  new Vector3(-.5f, 0f, .5f)
        };
        foreach(Vector3 cp in checkPoints) {
            Quaternion q = Quaternion.LookRotation(mineDirection);
            Vector3 cpr = q * cp;

            Ray checkRay = new Ray(exitPoint, cpr);
            if(Physics.Raycast(checkRay, cpr.magnitude, targetMask)) {
                text.text = "Exit\npnt\nobs-\ntrc-\nted";
                return;
            }
        }

        //check cost
        if(cost > mineAmmo) {
            string money = UFUtils.GetShortFormat(mineAmmo, 3);
            string price = UFUtils.GetShortFormat(cost, 3);
            text.text = "Not\nengh\nammo\n" + money + "\n/" + price;
        }

        //all conditions met!
        canMine = true;

        if(timer == 0f)
            text.text = "Rdy!";
        else {
            string charge = UFUtils.GetShortFormat(timer, 3);
            string goal = UFUtils.GetShortFormat(CHARGE_TIME, 3);
            text.text = "Chr-\nging\n" + charge + "\n/" + goal;
        }
        
    }

    private void Mine() {
        Vector3 exitBase = exitPoint + .5f * mineDirection +.8f * Vector3.down;
        player.transform.position = exitBase;
        sound.PlayOneShot(mineClip);
    }

    public void AddAmmo(UFItem item) {
        mineAmmo += 100;
    }

    public void FoundWeapon() {
        if(!active)
            mineAmmo = 500;
        active = true;
    }

    public void Reset() {
        active = false;
    }
}
