﻿using System;
using UnityCommons;
using UnityEngine;

namespace Pudge.RunningBinding.FromUnity
{
    public static class Indicator
    {
        public static Texture Circle;
        public static Texture Angle;
        public static Material Transparent;

        static Indicator()
        {
            Transparent = GetPrefab<Material>("Transparent");
            Circle = GetPrefab<Texture>("CircleIndicator");
            Angle = GetPrefab<Texture>("AngleIndicator");
        }

        public static T GetPrefab<T>(string name) where T : UnityEngine.Object
        {
            var prefab = PrefabLoader.InstantiatePrefab(name);
            if (prefab == null)
                throw new ApplicationException();
            return (T)prefab;
        }

        public static void Create(Transform parent, Texture texture, Color color, double radius)
        {
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Plane);
            indicator.GetComponent<MeshCollider>().convex = true;
            GameObject.Destroy(indicator.GetComponent<Collider>());
            GameObject.Destroy(indicator.GetComponent<Rigidbody>());

            var renderer = indicator.GetComponent<Renderer>();
            renderer.material = Transparent;
            renderer.material.mainTexture = texture;
            renderer.material.color = color;

            indicator.transform.SetParent(parent);
            indicator.transform.localPosition = Vector3.up;
            indicator.transform.localRotation = Quaternion.identity;
            indicator.transform.Rotate(new Vector3(0, 180, 0));

            indicator.transform.localScale = Vector3.one * 0.2f * (float)radius;
            indicator.name = "Marker";
        }
    }
}
