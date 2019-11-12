﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimController : MonoBehaviour
{
    public Animator PlayerAnimator;
    private AnimatorStateInfo animatorStateInfo;
    private SpriteRenderer spriteRenderer;
    private PlayerController playerController;
    private Rigidbody2D playerRigidbody;

    // player status

    private void Awake() {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        playerController = gameObject.GetComponent<PlayerController>();
        playerRigidbody = gameObject.GetComponent<Rigidbody2D>();
    }
    private void Update() {

        animatorStateInfo = PlayerAnimator.GetCurrentAnimatorStateInfo(0);

        this.CheckFaceDir();
        if (this.PlayOnHookAnim()) return;
        if (this.PlayJumpAnim()) return;
        this.PlayMoveAnim();
    }

    private void CheckFaceDir() {
        if (!playerController.FaceRight) spriteRenderer.flipX = true;
        else spriteRenderer.flipX = false;
    }
    private void PlayMoveAnim() {
        if (playerController.MoveDir.x != 0) {
            PlayerAnimator.SetBool("IsMoving", true);
        }
        else {
            PlayerAnimator.SetBool("IsMoving", false);
        }
    }
    private bool PlayOnHookAnim() {
        if (playerController.onHook) {
            PlayerAnimator.SetBool("OnHook", true);
            if (!animatorStateInfo.IsName("OnHook")) {
                PlayerAnimator.Play("OnHook");
            }
            return true;
        }
        return false;
    }
    private bool PlayJumpAnim() {
        if (!playerController.OnGround) {
            PlayerAnimator.SetBool("OnGround", false);
            if (!animatorStateInfo.IsName("Jump")) {
                PlayerAnimator.Play("Jump");
            }
            return true;
        }
        PlayerAnimator.SetBool("OnGround", true);
        return false;
    }
}