﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ENTICourse.IK
{

    // A typical error function to minimise
    public delegate float ErrorFunction(Vec3 target, float[] solution);

    public struct PositionRotation
    {
        Vec3 position;   
        Quat rotation;

        public PositionRotation(Vec3 position, Quat rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }
        
        // PositionRotation to Vector3
        public static implicit operator Vec3(PositionRotation pr)
        {
            return pr.position;
        }
        // PositionRotation to Quaternion
        public static implicit operator Quat(PositionRotation pr)
        {
            return pr.rotation;
        }
    }

    //[ExecuteInEditMode]
    public class InverseKinematics : MonoBehaviour
    {
        [Header("Joints")]
        public Transform BaseJoint;

        //[ReadOnly]
        public RobotJoint[] Joints = null;
        // The current angles
        [ReadOnly]
        public float[] Solution = null;

        [Header("Destination")]
        public Transform Effector;
        [Space]
        public Transform Destination;
        public float DistanceFromDestination;
        [ReadOnly]
        public Vec3 target;

        [Header("Inverse Kinematics")]
        [Range(0, 1f)]
        public float DeltaGradient = 0.1f; // Used to simulate gradient (degrees)
        [Range(0, 100f)]
        public float LearningRate = 0.1f; // How much we move depending on the gradient

        [Space()]
        [Range(0, 0.25f)]
        public float StopThreshold = 0.1f; // If closer than this, it stops
        [Range(0, 10f)]
        public float SlowdownThreshold = 0.25f; // If closer than this, it linearly slows down


        public ErrorFunction ErrorFunction;



        [Header("Debug")]
        public bool DebugDraw = true;



        // Use this for initialization
        void Start()
        {
            target = (Vec3)Destination.position;
            if (Joints == null)
                GetJoints();
        
           ErrorFunction = DistanceFromTarget;
        }

        [ExposeInEditor(RuntimeOnly = false)]
        public void GetJoints()
        {
            Joints = BaseJoint.GetComponentsInChildren<RobotJoint>();
            Solution = new float[Joints.Length];
        }



        // Update is called once per frame
        void Update()
        {
            // Do we have to approach the target?
            //TODO
            BallBehavior ball = Destination.GetComponent<BallBehavior>();
            //StopThreshold = ball.velocitat - Mathf.Abs((ball.velocitat * ball.velocitat) * ball.amplitud * Mathf.Sin(ball.velocitat * Time.time));

            target = (Vec3)Destination.position;
           
            if (ErrorFunction(target, Solution) > StopThreshold)
                ApproachTarget(target);

            if (DebugDraw)
            {
                Debug.DrawLine(Effector.transform.position, (Vector3)target, Color.green);
                Debug.DrawLine(Destination.transform.position, (Vector3)target, new Color(0, 0.5f, 0));
            }
        }

        public void ApproachTarget(Vec3 target)
        {
            //TODO

            //Calculate new angle value
            //Apply new angle value to robot joint with method MoveArm
            for(int i = 0; i < Solution.Length; i++)
            {
                Solution[i] -= LearningRate * CalculateGradient(target, Solution, i, DeltaGradient);
                if (Joints[i].Platform){
                    float aux = 0;
                    for (int j = i - 1; j >= 0; j--)
                    {
                        if(Joints[j].Axis == Joints[i].Axis)
                            aux -= Solution[j];
                    }
                    Solution[i] = aux - 90;
                }
                Joints[i].MoveArm(Solution[i]);
            }

        }

        
        public float CalculateGradient(Vec3 target, float[] Solution, int i, float delta)
        {
            //TODO
            float gradient = 0;
            float f_x = DistanceFromTarget(target, Solution);
            Solution[i] += delta;
            float f_x_plus_h = DistanceFromTarget(target, Solution);
            gradient = (f_x_plus_h - f_x) / delta;        

            return gradient;
        }

        // Returns the distance from the target, given a solution
        public float DistanceFromTarget(Vec3 target, float[] Solution)
        {
            Vec3 point = ForwardKinematics(Solution);
            return (point - target).Module();
        }


        /* Simulates the forward kinematics,
         * given a solution. */


        public PositionRotation ForwardKinematics(float[] Solution)
        {
            Vec3 prevPoint = (Vec3)Joints[0].transform.position;
            //Quaternion rotation = Quaternion.identity;

            // Takes object initial rotation into account
            Quat rotation = (Quat)transform.rotation;
            for (int i = 1; i < Joints.Length; i++)
            {
                // Rotates around a new axis
                rotation *= Quat.AngleAxis(Solution[i - 1], (Vec3)Joints[i - 1].Axis);
                Vec3 nextPoint = prevPoint + rotation * Joints[i].StartOffset;

                if (DebugDraw)
                    Debug.DrawLine((Vector3)prevPoint, (Vector3)nextPoint, Color.blue);

                prevPoint = nextPoint;
            }

            // The end of the effector
            return new PositionRotation(prevPoint, rotation);
        }
    }
}