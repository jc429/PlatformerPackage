using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformerEntity : MonoBehaviour
{
	protected Rigidbody _rigidbody{
		get{ return GetComponent<Rigidbody>(); }
	}

	const float maxFallSpeed = 20;

	//distance from center to just under feet
	protected float groundCheckRayLength = 0.55f;
	//distance from center to one side of collision box (minus a small bit for skin thickness)
	protected float colHalfWidth = 0.425f;
	protected bool groundedThisFrame;
	protected bool groundedLastFrame;
	protected float timeSinceLastGrounded;
	public bool IsGrounded{
		get{ return groundedThisFrame; }
	}

    // Start is called before the first frame update
    protected virtual void Start()
    {
        
    }

    // Update is called once per frame
 	protected virtual void Update()
    {
        UpdateGroundedState();
    }

	protected virtual void FixedUpdate(){
		ApplyGravity();
		LimitFallSpeed();
	}

	/* makes platforming feel much better by increasing gravity when at peak of jump or moving downward */
	void ApplyGravity(){
		if (!IsGrounded && _rigidbody.velocity.y < 5f) {
			_rigidbody.AddForce(Physics.gravity * 2);
		}
	}

	void LimitFallSpeed(){
		Vector3 v = _rigidbody.velocity;
		v.y = Mathf.Max(v.y, -maxFallSpeed);
		_rigidbody.velocity = v;
	}



	protected void UpdateGroundedState(){
		groundedLastFrame = groundedThisFrame;
		groundedThisFrame = CheckGroundedState();
		if(groundedThisFrame){
			timeSinceLastGrounded = 0;
		}
		else{
			timeSinceLastGrounded += Time.deltaTime;
		}
	}

	
	/* checks if entity is currently touching the ground */
	public bool CheckGroundedState() {
		bool groundHit;
		LayerMask gMask = Layers.GetGroundMask(false);
		
		bool groundHitL = Physics.Raycast(transform.position + new Vector3(-colHalfWidth, 0), Vector3.down, groundCheckRayLength, gMask);
		bool groundHitM = Physics.Raycast(transform.position, Vector3.down, groundCheckRayLength, gMask);
		bool groundHitR = Physics.Raycast(transform.position + new Vector3(colHalfWidth, 0), Vector3.down, groundCheckRayLength, gMask);
		groundHit = (groundHitL || groundHitM || groundHitR);

		Debug.DrawRay(transform.position, Vector3.down * groundCheckRayLength, (groundHitM ? Color.green : Color.white));
		Debug.DrawRay(transform.position + new Vector3(-colHalfWidth, 0), Vector3.down * groundCheckRayLength, (groundHitL ? Color.green : Color.white));
		Debug.DrawRay(transform.position + new Vector3(colHalfWidth, 0), Vector3.down * groundCheckRayLength, (groundHitR ? Color.green : Color.white));
		return groundHit;
	}

	
}
