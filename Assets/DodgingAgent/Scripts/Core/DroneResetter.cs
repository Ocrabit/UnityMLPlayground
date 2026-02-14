using UnityEngine;
using System.Collections.Generic;

namespace DodgingAgent.Scripts.Core
{
    public class ResettableItem
    {
        private readonly Vector3 localPos;
        private readonly Quaternion localRot;
        private readonly Transform tf;
        private readonly Rigidbody rb;
        private readonly ConfigurableJoint joint;

        public ResettableItem(Transform tf)
        {
            this.tf = tf;
            localPos = tf.localPosition;
            localRot = tf.localRotation;
            rb = tf.GetComponent<Rigidbody>();
            joint = tf.GetComponent<ConfigurableJoint>();
        }

        public void Reset()
        {
            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }

            if (joint)
                joint.targetRotation = Quaternion.identity;

            tf.localPosition = localPos;
            tf.localRotation = localRot;
        }

        public void Wake()
        {
            if (rb) rb.WakeUp();
        }
    }

    /// <summary>
    /// Resets drone transform and physics state. Supports random repositioning.
    /// </summary>
    public class DroneResetter
    {
        private readonly Transform root;
        private readonly List<ResettableItem> items;

        public DroneResetter(Transform root)
        {
            this.root = root;
            items = new List<ResettableItem>();
            AddRecursive(root);
        }

        private void AddRecursive(Transform tf)
        {
            items.Add(new ResettableItem(tf));
            for (int i = 0; i < tf.childCount; i++)
                AddRecursive(tf.GetChild(i));
        }

        public void Reset(Vector3? newPosition = null)
        {
            foreach (var item in items) item.Reset();

            if (newPosition.HasValue) root.position = newPosition.Value;

            // foreach (var item in items) item.Wake();
        }
    }
}
