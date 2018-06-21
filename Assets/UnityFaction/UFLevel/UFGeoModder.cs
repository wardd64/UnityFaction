using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UFLevelStructure;
using System;

public class UFGeoModer : MonoBehaviour {

    public int defaultHardness;
    public GeoRegion[] regions;
    public Material geoMaterial;

    public void Set(UFLevel level, Material geoMaterial) {
        this.defaultHardness = level.hardness;
        this.regions = level.geoRegions;
        this.geoMaterial = geoMaterial;
    }

    /// <summary>
    /// Geomods the given point with the given amount of power.
    /// The extent of the damage scales inversely with the level hardness.
    /// </summary>
    public void Mod(Vector3 point, float power) {
        throw new Exception("GeoMods not yet implemented");
        //TODO implement this
    }

    /// <summary>
    /// Get level hardness at specific point in the map.
    /// </summary>
    private int GetHardness(Vector3 point) {
        foreach(GeoRegion region in regions) {
            if(Inside(point, region))
                return region.hardness;
        }
        return defaultHardness;
    }

    private bool Inside(Vector3 point, GeoRegion region) {
        PosRot center = region.transform.posRot;
        bool box = region.shape == GeoRegion.GeoShape.box;
        float radius = region.sphereRadius;
        Vector3 extents = region.extents;

        return UFUtils.Inside(point, center, radius, extents, box);
    }
}
