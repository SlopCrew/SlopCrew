using ch.sycoforge.Decal;
using Reptile;
using UnityEngine;

namespace SlopCrew.Plugin;

public class SlopGraffitiSpot : MonoBehaviour {
    public GraffitiSpot OriginalGraffitiSpot;
    
    private EasyDecal topGraffiti;
    private GraffitiArt? topGraffitiArt;
    
    public void Init(GraffitiSpot originalSpot, EasyDecal newGraffiti) {
        this.OriginalGraffitiSpot = originalSpot;
        this.topGraffiti = newGraffiti;
        this.topGraffiti.DecalRenderer.enabled = false;
    }
    
    public void Paint(GraffitiArt graffitiArt, Player byPlayer = null) {
        this.topGraffitiArt = graffitiArt;
        this.topGraffiti.DecalMaterial = graffitiArt.graffitiMaterial;
        this.topGraffiti.DecalRenderer.enabled = true;
    }
}
