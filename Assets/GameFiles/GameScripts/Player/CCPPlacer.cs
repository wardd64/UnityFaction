using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Custom CheckPoint placer
/// </summary>
public class CCPPlacer : MonoBehaviour {

    private LinkedList<Vector3> checkPoints;
    private LinkedListNode<Vector3> currentPoint;

    private const float DELAY = 1f;
    private const float MAX_DELTA = 1e-3f;
    private const float COOLDOWN = 2f;
    private const float SNAP_TIME = .05f;

    private bool firstFrame;
    private bool active;
    private Vector3 checkPosition;
    private float timer;

    private PlayerLife player { get { return GetComponent<PlayerLife>(); } }

    private void Update() {
        if(checkPoints == null)
            checkPoints = new LinkedList<Vector3>();

        bool tryPlace = Global.input.GetKey("PlaceCCP");
        bool tryUse = Global.input.GetKey("UseCCP");
        bool forward = Global.input.GetKeyDown("Forward") || Global.input.GetKeyDown("Right");
        bool backward = Global.input.GetKeyDown("Backward") || Global.input.GetKeyDown("Left");
        bool tryGo = !firstFrame && (Global.input.GetKeyDown("jump") || Global.input.GetKeyDown("UseCCP"));

        bool trying = false;

        firstFrame = false;

        if(timer < 0f) {
            //negative timer: cooldown state
            timer += Time.deltaTime;
            if(timer > 0f)
                timer = 0f;

            trying = tryGo || tryUse || tryPlace;
        }
        else if(timer == 0f) {
            //zero timer: off state, check for activations

            if(active) {

                //dead state; continue
                if(tryGo)
                    Continue();

                //dead state; moving
                else if(forward || backward)
                    MoveCCP(forward);
                
            }
            else if(!player.isDead && Global.save.ccpAllowed && (tryPlace || tryUse)){
                //alive state: need to have delay, start counting.
                checkPosition = transform.position;
                timer = Time.deltaTime;
            }
        }
        else if(timer < DELAY) {
            //positive timer: alive and counting

            bool reset = (!tryPlace && !tryUse) || !InPosition(checkPosition);
            
            //cancel timer
            if(reset)
                timer = -.5f;

            //continue counting
            else
                timer += Time.deltaTime;

            trying = true;
        }
        else {
            //all done; perform action

            //place new CPP
            if(tryPlace)
                PlaceCPP();

            //die and start using CPP
            else
                StartUsingCCP(); 
        }

        active &= player.isDead;
        if(active) {
            float r = UFUtils.LerpExpFactor(SNAP_TIME);
            transform.position = Vector3.Lerp(transform.position, currentPoint.Value, r);
        }

        
        Global.hud.SetCCPProgress(timer, COOLDOWN, DELAY, trying);
    }

    private void PlaceCPP() {
        timer = -COOLDOWN;
        AddCheckPoint();
        Global.hud.PlacedCP();
    }

    private void AddCheckPoint() {
        checkPoints.AddLast(transform.position);
    }

    private void MoveCCP(bool forward) {
        if(forward && currentPoint.Next != null)
            currentPoint = currentPoint.Next;
        else if(!forward && currentPoint.Previous != null)
            currentPoint = currentPoint.Previous;
    }

    private void Continue() {
        timer = -COOLDOWN;
        transform.position = currentPoint.Value;
        player.Continue();
        Global.hud.UsedCP();
        while(checkPoints.Last != currentPoint)
            checkPoints.RemoveLast();
    }


    private bool InPosition(Vector3 position) {
        Vector3 delta = transform.position - position;
        return delta.magnitude < MAX_DELTA;
    }

    public void StartUsingCCP() {
        timer = 0f;

        if(!player.isDead) {
            AddCheckPoint();
            player.SoftDie();
        }

        if(checkPoints.Count > 0)
            currentPoint = checkPoints.Last;
        else
            player.Respawn();

        active = true;
        firstFrame = true;
    }
}
