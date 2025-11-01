using System;
using System.IO;
using UnityEngine;

[Serializable]
public struct PlaneBuilder
{
    public Vector3 origin;

    public PlaneBuilder(Vector3 origin)
    {
        this.origin = origin;
    }
}

[Serializable]
public class PlaneSet
{
    public Vector3 normal;
    public PlaneBuilder[] planes;

    public PlaneSet(Vector3 normal, PlaneBuilder[] planes)
    {
        this.normal = normal.normalized;
        this.planes = planes;
    }

    // --- Binary Write ---
    public static void Save(string path, PlaneSet data)
    {
        using var bw = new BinaryWriter(File.Open(path, FileMode.Create));

        // Write normal (once)
        bw.Write(data.normal.x);
        bw.Write(data.normal.y);
        bw.Write(data.normal.z);

        // Write number of planes
        bw.Write(data.planes.Length);

        // Write each origin
        foreach (var plane in data.planes)
        {
            bw.Write(plane.origin.x);
            bw.Write(plane.origin.y);
            bw.Write(plane.origin.z);
        }
    }

    // --- Binary Read ---
    public static PlaneSet Load(string path)
    {
        using var br = new BinaryReader(File.OpenRead(path));

        // Read shared normal
        var normal = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        // Read number of planes
        int count = br.ReadInt32();
        var planes = new PlaneBuilder[count];

        // Read each origin
        for (int i = 0; i < count; i++)
        {
            var origin = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            planes[i] = new PlaneBuilder(origin);
        }

        return new PlaneSet(normal, planes);
    }
}