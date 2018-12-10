using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

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
    public Object obj;

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

        if(GetEventTypeClass(type) == EventTypeClass.None)
            Debug.LogWarning("Event " + name + " will have no effects since it is of unknown type: " + type);
    }

    public void SetAudio(AudioClip clip, AudioMixerGroup musicChannel, AudioMixerGroup effectsChannel) {
        AudioSource sound = gameObject.AddComponent<AudioSource>();
        sound.volume = 1f;
        sound.clip = clip;
        sound.playOnAwake = false;

        switch(type) {

        case UFLevelStructure.Event.EventType.Music_Start:
        sound.loop = bool1;
        if(bool2)
            sound.outputAudioMixerGroup = effectsChannel;
        else
            sound.outputAudioMixerGroup = musicChannel;
        break;

        case UFLevelStructure.Event.EventType.Play_Sound:
        sound.outputAudioMixerGroup = effectsChannel;
        break;

        }
    }

    private void Start() {
        EventTypeClass etc = GetEventTypeClass(type);
        if(etc == EventTypeClass.StartTrigger)
            Trigger(true);
    }

    private void Update() {
        if(GetEventTypeClass(type) == EventTypeClass.Detector) {
            if(Detect())
                Trigger(true);

            timer = 0f;
            return;
        }

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

                case UFLevelStructure.Event.EventType.Switch:
                bool1 = !bool1;
                Trigger(bool1);
                timer = 0f;
                break;
                }
            }
            else if(etc == EventTypeClass.Effect) {
                
                IDRef.Type ignoredType = DoEffect(positiveSignal);
                Trigger(positiveSignal, ignoredType);
                timer = 0f;
            }
            else if(etc == EventTypeClass.ContinuousEffect) {
                DoContinuousEffect();
            }
            else {
                Trigger(positiveSignal);
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

    public void Deactivate() {
        if(GetEventTypeClass(type) == EventTypeClass.ContinuousEffect)
            timer = 0f;
    }

    public void DoContinuousEffect() {
        switch(type) {

        case UFLevelStructure.Event.EventType.Continuous_Damage:
        //TODO use damage type (int2)
        float dps = int1;
        if(int1 <= 0)
            dps = float.PositiveInfinity;
        UFLevel.GetPlayer<UFPlayerLife>().TakeDamage(Time.deltaTime * dps, int2, true);
        break;

        default:
        Debug.LogError("Event type " + type + " not implemented");
        break;
        }
    }

    private void Trigger(bool positive, IDRef.Type ignoredType = IDRef.Type.None) {
        if(ignoredType == IDRef.Type.All)
            return;

        foreach(int link in links) {
            if(UFLevel.GetByID(link).type != ignoredType)
                UFTrigger.Activate(link, positive);
        }
            
    }

    /// <summary>
    /// Checks if conditions for this detector event are met and returns true if so
    /// </summary>
    private bool Detect() {

        //preperatory variables
        float countDownValue = UFLevel.GetPlayer<UFPlayerMovement>().GetCountDownValue();

        switch(type) {
        case UFLevelStructure.Event.EventType.When_Countdown_Over:
        return countDownValue - Time.deltaTime <= 0f && countDownValue > 0f;

        case UFLevelStructure.Event.EventType.When_Countdown_Reaches:
        return countDownValue - Time.deltaTime <= int1 && countDownValue > int1;

        default:
        Debug.LogError("Event type " + type + " not implemented");
        return false;
        }
    }

    /// <summary>
    /// Perform effects of this event. 
    /// Returns the type of IDRef that is used up when performing the event,
    /// these references do not get activated in the usual sense by this 
    /// event type.
    /// </summary>
    private IDRef.Type DoEffect(bool positive) {

        //preperatory variables
        Transform playerTr = UFLevel.GetPlayer<Transform>();
        UFPlayerLife playerLi = UFLevel.GetPlayer<UFPlayerLife>();
        UFPlayerMovement playerMo = UFLevel.GetPlayer<UFPlayerMovement>();
        AudioSource sound = this.GetComponent<AudioSource>();

        //find effect type and do its effects. Return IDRefts that were used up.
        switch(type) {

        case UFLevelStructure.Event.EventType.Teleport:
        foreach(int link in links) {
            Transform t = UFLevel.GetByID(link).objectRef.transform;
            t.position = transform.position;
            t.rotation = transform.rotation;
        }
        return IDRef.Type.None;

        case UFLevelStructure.Event.EventType.Teleport_Player:
        playerTr.position = transform.position;
        playerTr.GetComponent<CharacterController>().Move(Vector3.zero);
        playerMo.SetRotation(transform.rotation);
        return IDRef.Type.None;

        case UFLevelStructure.Event.EventType.Music_Start:
        if(sound.clip == null)
            Debug.LogWarning("Music event has no audio clip assigned: " + name);
        sound.Play();
        return IDRef.Type.None;

        case UFLevelStructure.Event.EventType.Play_Sound:
        sound.rolloffMode = AudioRolloffMode.Linear;
        if(float1 >= 0f) {
            sound.minDistance = float1;
            sound.maxDistance = float1 * 2f;
            sound.spatialBlend = 1f;
        }
        else
            sound.spatialBlend = 0f;
        if(sound.clip == null)
            Debug.LogWarning("Play sound event has no audio clip assigned: " + name);
        sound.loop = bool1;
        sound.Play();
        if(float2 > 0f && !bool1)
            sound.SetScheduledEndTime(float2);
        return IDRef.Type.None;

        case UFLevelStructure.Event.EventType.Music_Stop:
        float fadeTime = float1;
        foreach(UFEvent e in GetEventsOfType(UFLevelStructure.Event.EventType.Music_Start))
            StartCoroutine(e.FadeAudioSource(fadeTime, 0f));
        return IDRef.Type.None;

        case UFLevelStructure.Event.EventType.Particle_State:
        foreach(UFParticleEmitter pem in GetLinksOfType<UFParticleEmitter>(IDRef.Type.ParticleEmitter))
            pem.Activate(positive);
        return IDRef.Type.ParticleEmitter;

        case UFLevelStructure.Event.EventType.Mover_Pause:
        foreach(UFMover mov in GetLinksOfType<UFMover>(IDRef.Type.Keyframe))
            mov.Activate(!positive);
        return IDRef.Type.Keyframe;

        case UFLevelStructure.Event.EventType.Reverse_Mover:
        bool setForwardIfMoving = int1 != 0;
        foreach(UFMover mov in GetLinksOfType<UFMover>(IDRef.Type.Keyframe))
            mov.Reverse(setForwardIfMoving);
        return IDRef.Type.Keyframe;

        case UFLevelStructure.Event.EventType.Modify_Rotating_Mover:
        bool increase = string1.Equals("Increase");
        float factor = increase ? 1f + (float1 / 100f) : 1f - (float1 / 100f);
        foreach(UFMover mov in GetLinksOfType<UFMover>(IDRef.Type.Keyframe))
            mov.ChangeRotationSpeed(factor);
        return IDRef.Type.Keyframe;

        case UFLevelStructure.Event.EventType.Explode:
        float radius = float1;
        float damage = float2;
        bool geo = bool1;
        float dist = (playerTr.position - transform.position).magnitude;
        if(dist < radius)
            playerLi.TakeDamage(damage, UFPlayerLife.DamageType.Explosive, false);
        if(geo)
            Debug.LogWarning("Explosion " + name + " requested geo mod; this will not work as of now!");
        GameObject explosionPrefab = obj as GameObject;
        if(explosionPrefab != null) {
            Vector3 explPos = transform.position;
            Quaternion explRot = transform.rotation;
            GameObject explosion = Instantiate(explosionPrefab, explPos, explRot, transform);
            explosion.transform.localScale = radius * 2f * Vector3.one;
        }
        else
            Debug.LogWarning("Explosion prefab for event " + name + " was not provided!");
        return IDRef.Type.None;

        case UFLevelStructure.Event.EventType.Skybox_State:
        UFLevel.playerInfo.SetSkyboxRotation(string1, float1);
        return IDRef.Type.None;

        case UFLevelStructure.Event.EventType.Bolt_State:
        foreach(UFBoltEmitter bem in GetLinksOfType<UFBoltEmitter>(IDRef.Type.BoltEmitter))
            bem.Activate(positive);
        return IDRef.Type.BoltEmitter;

        case UFLevelStructure.Event.EventType.Push_Region_State:
        foreach(UFForceRegion pr in GetLinksOfType<UFForceRegion>(IDRef.Type.PushRegion))
            pr.Activate(positive);
        return IDRef.Type.PushRegion;

        case UFLevelStructure.Event.EventType.Countdown_Begin:
        UFLevel.singleton.SetCountDown(int1);
        return IDRef.Type.None;

        case UFLevelStructure.Event.EventType.Countdown_End:
        UFLevel.singleton.SetCountDown(0f);
        return IDRef.Type.None;

        case UFLevelStructure.Event.EventType.Remove_Object:
        foreach(int link in links) {
            GameObject g = UFLevel.GetByID(link).objectRef;
            if(g == null)
                Debug.LogWarning("Trying to remove ID ref that does not exist: " + link);
            else
                g.SetActive(false);
        }
        return IDRef.Type.All;

        case UFLevelStructure.Event.EventType.Heal:
        if(bool1) {
            UFLevel.GetPlayer<UFPlayerLife>().GainHealth(int1);
            return IDRef.Type.None;
        }
        else {
            Debug.LogError("Entity heal not implemented");
            return IDRef.Type.Entity;
        }

        case UFLevelStructure.Event.EventType.Set_Gravity:
        Physics.gravity = float1 * Vector3.down;
        return IDRef.Type.None;

        default:
        Debug.LogError("Event type " + type + " not implemented");
        return IDRef.Type.None;
        }
    }

    private IEnumerator FadeAudioSource(float time, float targetVolume) {
        AudioSource s = GetComponent<AudioSource>();

        if(!s.isPlaying || s.time <= Time.deltaTime)
            yield break;

        while(s.volume != targetVolume) {
            s.volume = Mathf.MoveTowards(s.volume, targetVolume, Time.deltaTime / time);
            yield return null;
        }
    }

    private enum EventTypeClass {
        None, StartTrigger, Signal, Detector, Effect, ContinuousEffect
    }

    private List<T> GetLinksOfType<T>(IDRef.Type type) where T : Component{
        List<T> toReturn = new List<T>();
        foreach(int link in links) {
            if(UFLevel.GetByID(link).type == type || type == IDRef.Type.All)
                toReturn.Add(UFLevel.GetByID(link).objectRef.GetComponent<T>());
        }
        return toReturn;
    }

    private List<UFEvent> GetEventsOfType(UFLevelStructure.Event.EventType type) {
        UFEvent[] allEvents = this.transform.parent.GetComponentsInChildren<UFEvent>();
        List<UFEvent> toReturn = new List<UFEvent>();
        foreach(UFEvent e in allEvents) {
            if(e.type == type)
                toReturn.Add(e);
        }
        return toReturn;
    }

    private static EventTypeClass GetEventTypeClass(UFLevelStructure.Event.EventType type) {
        switch(type) {
        case UFLevelStructure.Event.EventType.StartTrigger:
        case UFLevelStructure.Event.EventType.When_Dead:
        return EventTypeClass.StartTrigger;

        case UFLevelStructure.Event.EventType.Delay:
        case UFLevelStructure.Event.EventType.Invert:
        case UFLevelStructure.Event.EventType.Cyclic_Timer:
        case UFLevelStructure.Event.EventType.Switch:
        return EventTypeClass.Signal;

        case UFLevelStructure.Event.EventType.Bolt_State:
        case UFLevelStructure.Event.EventType.Explode:
        case UFLevelStructure.Event.EventType.Heal:
        case UFLevelStructure.Event.EventType.Message:
        case UFLevelStructure.Event.EventType.Music_Start:
        case UFLevelStructure.Event.EventType.Music_Stop:
        case UFLevelStructure.Event.EventType.Particle_State:
        case UFLevelStructure.Event.EventType.Play_Sound:
        case UFLevelStructure.Event.EventType.Remove_Object:
        case UFLevelStructure.Event.EventType.Teleport:
        case UFLevelStructure.Event.EventType.Teleport_Player:
        case UFLevelStructure.Event.EventType.Set_Gravity:
        case UFLevelStructure.Event.EventType.Push_Region_State: 
        case UFLevelStructure.Event.EventType.Display_Fullscreen_Image:
        case UFLevelStructure.Event.EventType.Modify_Rotating_Mover:
        case UFLevelStructure.Event.EventType.Reverse_Mover:
        case UFLevelStructure.Event.EventType.Mover_Pause:
        case UFLevelStructure.Event.EventType.Skybox_State:
        case UFLevelStructure.Event.EventType.Countdown_Begin:
        case UFLevelStructure.Event.EventType.Countdown_End:
        return EventTypeClass.Effect;

        case UFLevelStructure.Event.EventType.Continuous_Damage:
        return EventTypeClass.ContinuousEffect;

        case UFLevelStructure.Event.EventType.When_Countdown_Over:
        case UFLevelStructure.Event.EventType.When_Enter_Vehicle:
        case UFLevelStructure.Event.EventType.When_Try_Exit_Vehicle:
        case UFLevelStructure.Event.EventType.When_Cutscene_Over:
        case UFLevelStructure.Event.EventType.When_Countdown_Reaches:
        case UFLevelStructure.Event.EventType.When_Life_Reaches:
        case UFLevelStructure.Event.EventType.When_Armor_Reaches:
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
        case UFLevelStructure.Event.EventType.Force_Monitor_Update:
        case UFLevelStructure.Event.EventType.Black_Out_Player:
        case UFLevelStructure.Event.EventType.Turn_Off_Physics:
        case UFLevelStructure.Event.EventType.Holster_Weapon:
        case UFLevelStructure.Event.EventType.Holster_Player_Weapon:
        case UFLevelStructure.Event.EventType.Clear_Endgame_If_Killed:
        case UFLevelStructure.Event.EventType.Win_PS2_Demo:
        case UFLevelStructure.Event.EventType.Enable_Navpoint:
        case UFLevelStructure.Event.EventType.Play_Vclip:
        case UFLevelStructure.Event.EventType.Endgame:
        */

        }
            

    }
}
