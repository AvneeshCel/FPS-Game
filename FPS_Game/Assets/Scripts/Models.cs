using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Models
{
    #region Player

    public enum PlayerStance
    {
        Stand,
        Crouch,
        Prone
    }

    [Serializable]
    public class PlayerSettingsModel
    {
        [Header("View Settings")]
        public float ViewXSensitivity;
        public float ViewYSensitivity;

        public bool ViewXInverted;
        public bool ViewYInverted;

        [Header("Movement Settings")]
        public bool SprintingHold;
        public float MovementSmoothing;

        [Header("Movement - Running")]
        public float RunningForwardSpeed;
        public float RunningStrafeSpeed;
        
        [Header("Movement - Walking")]
        public float WalkingForwardSpeed;
        public float WalkingBackwardsSpeed;
        public float WalkingStrafeSpeed;

        [Header("Jumping")]
        public float JumpHeight;
        public float JumpFalloff;
        public float FallingSmoothing;

        [Header("Speed Effectors")]
        public float SpeedEffector = 1;
        public float CrouchSpeedEffector;
        public float ProneSpeedEffector;
        public float FallingSpeedEffector;

        [Header("Is Grounded / Falling")]
        public float isGroundedRadius;
        public float isFallingSpeed;
    }

    [Serializable]
    public class CharacterStance
    {
        public float CameraHeight;
        public CapsuleCollider StanceCollider;
    }

    #endregion

    #region Weapons

    public enum WeaponFireType
    {
        SemiAuto,
        FullyAuto
    }


    [Serializable]
    public class WeaponSettings
    {
        [Header("Weapon Sway")]
        public float SwayAmount;
        public float SwaySmoothing;
        public bool SwayXInverted;
        public bool SwayYInverted;
        public float SwayResetSmoothing;
        public float SwayClampX;
        public float SwayClampY;


        [Header("Weapon Sway")]
        public float MovementSwayX;
        public float MovementSwayY;
        public bool MovementSwayXInverted;
        public bool MovementSwayYInverted;
        public float MovementSwaySmoothing;

    }
    #endregion
}
