using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformerMovement : PlatformerEntity {

	bool infiniteAirActions = true;

	/* adjustible physics properties */
	const float baseMoveSpeedGround = 4f;
	const float baseMoveSpeedAir = 3.2f;
	const float jumpSpeed = 9f;
	const float dashSpeed = 30f;

	const int maxAirJumps = 1;
	int numAirJumps;
	const int maxAirDashes = 1;
	int numAirDashes;
	
	bool isWallJumping;
	const float wallJumpLerpDuration = 1;
	float wallJumpLerpTime;
	
	bool isDashing;
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


	protected override void EnterGroundedState(){
		//reset jumps, dashes, etc here
		//apply landing effect?
		numAirJumps = maxAirJumps;
		numAirDashes = maxAirDashes;
	}

	/***  Halting Actions  ******************************************************************/
	
	public void HaltDownwardsMomentum(){
		_rigidbody.velocity = Vector3.zero;
		StartCoroutine(Suspend(0.13f));
	}

	IEnumerator Suspend(float time){
		_rigidbody.velocity = Vector3.zero;
		DisableGravity();
		yield return new WaitForSeconds(time);
		if(!IsGrounded){
			EnableGravity();
			_rigidbody.velocity = new Vector3(0,-1,0);
		}
		yield break;
	}

	/***  Jump  ****************************************************************************/

	public void AttemptJump(Vector2 dirInput){
		if(IsGrounded){
			PerformGroundedJump(jumpSpeed, dirInput.x);
		}
		else if(timeSinceLastGrounded < inputLeniency){
			//if we were slightly too late inputting jump, allow it
			PerformGroundedJump(jumpSpeed, dirInput.x);
		}
		else{
			//if we're slightly too early inputting jump, buffer it
			jumpBuffer = inputLeniency;
		}
	}

	protected void PerformGroundedJump(float speed, float horizInput) {
		jumpBuffer = 0;

		Vector3 vel = _rigidbody.velocity;
		vel.y = speed;
		if(horizInput != 0){
			float dir = Mathf.Sign(horizInput);
			vel.x = 0.2f * dir * speed;
		}
		_rigidbody.velocity = vel;
	}

	protected void PerformAirJump(float speed, float horizInput) {
		if(numAirJumps <= 0){
			return;
		}

		if(!infiniteAirActions){
			numAirJumps--;
		}

		ClearWallJumpVars();

		jumpBuffer = 0;

		Vector3 vel = _rigidbody.velocity;
		vel.y = speed;
		if(horizInput != 0){
			float dir = Mathf.Sign(horizInput);
			vel.x = 0.2f * dir * speed;
		}
		_rigidbody.velocity = vel;
	}

	void CheckJumpBuffer(){
		if(jumpBuffer > 0){
			if(IsGrounded){
				PerformGroundedJump(jumpSpeed, VirtualController.GetDPadAxisHorizontal());
			}
			jumpBuffer -= Time.deltaTime;
			if(jumpBuffer < 0){
				jumpBuffer = 0;
				if(CheckIfTouchingWall(-1)){
					PerformWallJump(jumpSpeed, 1);
				}
				else if(CheckIfTouchingWall(1)){
					PerformWallJump(jumpSpeed, -1);
				}
				else if(numAirJumps > 0){
					PerformAirJump(jumpSpeed, VirtualController.GetDPadAxisHorizontal());
				}
			}
		}
	}

	/***  Wall Jump  ************************************************************************/

	protected void PerformWallJump(float speed, int launchDir) {
		if(launchDir == 0){
			return;
		}

		jumpBuffer = 0;

		Vector3 vel = _rigidbody.velocity;
		vel.y = jumpSpeed;
		vel.x = jumpSpeed * launchDir * 1;
		_rigidbody.velocity = vel;

		isWallJumping = true;
		wallJumpLerpTime = 0;
	}

	void ClearWallJumpVars(){
		isWallJumping = false;
		wallJumpLerpTime = 0;
	}

	/***  Dash  ****************************************************************************/

	public void AttemptDash(Vector2 inputs){
		PerformAirDash(dashSpeed,inputs);
	}

	public bool PerformAirDash(float speed, Vector2 direction){
		if(direction == Vector2.zero){
			return false;
		}
		if(numAirDashes <= 0){
			return false;
		}

		if(!infiniteAirActions){
			numAirDashes--;
		}
		isDashing = true;
		LockInputs(true);

		Vector3 normalizedDir = direction.normalized;
		_rigidbody.velocity = normalizedDir * speed;
		DisableGravity();

		StartCoroutine(DashSlow());

		return true;
	}

	IEnumerator DashSlow(){

		const float maxDrag = 25;
		const float minDrag = 5;
		const float dashDuration = 0.25f;
		float dashTime = 0;

		while (dashTime < dashDuration)
		{
			dashTime += Time.deltaTime;
			float drag = Mathf.Lerp(minDrag,maxDrag, dashTime/dashDuration);
			//drag = 10;
			_rigidbody.drag = drag;
			yield return null;
		}


        //yield return new WaitForSeconds(1.3f);

		DashEnd();

		yield break;
	}

	void DashEnd(){
		//EnableGravity();
		LockInputs(false);
		_rigidbody.drag = 1;
		isDashing = false;
		EnableGravity();
		//HaltDownwardsMomentum();
	}

	/***  Basic Movement  ******************************************************************/

	public bool AttemptMovement(Vector3 moveInputs){
		if(isDashing){
			return false;
		}

		float moveSpeed = IsGrounded ? baseMoveSpeedGround : baseMoveSpeedAir;

		Vector3 adjustedMovement = LimitHorizontalMovementDistance(moveInputs, moveSpeed, isCrouching);
		if(adjustedMovement == Vector3.zero){
			return false;
		}

		if(isWallJumping){
			wallJumpLerpTime += Time.deltaTime;
			adjustedMovement.x = Mathf.Lerp(0, adjustedMovement.x, wallJumpLerpTime / wallJumpLerpDuration); 
			if(wallJumpLerpTime >= wallJumpLerpDuration){
				isWallJumping = false;
				wallJumpLerpTime = 0;
			}
		}

		Vector3 newPosition = transform.position;
		newPosition.x += adjustedMovement.x;	
		newPosition.y += adjustedMovement.y;	
		transform.position = newPosition;

		return true;
	}

	/* Limits movement so we don't push the entity too deep into a wall */
	public Vector3 LimitHorizontalMovementDistance(Vector3 inputDir, float moveSpd, bool reducedHitbox = false) {
		if (inputDir.Equals(Vector3.zero)) return Vector3.zero;
		
		const float slopeWallThreshold = 0.1f;
		const int numChecks = 5;
		const int numUpper = 2;	//checks positioned above the crouching hitbox

		Vector3 origin = transform.position;
		RaycastHit r;
		bool hit;
		float[] distlist = new float[numChecks];
		//offsets assume a 1x1 tile hitbox with the origin in the center of the lower tile
		//offsets MUST be in order from bottom to top
		float[] rayOffsets = new float[numChecks]{-0.45f, -0.15f, 0.0f, 0.15f, 0.45f};
		
		float spd =  0.5f + (moveSpd * Time.deltaTime);
		Vector3 moveDir = new Vector3(inputDir.x, 0);
		int gMask = Layers.GetGroundMask(true);

		//used to allow natural movement when colliding with slopes 
		bool isAscendingSlope = false;
		Vector3 aSlopeNormal = Vector3.zero;
		bool isDescendingSlope = false;
		Vector3 dSlopeNormal = Vector3.zero;

		for(int i = 0; i < numChecks; i++){
			//if crouching, skip upper checks (wont necessarily be correct for every game)
			if((i >= (numChecks - numUpper)) && reducedHitbox){
				continue;
			}
			Vector3 rayStart = origin + new Vector3(0, rayOffsets[i], 0);
			hit = Physics.Raycast(rayStart, moveDir, out r, spd, gMask);
			Debug.DrawRay(rayStart, spd * moveDir, (hit ? Color.red : Color.white));

			//if hitting a slope, handle it (bottom raycast only)
			if(i == 0 && Mathf.Abs(r.normal.y) > slopeWallThreshold){
				isAscendingSlope = true;
				aSlopeNormal = r.normal;
				continue;
			}
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
			moveDir.x *= (shortest - colHalfWidth);
		}
		else {
			moveDir.x *= moveSpd * Time.deltaTime;
		}

		if(isAscendingSlope){
			moveDir = AscendSlope(moveDir,aSlopeNormal);
		}

		Vector3 descendCheckStart = transform.position + new Vector3(-groundingHalfWidth * Mathf.Sign(moveDir.x), 0);
		bool slopeHit = Physics.Raycast(descendCheckStart, Vector3.down, out r, groundCheckRayLength, gMask);
		if(slopeHit){
			if(Mathf.Abs(r.normal.y) > slopeWallThreshold && (r.normal.x * moveDir.x > 0)){
				isDescendingSlope = true;
				dSlopeNormal = r.normal;

				moveDir = DescendSlope(moveDir,dSlopeNormal);
			}
		}


		//Debug.Log(moveDir);

		return moveDir;
	}

	public Vector3 AscendSlope(Vector3 movement, Vector3 slopeNormal){
		float angle = Vector3.Angle(slopeNormal, transform.up);
		float tan = Mathf.Tan(Mathf.Deg2Rad * angle);
		movement.y = tan * Mathf.Abs(movement.x);
		return movement;
	}

	public Vector3 DescendSlope(Vector3 movement, Vector3 slopeNormal){
		float angle = Vector3.Angle(slopeNormal, transform.up);
		float tan = Mathf.Tan(Mathf.Deg2Rad * angle);
		movement.y = tan * -Mathf.Abs(movement.x);
		return movement;
	}

	public bool CanUncrouch(){
		Vector3 origin = transform.position;
		Vector3 dir = Vector3.up;
		float length = 0.9f;
		int gMask = Layers.GetGroundMask(true);
		bool hit = false;
		hit |= Physics.Raycast(origin, dir, length, gMask);
		hit |= Physics.Raycast(origin + new Vector3(groundingHalfWidth, 0), dir, length, gMask);
		hit |= Physics.Raycast(origin + new Vector3(groundingHalfWidth, 0), dir, length, gMask);
		return !hit;
	}
}
