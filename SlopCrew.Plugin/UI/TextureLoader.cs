

using System.Reflection;
using System;
using UnityEngine;

namespace SlopCrew.Plugin.UI;

public class TextureLoader {
    public static Texture2D LoadResourceAsTexture(string path, int width, int height) {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
        if (stream is null) throw new Exception($"Could not load texture at path: {path}");

        var bytes = new byte[stream.Length];
        var read = 0;
        while (read < bytes.Length) {
            read += stream.Read(bytes, read, bytes.Length - read);
        }

        Texture2D texture = new(width, height);
        texture.LoadImage(bytes);
        texture.Apply();

        return texture;
    }
}
