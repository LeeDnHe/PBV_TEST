﻿using Exoa.Common;
using Exoa.Designer;
using Exoa.Events;
using Exoa.Touch;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Exoa.Cameras
{
    public class CameraOrthoBase : CameraBase, ITouchOrthoCamera
    {
        [Header("DISTANCE")]
        public Vector2 sizeMinMax = new Vector2(1, 12);
        protected float finalSize = 5.0f;

        protected const float distanceToSize = .4f;

        protected float initSize = 6f;
        protected float initDistance = 10f;

        [Header("Z DISTANCE")]
        public float fixedDistance = 30f;

        /// <summary>
        /// returns the camera orthographic size
        /// </summary>
        public float FinalSize
        {
            get
            {
                return finalSize;
            }
        }

        /// <summary>
        /// Init some camera parameters
        /// </summary>
        override protected void Init()
        {
            base.Init();
            finalSize = initSize;
        }


        void Update()
        {
            if (disableMoves)
                return;

            List<TouchFinger> twoFingers = CameraInputs.TwoFingerFilter.UpdateAndGetFingers();
            List<TouchFinger> oneFinger = CameraInputs.OneFingerFilter.UpdateAndGetFingers();
            float zoomRatio = 1;
            bool anyInteraction = false;
            Vector2 screenCenter = cam.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0));

            worldPointCameraCenter = ClampInCameraBoundaries(HeightScreenDepth.Convert(screenCenter));
            worldPointFingersDelta = Vector3.zero;
            worldPointFingersCenter = ClampInCameraBoundaries(HeightScreenDepth.Convert(CameraInputs.screenPointAnyFingerCountCenter));
            twistRot = Quaternion.identity;
            //Log("Start FinalOffset:" + finalOffset);
            //Log("Start FinalPosition:" + finalPosition);
            //Log("Start FinalRotation:" + FinalRotation);
            //Log("Start FinalDistance:" + FinalDistance);

            if (IsInputMatching(InputMapFingerDrag.RotateAround))
            {
                RotateFromVector(CameraInputs.GetAnyPixelScaledDelta());
                finalRotation = GetRotationFromPitchYaw();
                finalPosition = CalculatePosition(finalOffset, finalRotation, finalDistance);
                anyInteraction = true;
            }

            if (IsInputMatching(InputMapFingerDrag.RotateHead))
            {
                RotateFromVector(CameraInputs.GetAnyPixelScaledDelta());
                finalRotation = GetRotationFromPitchYaw();
                anyInteraction = true;
            }

            if (IsInputMatching(InputMapFingerPinch.ZoomAndRotate) || IsInputMatching(InputMapFingerPinch.ZoomOnly))
            {
                zoomRatio = CameraInputs.pinchRatio;
                anyInteraction = true;
            }

            if (IsInputMatching(InputMapScrollWheel.ZoomInCenter))
            {
                zoomRatio = CameraInputs.GetScroll();
                worldPointFingersCenter = ClampInCameraBoundaries(HeightScreenDepth.Convert(screenCenter));
                anyInteraction = true;
            }

            if (IsInputMatching(InputMapScrollWheel.ZoomUnderMouse))
            {
                zoomRatio = CameraInputs.GetScroll();
                worldPointFingersCenter = ClampInCameraBoundaries(HeightScreenDepth.Convert(BaseTouchInput.GetMousePosition()));
                anyInteraction = true;

            }

            finalSize = Mathf.Clamp(finalSize * zoomRatio, sizeMinMax.x, sizeMinMax.y);

            if (sizeMinMax.y == finalSize && zoomRatio > 1 || sizeMinMax.x == finalSize && zoomRatio < 1)
            {
                zoomRatio = 1;
            }

            if (IsInputMatching(InputMapFingerPinch.ZoomAndRotate) || IsInputMatching(InputMapFingerPinch.RotateOnly))
            {
                twistRot = Quaternion.AngleAxis(allowYawRotation ? CameraInputs.twistDegrees : 0, GetRotateAroundVector());
                anyInteraction = true;
            }
            if (!isFocusing && IsInputMatching(InputMapFingerDrag.Translate))
            {
                worldPointFingersDelta = Vector3.ClampMagnitude(HeightScreenDepth.ConvertDelta(CameraInputs.lastScreenPointAnyFingerCountCenter,
                CameraInputs.screenPointAnyFingerCountCenter, gameObject), maxTranslationSpeed);
                anyInteraction = true;
            }
            //Log("worldPointFingersCenter:" + worldPointFingersCenter);
            //Log("worldPointFingersCenter:" + worldPointFingersCenter);
            //Log("twistRot:" + twistRot);

            Vector3 vecFingersCenterToCamera = (finalPosition - worldPointFingersCenter);
            float vecFingersCenterToCameraDistance = vecFingersCenterToCamera.magnitude * zoomRatio;
            vecFingersCenterToCamera = vecFingersCenterToCamera.normalized * vecFingersCenterToCameraDistance;

            Vector3 targetPosition = worldPointFingersCenter + vecFingersCenterToCamera;
            //Log("vecFingersCenterToCamera:" + vecFingersCenterToCamera);
            //Log("targetPosition:" + targetPosition);

            Vector3 offsetFromFingerCenter = worldPointFingersCenter - worldPointFingersDelta;

            finalPosition = twistRot * (targetPosition - worldPointFingersCenter) + offsetFromFingerCenter;
            finalRotation = twistRot * finalRotation;

            Vector2 pitchYaw = GetRotationToPitchYaw(finalRotation);
            currentPitch = pitchYaw.x;
            currentYaw = pitchYaw.y;

            Vector3 newWorldPointCameraCenter = CalculateOffset(finalPosition, finalRotation);
            Vector3 newWorldPointCameraCenterClamped = ClampInCameraBoundaries(newWorldPointCameraCenter);

            //Log("FinalPosition:" + finalPosition);
            //Log("1 FinalRotation:" + finalRotation);
            //Log("newWorldPointCameraCenter:" + newWorldPointCameraCenter);
            //Log("newWorldPointCameraCenterClamped:" + newWorldPointCameraCenterClamped);

            finalOffset = newWorldPointCameraCenterClamped;

            //Log("End FinalOffset:" + finalOffset);
            //Log("End FinalPosition:" + finalPosition);
            //Log("2 FinalRotation:" + FinalRotation);
            //Log("End FinalDistance:" + FinalDistance);


            if (isFocusing)
            {
                HandleFocus();
            }

            finalPosition = CalculatePosition(finalOffset, finalRotation, finalDistance);


            //Log("FinalOffset:" + finalOffset);
            //Log("FinalPosition:" + finalPosition);
            //Log("3 FinalRotation:" + FinalRotation);
            //Log("FinalDistance:" + FinalDistance);


            ApplyToCamera();

        }



        /// <summary>
        /// Converts a distance from ground to a camera orthographic size
        /// </summary>
        /// <param name="d"></param>
        public void SetSizeByDistance(float d)
        {
            finalSize = Mathf.Clamp(d * distanceToSize, sizeMinMax.x, sizeMinMax.y);
        }

        override protected float CalculateClampedDistance(float distance)
        {
            finalSize = Mathf.Clamp(distance * distanceToSize, sizeMinMax.x, sizeMinMax.y);
            return distance;
        }

        /// <summary>
        /// Converts the orthographic size to a distance from ground
        /// </summary>
        /// <returns></returns>
        public float GetDistanceFromSize()
        {
            return finalSize / distanceToSize;
        }

        /// <summary>
        /// In case the camera is standalone (no CameraModeSwitcher) then this is apply 
        /// the position and rotation to the camera
        /// </summary>
        override protected void ApplyToCamera()
        {
            if (!standalone)
                return;

            base.ApplyToCamera();

            cam.orthographicSize = finalSize;
        }

        /// <summary>
        /// Return the matrix of the camera transform, in order to blend it when switching modes
        /// </summary>
        /// <returns></returns>
        override public Matrix4x4 GetMatrix()
        {
            float aspect = cam.aspect;
            float near = cam.nearClipPlane, far = cam.farClipPlane;
            finalSize = Mathf.Clamp(finalSize, sizeMinMax.x, sizeMinMax.y);
            return Matrix4x4.Ortho(-finalSize * aspect, finalSize * aspect, -finalSize, finalSize, near, far);
        }

        /// <summary>
        /// Converts a camera offset on ground to a camera position
        /// </summary>
        virtual public void SetPositionByOffset()
        {
            finalPosition = CalculatePosition(finalOffset, finalRotation, finalDistance);
        }

        /// <summary>
        /// Set the initial values for the reset function
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="rotation"></param>
        /// <param name="distanceOrSize"></param>
        override public void SetResetValues(Vector3 offset, Quaternion rotation, float size)
        {
            initOffset = offset;
            initRotation = rotation;
            initSize = size;
        }




        #region FOCUS
        /// <summary>
        /// Setup the camera move animation
        /// </summary>
        /// <param name="targetOffsetPosition"></param>
        /// <param name="changeDistance"></param>
        /// <param name="targetDistanceOrSize"></param>
        /// <param name="changeRotation"></param>
        /// <param name="targetRotation"></param>
        /// <param name="allowYOffsetFromGround"></param>
        /// <param name="instant"></param>
        override protected void FocusCamera(
            bool changeOffsetPosition, Vector3 targetOffsetPosition,
           bool changeDistance, float targetDistanceOrSize,
           bool changeRotation, Quaternion targetRotation,
           bool allowYOffsetFromGround = false,
           bool instant = false)
        {

            if (!instant)
            {
                focusMoveOffset.Reset(finalOffset);
                focusMoveDistanceOrSize.Reset(finalSize);
                focusMoveRotation.Reset(finalRotation);
            }
            else
            {
                focusMoveOffset.Reset(targetOffsetPosition);
                focusMoveDistanceOrSize.Reset(targetDistanceOrSize);
                focusMoveRotation.Reset(targetRotation);
            }

            base.FocusCamera(changeOffsetPosition, targetOffsetPosition, changeDistance, targetDistanceOrSize,
                changeRotation, targetRotation, allowYOffsetFromGround, instant);
        }


        /// <summary>
        /// Focus the camera on a GameObject (distance animation)
        /// </summary>
        /// <param name="go">The gameObject to get closer to</param>
        /// <param name="allowYOffsetFromGround">Allow offseting the camera from the ground to match the object's pivot y position and height</param>
        override public void FocusCameraOnGameObject(GameObject go, bool allowYOffsetFromGround = false)
        {
            focusMoveOffset.Reset(finalOffset);
            focusMoveDistanceOrSize.Reset(finalSize);

            base.FocusCameraOnGameObject(go, allowYOffsetFromGround);
        }



        /// <summary>
        /// Handle the camera focus/moveto
        /// </summary>
        virtual protected void HandleFocus()
        {
            if (!isFocusing)
                return;

            if (focusTargetGo != null)
            {
                Bounds b = focusTargetGo.GetBoundsRecursive();

                if (b.size == Vector3.zero && b.center == Vector3.zero)
                    return;

                // offseting the bounding box
                if (allowYOffsetFromGround)
                {
                    float yOffset = b.center.y;
                    b.extents = b.extents.SetY(b.extents.y + yOffset);

                }
                b.center = b.center.SetY(groundHeight);

                Vector3 max = b.size;
                // Get the radius of a sphere circumscribing the bounds
                float radius = max.magnitude * focusRadiusMultiplier;

                focusTargetPosition = b.center;
                focusTargetDistanceOrSize = Mathf.Clamp(radius, sizeMinMax.x, sizeMinMax.y); ;
            }

            if (enableDistanceChange && !focusMoveDistanceOrSize.Completed)
            {
                finalSize = focusMove.Update(ref focusMoveDistanceOrSize,
                    focusTargetDistanceOrSize, OnFocusCompleted);
            }
            if (enableRotationChange && !focusMoveRotation.Completed)
            {
                finalRotation = focusMove.Update(ref focusMoveRotation,
                    focusTargetRotation, OnFocusCompleted);
                currentPitch = finalRotation.eulerAngles.x;
                currentYaw = finalRotation.eulerAngles.y;
            }
            if (enablePositionChange && !focusMoveOffset.Completed)
            {
                finalOffset = worldPointCameraCenter = focusMove.Update(ref focusMoveOffset,
                    focusTargetPosition, OnFocusCompleted);

            }

            finalPosition = CalculatePosition(finalOffset, finalRotation, finalDistance);

        }

        #endregion

    }
}
