﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformerPlayerController : MonoBehaviour
{
	protected PlatformerMovement _movement;

    // Start is called before the first frame update
    void Awake()
    {
        _movement = GetComponent<PlatformerMovement>();
		if(_movement == null){
			Debug.Log("Movement Script not found!");
		}
    }

    // Update is called once per frame
    void Update()
    {
		Move();
	}

	void Move(){
		Vector3 moveInputs = Vector3.zero;
		moveInputs.x = VirtualController.GetDPadAxisHorizontal();
		moveInputs.y = VirtualController.GetDPadAxisVertical();

		_movement.AttemptMovement(moveInputs);

        if (VirtualController.JumpButtonPressed()) {
			_movement.AttemptJump(VirtualController.GetDPadAxes());
		}

		if(Input.GetKeyDown(KeyCode.LeftShift)){
			_movement.AttemptDash(VirtualController.GetDPadAxes());
		}
    }
}
