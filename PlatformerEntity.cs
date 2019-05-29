using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformerEntity : MonoBehaviour
{
	protected Rigidbody _rigidbody{
		get{ return GetComponent<Rigidbody>(); }
	}

	const float maxFallSpeed = 20;

	[Space]
	//distance from center to just under feet
	protected float groundCheckRayLength = 0.55f;
	//distance from center to one side of collision box (exact)
	protected float colHalfWidth = 0.475f;
	//distance from center to one side of collision box (with small inset)	
	protected float groundingHalfWidth = 0.475f;
	//distance from center of collision box to floor
	protected float colToFloor = 0.475f;
	[Space]
	protected bool groundedThisFrame;
	protected bool groundedLastFrame;
	protected float distanceToGround;
	protected float timeSinceLastGrounded;
	public bool IsGrounded{
		get{ return groundedThisFrame; }
	}

	[Space]
	protected float gravMultiLarge = 2.5f;
	protected float gravMultiSmall = 2;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        
    }

    // Update is called once per frame
 	protected virtual void Update()
    {
        UpdateGroundedState();
		ApplyGravity();
    }

	protected virtual void FixedUpdate(){
		LimitFallSpeed();
		DecayHorizontalMotion();
	}

	/* makes platforming feel much better by increasing gravity when at peak of jump or moving downward */
	void ApplyGravity(){
		if (!IsGrounded){
			//if going down use big multiplier
			if(_rigidbody.velocity.y < 0) {
				//_rigidbody.AddForce(Physics.gravity * gravMultiLarge);
				Vector3 accel = Vector2.up * Physics.gravity.y * gravMultiLarge * Time.deltaTime;
				_rigidbody.velocity += accel;
			}
			/*else if(!VirtualController.JumpButtonPressed(true)){
				Vector3 accel = Vector2.up * Physics.gravity.y * gravMultiSmall * Time.deltaTime;
				_rigidbody.velocity += accel;
			}*/
		}
	}

	/* prevents entity from moving so fast they fly through colliders */
	void LimitFallSpeed(){
		Vector3 v = _rigidbody.velocity;
		v.y = Mathf.Max(v.y, -maxFallSpeed);
		_rigidbody.velocity = v;
	}

	/* reduces horizontal momentum over time */
	void DecayHorizontalMotion(){
		const float velDecayRate = 0.95f;
		Vector3 v = _rigidbody.velocity;
		if(v.x == 0){
			return;
		}
		v.x *= velDecayRate;
		_rigidbody.velocity = v;
	}

	/* checks if grounded and updates entity accordingly */
	protected void UpdateGroundedState(){
		groundedLastFrame = groundedThisFrame;
		groundedThisFrame = CheckGroundedState();
		if(groundedThisFrame && !groundedLastFrame){
			//only do all this if we're going down
			if(_rigidbody.velocity.y <= 0.5f){
				_rigidbody.useGravity = false;
				_rigidbody.velocity = Vector3.zero;
				Vector3 pos = transform.position;
				pos.y -= distanceToGround;
				transform.position = pos;
				timeSinceLastGrounded = 0;
			}
			EnterGroundedState();
		}
		if(!groundedThisFrame && groundedLastFrame){
			_rigidbody.useGravity = true;
			ExitGroundedState();
		}
		if(!groundedThisFrame){
			timeSinceLastGrounded += Time.deltaTime;
		}
	}

	protected virtual void EnterGroundedState(){ }

	protected virtual void ExitGroundedState(){ }

	
	/* checks if entity is currently touching the ground */
	public bool CheckGroundedState() {
		bool groundHit;
		LayerMask gMask = Layers.GetGroundMask(false);
		
		float distToGround = float.MaxValue;
		RaycastHit r;
		bool groundHitL = Physics.Raycast(transform.position + new Vector3(-groundingHalfWidth, 0), Vector3.down, out r, groundCheckRayLength, gMask);
		if(groundHitL){
			distToGround = Mathf.Min(r.distance - colToFloor, distToGround);
		}
		bool groundHitM = Physics.Raycast(transform.position, Vector3.down, out r, groundCheckRayLength, gMask);
		if(groundHitM){
			distToGround = Mathf.Min(r.distance - colToFloor, distToGround);
		}
		bool groundHitR = Physics.Raycast(transform.position + new Vector3(groundingHalfWidth, 0), Vector3.down, out r, groundCheckRayLength, gMask);
		if(groundHitR){
			distToGround = Mathf.Min(r.distance - colToFloor, distToGround);
		}
		bool groundHitMR = Physics.Raycast(transform.position + new Vector3(0.5f*groundingHalfWidth, 0), Vector3.down, out r, groundCheckRayLength, gMask);
		if(groundHitMR){
			distToGround = Mathf.Min(r.distance - colToFloor, distToGround);
		}
		bool groundHitML = Physics.Raycast(transform.position + new Vector3(-0.5f*groundingHalfWidth, 0), Vector3.down, out r, groundCheckRayLength, gMask);
		if(groundHitML){
			distToGround = Mathf.Min(r.distance - colToFloor, distToGround);
		}

		groundHit = (groundHitL || groundHitM || groundHitR || groundHitML || groundHitMR);
		
		distanceToGround = (groundHit) ? distToGround : 0;
		

		Debug.DrawRay(transform.position + new Vector3(-groundingHalfWidth, 0), Vector3.down * groundCheckRayLength, (groundHitL ? Color.green : Color.white));
		Debug.DrawRay(transform.position, Vector3.down * groundCheckRayLength, (groundHitM ? Color.green : Color.white));
		Debug.DrawRay(transform.position + new Vector3(groundingHalfWidth, 0), Vector3.down * groundCheckRayLength, (groundHitR ? Color.green : Color.white));
		return groundHit;
	}

	
}
