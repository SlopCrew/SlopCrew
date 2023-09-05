using System.Collections;
using System.Collections.Generic;
using ch.sycoforge.Decal;
using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin.Encounters; 

public class SlopGraffitiEncounter : SlopEncounter {
    private List<GameObject> slopGraffitiSpots = new();

    public SlopGraffitiEncounter() {
        Plugin.Log.LogInfo("Created SlopGraffitiEncounter");
    }

    public void Start() {
        var graffitiSpots = Resources.FindObjectsOfTypeAll<GraffitiSpot>();
        
        Plugin.Log.LogInfo($"Found {graffitiSpots.Length} graffiti spots");

        foreach (var graffitiSpot in graffitiSpots) {
            var newGraffitiSpot = CreateSlopGraffitSpot(graffitiSpot);
            this.slopGraffitiSpots.Add(newGraffitiSpot);
        }
    }

    public GameObject CreateSlopGraffitSpot(GraffitiSpot originalSpot) {
        // Hide the old graffiti decals
        originalSpot.topGraffiti.gameObject.SetActive(false);
        originalSpot.bottomGraffiti.gameObject.SetActive(false);
        
        // Make our new graffiti objects
        var newGraffitiSpot = new GameObject("Slop_" + originalSpot.gameObject.name);
        var slopGraffitiComponent = newGraffitiSpot.AddComponent<SlopGraffitiSpot>();
        var newCollider = newGraffitiSpot.AddComponent<BoxCollider>();
            
        // Set up the new graffiti objects
        newGraffitiSpot.gameObject.layer = originalSpot.gameObject.layer;
        newGraffitiSpot.tag = originalSpot.tag;
        
        var collider = originalSpot.gameObject.GetComponent<BoxCollider>();
        newCollider.center = collider.center;
        newCollider.size = collider.size;
        newCollider.isTrigger = true;
        
        var newColliderTransform = newCollider.transform;
        var colliderTransform = collider.transform;
        newColliderTransform.position = colliderTransform.position;
        newColliderTransform.rotation = colliderTransform.rotation;

        newGraffitiSpot.transform.SetParent(originalSpot.transform.parent);
        
        // Set up the graffiti decal
        var newGraffitiDecal = new GameObject("SlopGraffiti");
        var decalComponent = newGraffitiDecal.gameObject.AddComponent<EasyDecal>();

        var newDecalTransform = newGraffitiDecal.transform;
        var originalDecalTransform = originalSpot.topGraffiti.transform;
        newDecalTransform.position = originalDecalTransform.position;
        newDecalTransform.rotation = originalDecalTransform.rotation;
        newDecalTransform.localScale = originalDecalTransform.localScale;
        
        newGraffitiDecal.transform.SetParent(newGraffitiSpot.transform);
        slopGraffitiComponent.Init(originalSpot, decalComponent);
        
        return newGraffitiSpot;
    }

    public void CleanUp() {
        foreach (var graffitiSpot in this.slopGraffitiSpots) {
            Object.Destroy(graffitiSpot);
        }
        
        var originalGraffiti = Resources.FindObjectsOfTypeAll<GraffitiSpot>();
        
        foreach (var graffitiSpot in originalGraffiti) {
            graffitiSpot.gameObject.SetActive(true);
        }
    }
}
