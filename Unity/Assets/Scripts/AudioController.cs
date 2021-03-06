﻿using UnityEngine;
using System.Collections;

public class AudioController : MonoBehaviour {

	public AudioSource musicHappy;
	public AudioSource musicSad;
	public AudioSource musicLost;
	public AudioSource srcCollectable;
	public AudioSource srcMother;
	public AudioSource srcChild;

	public AudioClip sndCollect;
	public AudioClip[] sndCollectCandy;
	public AudioClip[] sndChildSad;
	public AudioClip[] sndChildLeave;
	public AudioClip[] sndChildWandering;
	public AudioClip[] sndJoin;
	public AudioClip[] sndIntercom;

	private float musicState = 1.0f; // 1.0 = happy, 0.0 = sad, -1.0 = lost.
	private float desiredState = 1.0f;
	private float intercomLeft = 18.0f;

	private float[] smoothBuffer;

	// Use this for initialization
	void Start () {
		musicHappy.Play();
		musicSad.Play();
		musicLost.Play();

		musicHappy.volume = 1.0f;
		musicSad.volume = 0.0f;
		musicLost.volume = 0.0f;

		smoothBuffer = new float [] { 1.0f, 1.0f, 1.0f, 1.0f };
	}
	
	// Update is called once per frame
	void Update () {
		var volHappy = Mathf.Clamp01(musicState);
		var volSad = 1.0f - Mathf.Abs(musicState);
		var volLost = Mathf.Clamp01(-musicState);

		musicHappy.volume = Mathf.SmoothStep(0.0f, 1.0f, volHappy);
		musicSad.volume = Mathf.SmoothStep(0.0f, 1.0f, volSad);
		musicLost.volume = Mathf.SmoothStep(0.0f, 1.0f, volLost);

		smoothBuffer[0] = desiredState;
		for (int i = 1; i < smoothBuffer.Length; i++) {
			smoothBuffer[i] += (smoothBuffer[i - 1] - smoothBuffer[i]) * Time.deltaTime * 2.0f;
		}
		musicState = smoothBuffer[smoothBuffer.Length - 1];

		intercomLeft -= Time.deltaTime;
		if (intercomLeft < 0.0f) {
			intercomLeft = Random.Range(25.0f, 35.0f); // Average clip length is 11 seconds, so 20-30 sec intervals.
			srcCollectable.PlayOneShot(RandomClip(sndIntercom));
		}
	}

	public void SetHappy () {
		desiredState = 1.0f;
	}

	public void SetSad () {
		desiredState = 0.0f;
	}

	public void SetLost () {
		desiredState = -1.0f;
	}

	public void CollectAt (Vector3 at) {
		srcCollectable.transform.position = at;
		srcCollectable.PlayOneShot(sndCollect);
	}

	private static AudioClip RandomClip(AudioClip[] clips) {
		return clips[Random.Range(0, clips.Length)];
	}

	public void CollectCandy () {
		srcChild.Stop();
		srcChild.PlayOneShot(RandomClip(this.sndCollectCandy));
	}

	public void ChildSad () {
		srcChild.Stop();
		srcChild.PlayOneShot(RandomClip(this.sndChildSad));
	}

	public void ChildLeave () {
		srcChild.Stop();
		srcChild.PlayOneShot(RandomClip(this.sndChildLeave));
	}

	public void ChildWandering () {
		if (!srcChild.isPlaying) {
			srcChild.PlayOneShot(RandomClip(this.sndChildWandering));
		}
	}

	public void Join () {
		srcMother.Stop();
		srcMother.PlayOneShot(RandomClip(this.sndJoin));
	}
}
