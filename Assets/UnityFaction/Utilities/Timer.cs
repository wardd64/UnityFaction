using UnityEngine;
using System;

[Serializable]
public class Timer{

	float startTime;
	float time;

    /// <summary>
    /// Constructs a timer object that can be handily made to tick towards 0.
    /// </summary>
    /// <param name="time">Time upon reset</param>
    /// <param name="initialTime">Time upon construction</param>
	public Timer(float time, float initialTime) {
		this.startTime = time;
		this.time = initialTime;
	}

    /// <summary>
    /// Constructs a timer object that can be handily made to tick towards 0.
    /// </summary>
    /// <param name="time">Time upon reset and construction.</param>
	public Timer(float time) {
		this.startTime = time;
		this.time = this.startTime;
	}

	public void Reset() {
		this.time = this.startTime;
	}

	public void Set(float time) {
		this.time = time;
	}

	public float MaxTime() {
		return this.startTime;
	}

	/// <summary>
	/// Number of seconds the timer is away from completion
	/// </summary>
	public float GetTime() {
		return time;
	}

	/// <summary>
	/// Fractional value (between 0 and 1) of this timer's completion.
	/// Is 0 when timer has completed, 1 when it is just starting.
	/// </summary>
	public float GetTimeFrac() {
		return time/startTime;
	}

	/// <summary>
	/// Advances time of this timer by given deltaTime. 
	/// Returns true if time is up.
	/// </summary>
	public bool Tick(float deltaTime) {
		time -= deltaTime;
		if(time <= 0f) {
			time = 0f;
			return true;
		}
		return false;
	}

	/// <summary>
	/// Advances time of this timer; should be called once in Update();
    /// Returns true if time is up.
	/// </summary>
	public bool Tick() {
		return this.Tick(Time.deltaTime);
	}

	/// <summary>
	/// Advances time of this timer by given deltaTime. 
	/// Returns true if timer runs out in this tick.
	/// </summary>
	public bool TickTrigger(float deltaTime) {
		if(time <= 0f)
			return false;

		time -= deltaTime;
		if(time <= 0f) {
			time = 0f;
			return true;
		}
		return false;
	}

	/// <summary>
	/// advances time of the timer, looping around the end, back to the start
	/// </summary>
	public void LoopTick(float deltaTime) {
		time -= deltaTime;
		time = Mathf.Repeat(time, startTime);
	}

	/// <summary>
	/// advances time of the timer, looping around the end, back to the start
	/// </summary>
	public void LoopTick() {
		this.LoopTick(Time.deltaTime);
	}

	/// <summary>
	/// Advances time of this timer; should only be called in Update(). 
	/// Returns true if timer runs out in this tick.
	/// </summary>
	public bool TickTrigger() {
		return this.TickTrigger(Time.deltaTime);
	}

	/// <summary>
	/// Advances time of this timer, returning actual delta time
	/// </summary>
	public float PreciseTick() {
		float toReturn = time;
		time -= Time.deltaTime;
		if(time <= 0f) {
			time = 0f;
			return toReturn;
		}
		return Time.deltaTime;
	}

	public bool done {
		get {
			return time <= 0f;
		}
	}

	public void SetDone() {
		this.time = 0f;
	}

	/// <summary>
	/// Return smallest time this timer is away from start or finish, 
	/// useful for managing symetrical transition in and out.
	/// </summary>
	public float GetBorderTime() {
		return Mathf.Min(time, startTime - time);
	}

	/// <summary>
	/// returns true if remaining time is lower than given fraction of the initial time.
	/// </summary>
	public bool DoneFrac(float frac) {
		return time <= frac * startTime;
	}

	/// <summary>
	/// returns true if remaining time is lower than given time
	/// </summary>
	public bool Done(float time) {
		return this.time <= time;
	}

	/// <summary>
	/// Creates fresh copy of this timer. 
	/// Remember to orient your objects kids!
	/// </summary>
	public Timer Clone() {
		return new Timer(this.startTime, this.time);
	}

	/// <summary>
	/// Creates linear interpolotation of this timer and the given timer.
	/// Start time of the timers is assumed equal.
	/// </summary>
	public Timer Lerp(Timer right, float factor) {
		float time = Mathf.Lerp(this.time, right.time, factor);
		return new Timer(this.startTime, time);
	}
}
