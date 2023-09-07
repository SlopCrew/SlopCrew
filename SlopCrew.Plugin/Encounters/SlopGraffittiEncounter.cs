using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using ch.sycoforge.Decal;
using Reptile;
using SlopCrew.Common;
using SlopCrew.Common.Network.Clientbound;
using UnityEngine;

namespace SlopCrew.Plugin.Encounters;

public class SlopGraffitiEncounter : SlopEncounter {
    private List<GameObject> slopGraffitiSpots = new();
    private GraffitiSpot[]? graffitiSpots;
    private int numOfSpots = 5;
    private int scoreToWin = 3;

    public override void Start(ClientboundEncounterStart encounterStart) {
        base.Start(encounterStart);
        GraffitiEncounterConfig config = (GraffitiEncounterConfig) encounterStart.EncounterConfig;
        graffitiSpots = Resources.FindObjectsOfTypeAll<GraffitiSpot>();
        this.scoreToWin = (numOfSpots + 1) / 2;

        foreach (var graffitiSpot in this.graffitiSpots) {
            // Hide the old graffiti decals
            graffitiSpot.topGraffiti.gameObject.SetActive(false);
            graffitiSpot.bottomGraffiti.gameObject.SetActive(false);
        }

        for (int i = 0; i < numOfSpots; i++) {
            var graffitiSpot = this.graffitiSpots[config.GraffitiSpots[i]];
            var slopGraffitiSpot = CreateSlopGraffitSpot(graffitiSpot);
            this.slopGraffitiSpots.Add(slopGraffitiSpot);
        }
    }

    public GameObject CreateSlopGraffitSpot(GraffitiSpot originalSpot) {
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

        // Set up the shine marker
        var newGraffitiMarker = new GameObject("GraffitiMarker");
        var shineMarker = originalSpot.marker.transform.GetChild(0);
        var newShineMarker = new GameObject("ShineMarker");
        var newMeshFilter = newShineMarker.AddComponent<MeshFilter>();
        var newMeshRenderer = newShineMarker.AddComponent<MeshRenderer>();

        newGraffitiMarker.transform.position = originalSpot.marker.transform.position;
        newShineMarker.transform.position = shineMarker.transform.position;
        newShineMarker.transform.rotation = shineMarker.transform.rotation;
        newMeshFilter.mesh = shineMarker.GetComponent<MeshFilter>().mesh;
        newMeshRenderer.material = shineMarker.GetComponent<MeshRenderer>().material;

        newGraffitiMarker.transform.SetParent(newGraffitiSpot.transform);
        newShineMarker.transform.SetParent(newGraffitiMarker.transform);

        // FINALLY DONE
        slopGraffitiComponent.Init(originalSpot, decalComponent, newGraffitiMarker);

        return newGraffitiSpot;
    }

    public override void EncounterUpdate() {
        this.MyScore = Plugin.PlayerManager.GraffitiCount;
        this.TheirScore = this.Opponent.GraffitiCount;
        
        if (this.MyScore >= scoreToWin || this.TheirScore >= scoreToWin) {
            this.SetEncounterState(SlopEncounterState.Outro);
        }
    }

    public override void EncounterCleanUp() {
        foreach (var slopSpot in this.slopGraffitiSpots) {
            Object.Destroy(slopSpot);
        }

        foreach (var graffitiSpot in this.graffitiSpots) {
            graffitiSpot.topGraffiti.gameObject.SetActive(true);
            graffitiSpot.bottomGraffiti.gameObject.SetActive(true);
        }
    }
}
