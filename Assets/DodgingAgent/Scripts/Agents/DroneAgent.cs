using System;
using MBaske;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;
using DodgingAgent.Scripts.Core;

namespace DodgingAgent.Scripts.Agents
{
    public enum TrainingObjective
    {
        HoldPosition,
        Explore
    }
    
    /// <summary>
    /// Drone agent for ML-Agents training
    /// </summary>
    public class DroneAgent : Agent
    {
        [SerializeField] private Multicopter multicopter;

        [Header("Training Configuration")]
        [SerializeField] private TrainingObjective objective = TrainingObjective.HoldPosition;
        [SerializeField] private float resetDistance = 50f;
        [SerializeField, Range(0f, 1f)] private float objectiveRewardWeight = 0.3f;

        [Tooltip("Target distance per step (m). Gets max reward at this speed in Explore mode.")]
        [SerializeField] private float optimalStepDistance = 1f;

        [Header("Success Condition")]
        [Tooltip("HoldPosition: steps to hold near spawn")]
        [SerializeField] private float successHoldSteps = 500f;
        [Tooltip("Explore: total meters to travel")]
        [SerializeField] private float successExploreDistance = 100f;
        [Tooltip("HoldPosition: max distance from spawn (m) to count as holding")]
        [SerializeField] private float holdThreshold = 1f;
        [Tooltip("Bonus reward for completing goal (faster = better via less penalty time)")]
        [SerializeField] private float successBonus = 50f;

        private DroneResetter resetter;
        private RandomizePosition randomizePosition;
        private Vector3 origin;
        private Vector3 initialPosition;
        private Vector3 lastPosition;
        private float goalProgress;
        private float successGoal;
        
        public override void Initialize()
        {
            multicopter.Initialize();
            resetter = new DroneResetter(multicopter.Frame.parent);
            randomizePosition = GetComponent<RandomizePosition>();
            origin = transform.InverseTransformPoint(randomizePosition ? randomizePosition.transform.position : multicopter.Frame.position);
            Debug.Log($"[DroneAgent] Initialized with: Origin={origin}, RandomizePosition={randomizePosition != null}");
        }

        public override void OnEpisodeBegin()
        {
            resetter.Reset(randomizePosition ? randomizePosition.GetResetPosition() : null);
            initialPosition = transform.InverseTransformPoint(multicopter.Frame.position);
            lastPosition = initialPosition;
            goalProgress = 0f;
            successGoal = objective == TrainingObjective.HoldPosition ? successHoldSteps : successExploreDistance;
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            // Note: ImuSensor and LidarSensor observations are automatically collected
            // via SensorComponent - no manual collection needed!

            // Rotor thrust (4 observations)
            foreach (var rotor in multicopter.Rotors)
            {
                sensor.AddObservation(rotor.CurrentThrust);
            }
        }

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            // Handle thrust inputs
            float[] actions = actionBuffers.ContinuousActions.Array;
            float[] mappedThrust = new float[actions.Length];

            for (int i = 0; i < actions.Length; i++)
            {
                mappedThrust[i] = Mathf.Lerp(-0.4f, 1f, (actions[i] + 1f) * 0.5f);
            }
            multicopter.UpdateThrust(mappedThrust);

            Vector3 localFramePosition = transform.InverseTransformPoint(multicopter.Frame.position);

