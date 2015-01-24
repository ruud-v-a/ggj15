﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ChildBehaviour : MonoBehaviour {

	public int initialComfort = 10;
	public int cryAt = 5;
	public int runAwayAt = 0;
	public float moveTime = 0.4f;
	public float turnTime = 0.15f;

	private bool isFollowing = true;
	public bool isCrying { get; private set; }
	private GameObject player;
	private int comfort;
	private Queue<Movement> movements = new Queue<Movement>();
	private float movementT;

	private Vector3 waistPosition { get { return this.transform.position + new Vector3(0f, 0.5f, 0f); } }
	
	void Start () {
		this.player = GameObject.FindGameObjectWithTag("Player");
		comfort = initialComfort;
		isCrying = false;
	}

	void Update () {
		if (this.movements.Count > 0) {
			HandleMove();
		}
	}

	void LateUpdate () {
		if (this.movements.Count == 0 && isFollowing) {
			// Track the parent (player).
			this.transform.position = player.transform.position;
			this.transform.rotation = player.transform.rotation;
		}
	}

	private Movement MakeMovement (Vector3 offset) {
		return new Movement {
			kind = MovementKind.Move,
			duration = moveTime,
			fromPos = this.transform.position,
			toPos = this.transform.position + offset
		};
	}
	
	private Movement MakeRotation (float angle) {
		return new Movement {
			kind = MovementKind.Rotate,
			duration = turnTime,
			fromRot = this.transform.rotation,
			toRot = Quaternion.AngleAxis(angle, Vector3.up) * this.transform.rotation
		};
	}

	private void HandleMove () {
		var movement = this.movements.Peek();
		this.movementT += Time.deltaTime ;
		var t = this.movementT / movement.duration;
		
		// This one is done now.
		if (t > 1.0f) {
			this.movementT = 0.0f;
			t = 1.0f;
			this.movements.Dequeue();
			MoveCompleted(movement);
		}
		
		// Ease / smooph step.
		t = Mathf.SmoothStep(0.0f, 1.0f, t);
		
		if (movement.kind == MovementKind.Move) {
			// Move to the destination, but if we should follow the player whilst still in a movement,
			// then move towards the player instead.
			var to = isFollowing ? player.transform.position : movement.toPos;
			this.transform.position = movement.fromPos * (1.0f - t) + to * t;
		}
		
		if (movement.kind == MovementKind.Rotate) {
			this.transform.rotation = Quaternion.Lerp(movement.fromRot, movement.toRot, t);
		}
	}

	private void MoveCompleted (Movement movement) {
		if (this.movements.Count == 0 && !isFollowing) {
			ComputeNextMove(movement);
		}
	}

	private void ComputeNextMove (Movement previous) {
		var left = transform.TransformDirection(Vector3.left);
		var right = transform.TransformDirection(Vector3.right);
		var forward = transform.forward;
		
		var canLeft = !Physics.Raycast(waistPosition, left, 1.4f);
		var canRight = !Physics.Raycast(waistPosition, right, 1.4f);
		var canForward = !Physics.Raycast(waistPosition, forward, 1.4f);
		
		Debug.DrawRay(waistPosition, left * 1.4f, Color.red, 0f);
		Debug.DrawRay(waistPosition, right * 1.4f, Color.blue, 0f);
		Debug.DrawRay(waistPosition, forward * 1.4f, Color.cyan, 0f);

		// If we rotated previously, try to move forward now.
		if (previous.kind == MovementKind.Move && canForward) {
			this.movements.Enqueue(MakeMovement(forward));
			return;
		}

		// Otherwise prefer to go to the spot that is furthest away from the player.
		var options = new List<Vector3>();
		if (canLeft) options.Add (left);
		if (canRight) options.Add (right);
		if (canForward) options.Add (forward);

		// If we are stuck, turn around.
		if (options.Count == 0) {
			this.movements.Enqueue(MakeRotation(180.0f));
		} else {
			var go = options.OrderByDescending(x => (this.player.transform.position - this.transform.position - x).sqrMagnitude).First();

			if (go == left) {
				this.movements.Enqueue(MakeRotation(-90.0f));
			} else if (go == right) {
				this.movements.Enqueue(MakeRotation(90.0f));
			}
			this.movements.Enqueue(MakeMovement(go));
		}

		// TODO: we could make the child stop / reverse / stop at candy.
	}

	void OnTriggerEnter(Collider other) {
		// If the parent does find the child again after it ran away, the child will follow again,
		// but it will be crying.
		if (!isFollowing && other.gameObject.tag == "Player") {
			isFollowing = true;
			isCrying = true;
			comfort = cryAt; // TODO: Could add some randomness?
		}
	}

	// When the parent sets a step, it should derement the comfort of the child.
	public void DecrementComfort () {
		// Decrementing is only effective when the child is obedient and following the parent.
		if (isFollowing) {
			comfort--;
			Debug.Log(string.Format("Decremented child comport, comport is {0}", this.comfort));

			if (comfort <= runAwayAt) {
				isFollowing = false;
				isCrying = false;
				Debug.Log("Child ran away!");
				// TODO: effects and the like, feedback

				// Turn around, run away from the parent!
				this.movements.Enqueue(MakeRotation(180.0f));
				return;
			}

			if (comfort <= cryAt) {
				isCrying = true;
				Debug.Log("Child began to cry!");
				// TODO: effects and the like, feeback
				return;
			}
		}
	}
}
