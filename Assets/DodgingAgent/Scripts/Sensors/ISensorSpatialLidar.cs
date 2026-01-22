using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace DodgingAgent.Scripts.Sensors
{
    /// <summary>
    /// Spatial LiDAR sensor that builds a point cloud map of the environment.
    /// Collects world-space points with step-based timestamps.
    /// </summary>
    public class ISensorSpatialLidar : ISensor
    {
        private readonly Transform _referenceTransform;
        private readonly float _maxDistance;
        private readonly LayerMask _detectionLayers;
        private readonly Vector3[] _rayDirections;
        private readonly Agent _agent; // needed for step count

        public readonly bool recordMap;
        private int _episode;
        private readonly List<SpatialStamp> spatialMap;

        [Serializable]
        public struct SpatialStamp
        {
            public Vector3[] vectors;
            public int step;
            public int episode;
            
            public SpatialStamp(Vector3[] vectors, int step, int episode)
            {
                this.vectors = vectors;
                this.step = step;
                this.episode = episode;
            }
        }

        public ISensorSpatialLidar(Agent agent, Transform referenceTransform, float maxDistance, LayerMask detectionLayers, int numberOfRays = 360, bool recordMap = false)
        {
            _agent = agent;
            _referenceTransform = referenceTransform;
            _maxDistance = maxDistance;
            _detectionLayers = detectionLayers;
            _rayDirections = SetupUnitDirections(numberOfRays);

            this.recordMap = recordMap;
            if (recordMap) spatialMap = new List<SpatialStamp>();
        }

        private static Vector3[] SetupUnitDirections(int n)
        {
            List<Vector3> directions = new List<Vector3>();
            float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f)); // roughly 2.399963
            
            for (int i = 0; i < n; i++)
            {
                float y =  1f - 2f * ((i + 0.5f) / n); // -1 to 1
                float r = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
                float theta = goldenAngle * i;
                
                float x = Mathf.Cos(theta) * r;
                float z = Mathf.Sin(theta) * r;

                directions.Add(new Vector3(x, y, z));
            }

            return directions.ToArray();
        }

        public ObservationSpec GetObservationSpec() => ObservationSpec.Vector(_rayDirections.Length * 3);

        public int Write(ObservationWriter writer)
        {
            Vector3[] observations = new Vector3[_rayDirections.Length];
            Vector3 origin = _referenceTransform.position;

            for (int i = 0; i < _rayDirections.Length; i++)
            {
                Vector3 worldDirection = _referenceTransform.TransformDirection(_rayDirections[i]);

                if (Physics.Raycast(origin, worldDirection, out RaycastHit hit, _maxDistance, _detectionLayers))
                {
                    observations[i] = _rayDirections[i] * hit.distance;
                }
                else
                {
                    observations[i] = Vector3.zero;
                }
            }

            if (recordMap)
            {
                spatialMap.Add(new SpatialStamp(observations, _agent.StepCount, _episode));
            }
            
            for (int i = 0; i < observations.Length; i++)
            {
                writer.Add(observations[i], i * 3);
            }
            return _rayDirections.Length * 3;
        }

        public byte[] GetCompressedObservation() => null;
        public void Update() { }

        public void Reset()
        {
            _episode++;
        }
        public CompressionSpec GetCompressionSpec() => CompressionSpec.Default();
        public string GetName() => "SpatialLidarSensor";

        public Vector3[] GetRayDirections() => _rayDirections;
    }
}