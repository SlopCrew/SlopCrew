

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

        var texture = new Texture2D(width, height);
        texture.LoadImage(bytes);
        texture.Apply();

        return texture;
    }

    public static Sprite LoadResourceAsSprite(string path, int width, int height, float pivotX = 0.5f, float pivotY = 0.5f) {
        Texture2D texture = LoadResourceAsTexture(path, width, height);
        return Sprite.Create(new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(pivotX, pivotY), 100.0f, texture);
    }
}
