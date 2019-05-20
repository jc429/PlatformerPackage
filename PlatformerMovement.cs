using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformerMovement : PlatformerEntity {

	/* adjustible physics properties */
	const float baseMoveSpeedGround = 4f;
	const float baseMoveSpeedAir = 3.2f;
	const float jumpSpeed = 9f;
	const int maxAirJumps = 1;
	int numAirJumps;
	
	bool isCrouching;
	
	//timeframe to buffer a jump input (after walking off a cliff)
	const float inputLeniency = 0.05f;

	float jumpBuffer;
	

	public bool IsMoving{
		get{
			return _rigidbody.velocity.x != 0;
		}
	}

	private bool inputLock;
	public bool InputsLocked{
		get{ return inputLock; }
	}
	/* Locks the input */
	public void LockInputs(bool inlock){
		inputLock = inlock;
	}

	
	protected override void Update(){
		base.Update();
		CheckJumpBuffer();
	}



	public void AttemptJump(){
		if(IsGrounded){
			PerformJump();
		}
		else if(timeSinceLastGrounded < inputLeniency){
			//if we were slightly too late inputting jump, allow it
			PerformJump();
		}
		else{
			//if we're slightly too early inputting jump, buffer it
			jumpBuffer = inputLeniency;
		}
	}

	protected void PerformJump() {
		jumpBuffer = 0;

		Vector3 vel = _rigidbody.velocity;
		vel.y = jumpSpeed;
		_rigidbody.velocity = vel;
	}

	void CheckJumpBuffer(){
		if(jumpBuffer > 0){
			if(IsGrounded){
				PerformJump();
			}
			jumpBuffer -= Time.deltaTime;
			if(jumpBuffer < 0){
				jumpBuffer = 0;
			}
		}
	}

	protected bool AttemptMovement(Vector3 moveInputs){
		float moveSpeed = IsGrounded ? baseMoveSpeedGround : baseMoveSpeedAir;

		Vector3 adjustedMovement = AdjustHorizontalMovementDistance(moveInputs, moveSpeed, isCrouching);
		if(adjustedMovement == Vector3.zero){
			return false;
		}

		Vector3 newPosition = transform.position;
		newPosition.x += adjustedMovement.x;	//x axis only for now?
		transform.position = newPosition;

		return true;
	}

	/* Limits movement so we don't push the entity too deep into a wall */
	public Vector3 AdjustHorizontalMovementDistance(Vector3 moveDir, float moveSpd, bool reducedHitbox = false) {
		if (moveDir.Equals(Vector3.zero)) return Vector3.zero;
		
		Vector3 origin = transform.position;
		RaycastHit r;
		bool hit;
		float[] distlist = new float[5];
		//offsets assume a 1x2 tile hitbox with the origin in the center of the lower tile
		float[] rayOffsets = new float[5]{0.8f, 0.5f, 0.2f, -0.15f, -0.45f};
		
		float spd =  0.5f + (moveSpd * Time.deltaTime);
		Vector3 dir = new Vector3(moveDir.x, 0);
		int gMask = Layers.GetGroundMask(true);

		for(int i = 0; i < 5; i++){
			//if crouching, skip upper checks (wont necessarily be correct for every game)
			if(i < 2 && reducedHitbox){
				continue;
			}
			Vector3 rayStart = origin + new Vector3(0, rayOffsets[i], 0);
			hit = Physics.Raycast(rayStart, dir, out r, spd, gMask);
			Debug.DrawRay(rayStart, spd * dir, (hit ? Color.red : Color.white));
			distlist[i] = r.distance;

		}

		float shortest = float.MaxValue;
		foreach (float f in distlist) {
			if (f == 0) {
				continue;
			}
			if (f < shortest) {
				shortest = f;
			}
		}

		if (shortest > 0 && shortest < float.MaxValue) {
			moveDir.x *= (shortest - 0.5f);
		}
		else {
			moveDir.x *= moveSpd * Time.deltaTime;
		}

		return moveDir;
	}

	public bool CanUncrouch(){
		Vector3 origin = transform.position;
		Vector3 dir = Vector3.up;
		float length = 0.9f;
		int gMask = Layers.GetGroundMask(true);
		bool hit = false;
		hit |= Physics.Raycast(origin, dir, length, gMask);
		hit |= Physics.Raycast(origin + new Vector3(colHalfWidth, 0), dir, length, gMask);
		hit |= Physics.Raycast(origin + new Vector3(colHalfWidth, 0), dir, length, gMask);
		return !hit;
	}
}
