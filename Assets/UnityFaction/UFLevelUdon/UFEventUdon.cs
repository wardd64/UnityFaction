
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class UFEventUdon : UdonSharpBehaviour
{

    //dynamic variables
    private bool positiveSignal;
    private float timer;

    //general variables
    public int type;
    public int typeClass;
    public float delay;

    //event data
    public bool bool1, bool2;
    public int int1, int2;
    public float float1, float2;
    public string string1, string2;
    public GameObject[] links;
    public int[] linkType;
    public Color color;
    public Object obj;

    //signal trigger
    public int signal, deactivate;

    private void Start()
    {
        if(typeClass == 1) //start
            Trigger(true, 0);
    }


    private void Update()
    {
        if(signal != 0) {
            Activate(signal > 0);
            signal = 0;
        }

        if(deactivate != 0) {
            if(typeClass == 5) //continuous effect
                timer = 0f;
            deactivate = 0;
        }

        if(typeClass == 3) //detector
        {
            if(Detect())
                Trigger(true, 0);

            timer = 0f;
            return;
        }

        if(timer > 0f)
            timer += Time.deltaTime;

        if(timer > delay)
        {
            if(typeClass == 2) //signal
            {
                switch(type)
                {

                case 49:
                Trigger(positiveSignal, 0);
                timer = 0f;
                break;

                case 5:
                Trigger(positiveSignal, 0);
                timer -= delay;
                break;

                case 17:
                Trigger(!positiveSignal, 0);
                timer = 0f;
                break;

                case 41:
                bool1 = !bool1;
                Trigger(bool1, 0);
                timer = 0f;
                break;
                }
            }
            else if(typeClass == 4) //effect
            {
                int ignoredType = DoEffect(positiveSignal);
                Trigger(positiveSignal, ignoredType);
                timer = 0f;
            }
            else if(typeClass == 5) //continuous effect
            {
                DoContinuousEffect();
            }
            else
            {
                Trigger(positiveSignal, 0);
                timer = 0f;
            }
        }
    }

    public void Activate(bool positive)
    {
        if(timer == 0f)
        {
            positiveSignal = positive;
            timer = Time.deltaTime;
        }
    }

    public void DoContinuousEffect()
    {
        switch(type)
        {

        case 4: //continuous damage
        //TODO use damage type (int2)
        float dps = int1;
        if(int1 <= 0)
            dps = float.PositiveInfinity;
        float damage = Time.deltaTime * dps;
        float hp = Networking.LocalPlayer.CombatGetCurrentHitpoints();
        hp -= damage;
        Networking.LocalPlayer.CombatSetCurrentHitpoints(hp);
        break;

        default:
        Debug.LogError("Event type " + type + " not implemented");
        break;
        }
    }

    /// <summary>
    /// Ignored type is 0 by default, to trigger all links.
    /// </summary>
    private void Trigger(bool positive, int ignoredType)
    {
        if(ignoredType == 1) //all
            return;

        for(int i = 0; i < links.Length; i++)
        {
            if(linkType[i] != ignoredType)
                TriggerUdon(links[i], "signal", positive);
        }
    }

    /// <summary>
    /// Checks if conditions for this detector event are met and returns true if so
    /// </summary>
    private bool Detect()
    {

        //preperatory variables
        //float countDownValue = UFLevel.GetPlayer<UFPlayerMovement>().GetCountDownValue();

        switch(type)
        {
        /*
        case UFLevelStructure.Event.EventType.When_Countdown_Over:
        return countDownValue - Time.deltaTime <= 0f && countDownValue > 0f;

        case UFLevelStructure.Event.EventType.When_Countdown_Reaches:
        return countDownValue - Time.deltaTime <= int1 && countDownValue > int1;
        */

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
    private int DoEffect(bool positive)
    {

        //preperatory variables
        /*
        Transform playerTr = UFLevel.GetPlayer<Transform>();
        UFPlayerLife playerLi = UFLevel.GetPlayer<UFPlayerLife>();
        UFPlayerMovement playerMo = UFLevel.GetPlayer<UFPlayerMovement>();
        */
        AudioSource sound = this.GetComponent<AudioSource>();

        //find effect type and do its effects. Return IDRefts that were used up.
        switch(type)
        {

        case 43: //teleport
        foreach(GameObject g in links)
        {
            g.transform.position = transform.position;
            g.transform.rotation = transform.rotation;
        }
        return 0;

        case 64: //teleport player
        Networking.LocalPlayer.TeleportTo(transform.position, transform.rotation);
        return 0;

        case 24: //music start
        if(sound.clip == null)
            Debug.LogWarning("Music event has no audio clip assigned: " + name);
        sound.Play();
        return 0;

        case 28: //play sound
        sound.rolloffMode = AudioRolloffMode.Linear;
        if(float1 >= 0f)
        {
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
        return 0;

        /*
        case 25: //music stop
        float fadeTime = float1;
        foreach(AudioSource source in FindObjectsOfType<AudioSource>())
        {
            if(source.name.Contains("Music"))
                source.Stop();
        }
        return 0;
        */

        /*
        case 26: //particle state
        foreach(UFParticleEmitter pem in GetLinksOfType<UFParticleEmitter>(IDRef.Type.ParticleEmitter))
            pem.Activate(positive);
        return 9; //particle emitter
        */

        case 73: //mover pause
        foreach(GameObject mov in GetLinksOfType(5)) //keyframe
            TriggerUdon(mov, "signal", false);
        return 5;

        case 90: //reverse mover
        bool setForwardIfMoving = int1 != 0;
        foreach(GameObject mov in GetLinksOfType(5)) //keyframe
            TriggerUdon(mov, "reverse", setForwardIfMoving);
        return 5;

        /*
        case 67: //modify rotator
        bool increase = string1.Equals("Increase");
        float factor = increase ? 1f + (float1 / 100f) : 1f - (float1 / 100f);
        foreach(UdonBehaviour mov in GetLinksOfType(5)) //keyframe
            mov.ChangeRotationSpeed(factor);
        return 5;
        */

        /*
        case 7: //explode
        float radius = float1;
        float damage = float2;
        bool geo = bool1;
        float dist = (playerTr.position - transform.position).magnitude;
        if(dist < radius)
            playerLi.TakeDamage(damage, UFPlayerLife.DamageType.Explosive, false);
        if(geo)
            Debug.LogWarning("Explosion " + name + " requested geo mod; this will not work as of now!");
        GameObject explosionPrefab = (GameObject)obj;
        if(explosionPrefab != null)
        {
            Vector3 explPos = transform.position;
            Quaternion explRot = transform.rotation;
            GameObject explosion = Instantiate(explosionPrefab, explPos, explRot, transform);
            explosion.transform.localScale = radius * 2f * Vector3.one;
        }
        else
            Debug.LogWarning("Explosion prefab for event " + name + " was not provided!");
        return 0;
        */

        /*
        case UFLevelStructure.Event.EventType.Skybox_State:
        UFLevel.playerInfo.SetSkyboxRotation(string1, float1);
        return 0;
        */

        case 3: //Bolt state
        foreach(GameObject bem in GetLinksOfType(11))
            TriggerUdon(bem, "signal", positive);
        return 11; //bolt emitter

        case 52: //push region state
        foreach(GameObject pr in GetLinksOfType(18)) //Force region
            pr.SetActive(positive);
        return 18; //push region

        case 30: //delete
        foreach(GameObject g in links)
            g.SetActive(false);
        return 1;

        case 45: //set gravity
        Networking.LocalPlayer.SetGravityStrength(float1);
        return 0;

        default:
        Debug.Log("Event type " + type + " not implemented");
        return 0;
        }
    }

    private GameObject[] GetLinksOfType(int type) {
        int count = 0;
        for(int i = 0; i < links.Length; i++)
        {
            if(linkType[i] == type || type == 1)
                count++;
        }
        int j = 0;
        GameObject[] toReturn = new GameObject[count];
        for(int i = 0; i < links.Length; i++)
        {
            if(linkType[i] == type || type == 1)
                toReturn[j++] = links[i];
        }
        return toReturn;
    }

    private void TriggerUdon(GameObject g, string signal, bool positive)
    {
        UdonBehaviour ub = (UdonBehaviour)g.GetComponent(typeof(UdonBehaviour));
        ub.SetProgramVariable(signal, positive ? 1 : -1);
    }

    /*
    private UFEvent[] GetEventsOfType(int type)
    {
        UFEvent[] allEvents = this.transform.parent.GetComponentsInChildren<UFEvent>();
        int count = 0;
        foreach(UFEvent e in allEvents)
        {
            if(e.type == type)
                count++;
        }
        int i = 0;
        UFEvent[] toReturn = new UFEvent[count];
        foreach(UFEvent e in allEvents)
        {
            if(e.type == type)
                toReturn[i++] = e;
        }

        return toReturn;
    }
    */
}
