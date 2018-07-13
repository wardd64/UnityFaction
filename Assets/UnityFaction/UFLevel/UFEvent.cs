using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UFEvent : MonoBehaviour {

    //dynamic variables
    private bool positiveSignal;
    private float timer;

    //general variables
    public UFLevelStructure.Event.EventType type;
    public float delay;

    //event data
    public bool bool1, bool2;
    public int int1, int2;
    public float float1, float2;
    public string string1, string2;
    public int[] links;
    public Color color;

    public void Set(UFLevelStructure.Event e){
        type = e.type;
        delay = Mathf.Max(0f, e.delay);

        bool1 = e.bool1;
        bool2 = e.bool1;
        int1 = e.int1;
        int2 = e.int2;
        float1 = e.float1;
        float2 = e.float2;
        string1 = e.string1;
        string2 = e.string2;
        links = e.links;
        color = e.color;

        UFLevel.SetObject(e.transform.id, gameObject);
    }

    public void SetAudio(AudioClip clip) {
        AudioSource sound = gameObject.AddComponent<AudioSource>();
        sound.volume = 1f;
        sound.clip = clip;

        switch(type) {

        case UFLevelStructure.Event.EventType.Music_Start:
        sound.loop = bool1;
        bool useEffectsVolume = bool2;
        //TODO set appropriate mixer channel
        break;

        case UFLevelStructure.Event.EventType.Play_Sound:
        //TODO set appropriate mixer channel
        break;

        }
    }

    private void Start() {

    }

    private void Update() {
        if(timer > 0f)
            timer += Time.deltaTime;

        if(timer > delay) {
            EventTypeClass etc = GetEventTypeClass(type);
            if(etc == EventTypeClass.Signal) {
                switch(type) {

                case UFLevelStructure.Event.EventType.Delay:
                Trigger(positiveSignal);
                timer = 0f;
                break;

                case UFLevelStructure.Event.EventType.Cyclic_Timer:
                Trigger(positiveSignal);
                timer -= delay;
                break;

                case UFLevelStructure.Event.EventType.Invert:
                Trigger(!positiveSignal);
                timer = 0f;
                break;
                }
            }
            else if(GetEventTypeClass(type) == EventTypeClass.Effect) {
                if(positiveSignal)
                    DoEffect(positiveSignal);
                timer = 0f;
            }
        }
           
        
    }

    public void Activate(bool positive) {
        if(timer == 0f) {
            positiveSignal = positive;
            timer = Time.deltaTime;
        }

    }

    private void Trigger(bool positive) {
        foreach(int link in links)
            UFTrigger.Activate(link);
    }

    private void DoEffect(bool positive) {
        switch(type) {

        case UFLevelStructure.Event.EventType.Delay:

        break;

        case UFLevelStructure.Event.EventType.Teleport:
        foreach(int link in links) {
            Transform t = UFLevel.GetByID(link).objectRef.transform;
            t.position = transform.position;
            t.rotation = transform.rotation;
        }
        break;

        case UFLevelStructure.Event.EventType.Teleport_Player:
        Transform player = UFLevel.GetPlayer<Transform>();
        player.position = transform.position;
        float rot = transform.rotation.eulerAngles.y;
        player.rotation = Quaternion.Euler(0f, rot, 0f);
        break;

        case UFLevelStructure.Event.EventType.Music_Start:
        this.GetComponent<AudioSource>().Play();
        break;

        case UFLevelStructure.Event.EventType.Music_Stop:
        float fadeTime = float1;
        StartCoroutine(FadeAudioSource(fadeTime, 0f));
        break;

        case UFLevelStructure.Event.EventType.Particle_State:
        //TODO
        break;

        case UFLevelStructure.Event.EventType.Mover_Pause:
        break;

        case UFLevelStructure.Event.EventType.Reverse_Mover:
        bool setForwardIfMoving = int1 != 0; //reverse otherwise
        //TODO
        break;

        case UFLevelStructure.Event.EventType.Modify_Rotating_Mover:
        //TODO
        break;

        case UFLevelStructure.Event.EventType.Invert:
        //TODO
        break;

        case UFLevelStructure.Event.EventType.Explode:
        //TODO
        break;

        case UFLevelStructure.Event.EventType.Continuous_Damage:
        UFLevel.GetPlayer<UFPlayerLife>().TakeDamage(float1);
        break;

        default:
        Debug.LogError("Event type " + type + " not implemented");
        break;
        }
    }

    private IEnumerator FadeAudioSource(float time, float targetVolume) {
        AudioSource s = GetComponent<AudioSource>();
        while(s.volume != targetVolume) {
            s.volume = Mathf.MoveTowards(s.volume, targetVolume, Time.deltaTime / time);
            yield return null;
        }
        
    }

    private enum EventTypeClass {
        None, StartTrigger, Signal, Detector, Effect, 
    }

    private static EventTypeClass GetEventTypeClass(UFLevelStructure.Event.EventType type) {
        switch(type) {
        case UFLevelStructure.Event.EventType.StartTrigger:
        case UFLevelStructure.Event.EventType.When_Dead:
        return EventTypeClass.StartTrigger;

        case UFLevelStructure.Event.EventType.Delay:
        case UFLevelStructure.Event.EventType.Invert:
        case UFLevelStructure.Event.EventType.Cyclic_Timer:
        return EventTypeClass.Signal;

        case UFLevelStructure.Event.EventType.Bolt_state:
        case UFLevelStructure.Event.EventType.Continuous_Damage:
        case UFLevelStructure.Event.EventType.Explode:
        case UFLevelStructure.Event.EventType.Heal:
        case UFLevelStructure.Event.EventType.Message:
        case UFLevelStructure.Event.EventType.Music_Start:
        case UFLevelStructure.Event.EventType.Music_Stop:
        case UFLevelStructure.Event.EventType.Particle_State:
        case UFLevelStructure.Event.EventType.Play_Sound:
        case UFLevelStructure.Event.EventType.Remove_Object:
        case UFLevelStructure.Event.EventType.Teleport:
        case UFLevelStructure.Event.EventType.Set_Gravity:
        case UFLevelStructure.Event.EventType.Push_Region_State: 
        case UFLevelStructure.Event.EventType.Display_Fullscreen_Image:
        return EventTypeClass.Effect;

        case UFLevelStructure.Event.EventType.When_Countdown_Over:
        case UFLevelStructure.Event.EventType.When_Enter_Vehicle:
        case UFLevelStructure.Event.EventType.When_Try_Exit_Vehicle:
        case UFLevelStructure.Event.EventType.When_Cutscene_Over:
        case UFLevelStructure.Event.EventType.When_Countdown_Reach:
        case UFLevelStructure.Event.EventType.When_Life_Reaches:
        case UFLevelStructure.Event.EventType.When_Armor_Reaches:
        case UFLevelStructure.Event.EventType.Reverse_Mover:
        case UFLevelStructure.Event.EventType.When_Hit:
        return EventTypeClass.Detector;

        default:
        return EventTypeClass.None;

        /*
        case UFLevelStructure.Event.EventType.Drop_Weapon:
        case UFLevelStructure.Event.EventType.Ignite_Entity:
        case UFLevelStructure.Event.EventType.Defuse_Nuke:
        case UFLevelStructure.Event.EventType.Never_Leave_Vehicle:
        case UFLevelStructure.Event.EventType.Fire_Weapon_No_Anim:
        case UFLevelStructure.Event.EventType.Drop_Point_Marker:
        case UFLevelStructure.Event.EventType.Follow_Player:
        case UFLevelStructure.Event.EventType.Follow_Waypoints:
        case UFLevelStructure.Event.EventType.Give_item_To_Player:
        case UFLevelStructure.Event.EventType.Goal_Create:
        case UFLevelStructure.Event.EventType.Goal_Check:
        case UFLevelStructure.Event.EventType.Goal_Set:
        case UFLevelStructure.Event.EventType.Goto:
        case UFLevelStructure.Event.EventType.Goto_Player:
        case UFLevelStructure.Event.EventType.Activate_Capek_Shield:
        case UFLevelStructure.Event.EventType.Load_Level:
        case UFLevelStructure.Event.EventType.Look_At:
        case UFLevelStructure.Event.EventType.Make_Invulnerable: 
        case UFLevelStructure.Event.EventType.Make_Fly:
        case UFLevelStructure.Event.EventType.Make_Walk:
        case UFLevelStructure.Event.EventType.Play_Animation:
        case UFLevelStructure.Event.EventType.Set_AI_Mode:
        case UFLevelStructure.Event.EventType.Slay_Object:
        case UFLevelStructure.Event.EventType.Set_Light_State:
        case UFLevelStructure.Event.EventType.Set_Liquid_Depth:
        case UFLevelStructure.Event.EventType.Set_Friendliness:
        case UFLevelStructure.Event.EventType.Shake_Player:
        case UFLevelStructure.Event.EventType.Shoot_At:
        case UFLevelStructure.Event.EventType.Shoot_Once:
        case UFLevelStructure.Event.EventType.Armor:
        case UFLevelStructure.Event.EventType.Spawn_Object:
        case UFLevelStructure.Event.EventType.Swap_Textures:
        case UFLevelStructure.Event.EventType.Switch:
        case UFLevelStructure.Event.EventType.Switch_Model:
        case UFLevelStructure.Event.EventType.Alarm:
        case UFLevelStructure.Event.EventType.Alarm_Siren:
        case UFLevelStructure.Event.EventType.Go_Undercover:
        case UFLevelStructure.Event.EventType.Monitor_State:
        case UFLevelStructure.Event.EventType.UnHide:
        case UFLevelStructure.Event.EventType.Headlamp_State:
        case UFLevelStructure.Event.EventType.Item_Pickup_State:
        case UFLevelStructure.Event.EventType.Cutscene:
        case UFLevelStructure.Event.EventType.Strip_Player_Weapons:
        case UFLevelStructure.Event.EventType.Fog_State:
        case UFLevelStructure.Event.EventType.Detach:
        case UFLevelStructure.Event.EventType.Skybox_State:
        case UFLevelStructure.Event.EventType.Force_Monitor_Update:
        case UFLevelStructure.Event.EventType.Black_Out_Player:
        case UFLevelStructure.Event.EventType.Turn_Off_Physics:
        case UFLevelStructure.Event.EventType.Teleport_Player:
        case UFLevelStructure.Event.EventType.Holster_Weapon:
        case UFLevelStructure.Event.EventType.Holster_Player_Weapon:
        case UFLevelStructure.Event.EventType.Modify_Rotating_Mover:
        case UFLevelStructure.Event.EventType.Clear_Endgame_If_Killed:
        case UFLevelStructure.Event.EventType.Win_PS2_Demo:
        case UFLevelStructure.Event.EventType.Enable_Navpoint:
        case UFLevelStructure.Event.EventType.Play_Vclip:
        case UFLevelStructure.Event.EventType.Endgame:
        case UFLevelStructure.Event.EventType.Mover_Pause:
        case UFLevelStructure.Event.EventType.Countdown_begin:
        case UFLevelStructure.Event.EventType.Countdown_End:
        */

        }
            

    }
}
