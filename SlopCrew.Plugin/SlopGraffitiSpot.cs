using System;
using ch.sycoforge.Decal;
using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin;

public class SlopGraffitiSpot : MonoBehaviour {
    public GraffitiSpot OriginalGraffitiSpot;
    public bool firstTime = true;

    private EasyDecal graffiti;
    private GraffitiArt? graffitiArt;
    private int swirlControlHash;
    private GameObject marker;
    private bool isMarkerActive;

    public void Awake() {
        this.swirlControlHash = Shader.PropertyToID("_SwirlControl");
    }

    public void Init(GraffitiSpot originalSpot, EasyDecal graffitiDecal, GameObject graffitiMarker) {
        this.OriginalGraffitiSpot = originalSpot;
        this.graffiti = graffitiDecal;
        this.marker = graffitiMarker;
        this.ToggleMarker(true);
        this.SetGraffitiDecal(originalSpot.emptyGraffitiMaterial);
    }

    private void ToggleMarker(bool showMarker) {
        // IDEK what this marker does but whatever it's staying now
        this.isMarkerActive = showMarker;
        this.marker.SetActive(showMarker);
    }

    public void Paint(GraffitiArt newGraffitiArt, Player byPlayer = null) {
        this.graffitiArt = newGraffitiArt;
        this.SetGraffitiDecal(graffitiArt.graffitiMaterial);
        this.ToggleMarker(false);
        this.firstTime = false;
    }

    private void SetGraffitiDecal(Material newGraffitiMaterial) {
        this.graffiti.DecalMaterial = newGraffitiMaterial;
        this.graffiti.DecalRenderer.enabled = true;
        this.SetGraffitiMaterial(this.graffiti.DecalRenderer.material, 3001, this.swirlControlHash, false);
    }

    private void SetGraffitiMaterial(Material material, int renderQueue, int swirlControlHash, bool setMarkerActive) {
        material.renderQueue = renderQueue;
        material.SetFloat(swirlControlHash, Convert.ToInt32(setMarkerActive));
    }
}