            switch (objective)
            {
                case TrainingObjective.HoldPosition: // Position holding reward (exponential decaying reward)
                    float distanceFromInitial = Vector3.Distance(localFramePosition, initialPosition);
                    float positionReward = Mathf.Exp(-distanceFromInitial * 0.5f);
                    AddReward(positionReward * objectiveRewardWeight);

                    // Calculate Goal Progress
                    if (distanceFromInitial <= holdThreshold)
                    {
                        goalProgress++;
                        if (goalProgress >= successGoal)
                        {
                            AddReward(successBonus);
                            Debug.Log($"{gameObject.name} SUCCESS! Held position for {goalProgress} steps. Final reward: {GetCumulativeReward():F2}");
                            EndEpisode();
                        }
                    } else { goalProgress = 0; } // Reset on drift
                    break;

                case TrainingObjective.Explore: // distance traveled this step (exponential decaying reward)
                    float distanceTraveled = Vector3.Distance(localFramePosition, lastPosition);
                    float explorationReward = Mathf.Exp(-Mathf.Abs(distanceTraveled - optimalStepDistance) * 0.5f);
                    AddReward(explorationReward * objectiveRewardWeight);
                    lastPosition = localFramePosition;

                    // Calculate Goal Progress
                    goalProgress += distanceTraveled;
                    if (goalProgress >= successGoal)
                    {
                        AddReward(successBonus);
                        Debug.Log($"{gameObject.name} SUCCESS! Explored {goalProgress:F2}m in {StepCount} steps. Final reward: {GetCumulativeReward():F2}");
                        EndEpisode();
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            // Debug.Log($"{gameObject.name} - Goal Progress: {goalProgress:F2} / {successGoal:F2} ({(goalProgress/successGoal*100f):F1}%)");
            // TODO: Make a ui bar to view progress on inference

            // Stay upright
            AddReward(Mathf.Clamp01(multicopter.Frame.up.y) * 0.75f);
            
            // Don't move to crazily
            float velocityMag = multicopter.Rigidbody.linearVelocity.magnitude;
            if (velocityMag > 0.5f) { AddReward(-(velocityMag - 0.5f) * 0.1f); }
            AddReward(multicopter.Rigidbody.angularVelocity.magnitude * -0.05f);
            
            // Check for distance from origin
            if (Vector3.Distance(localFramePosition, origin) > resetDistance)
            {
                Debug.Log($"[DroneAgent] RESET: Flew too far from origin! Distance: {Vector3.Distance(localFramePosition, origin):F2}m (limit: {resetDistance}m)");
                EndEpisode();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Debug.Log($"HandleCollision hit {collision.gameObject.name}: Tag={collision.gameObject.tag}");
            if (collision.gameObject.CompareTag("Wall"))
            {
                AddReward(-150f);
                // Debug.Log($"{gameObject.name} crashed after {StepCount} steps with reward {currentReward:F2}, penalty: {crashPenalty:F2}, final: {GetCumulativeReward():F2}");
                EndEpisode();
            }
        }
        
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var cont = actionsOut.ContinuousActions;

            // Throttle (Space/Shift for up/down)
            float throttle = 0f;
            if (Keyboard.current.spaceKey.isPressed) throttle = 1f;
            if (Keyboard.current.leftShiftKey.isPressed) throttle = -1f;

            // Pitch (W/S - forward/backward)
            float pitch = 0f;
            if (Keyboard.current.wKey.isPressed) pitch = 1f;
            if (Keyboard.current.sKey.isPressed) pitch = -1f;

            // Roll (A/D - left/right)
            float roll = 0f;
            if (Keyboard.current.aKey.isPressed) roll = -1f;
            if (Keyboard.current.dKey.isPressed) roll = 1f;

            // Yaw (Q/E - rotate left/right)
            float yaw = 0f;
            if (Keyboard.current.qKey.isPressed) yaw = -1f;
            if (Keyboard.current.eKey.isPressed) yaw = 1f;

            // Standard quadcopter mixing (assuming 4 rotors in X configuration)
            if (multicopter.Rotors.Length == 4)
            {
                cont[0] = throttle - pitch + roll - yaw;
                cont[1] = throttle - pitch - roll + yaw;
                cont[2] = throttle + pitch - roll - yaw;
                cont[3] = throttle + pitch + roll + yaw;

                for (int i = 0; i < 4; i++)
                {
                    cont[i] = Mathf.Clamp(cont[i], -1f, 1f);
                }
            }
            else
            {
                for (int i = 0; i < multicopter.Rotors.Length; i++)
                {
                    cont[i] = throttle;
                }
            }
        }
    }
}