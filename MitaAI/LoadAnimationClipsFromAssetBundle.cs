using UnityEngine;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;

public class LoadAnimationClipsFromAssetBundle
{
    private string assetBundlePath = Path.Combine(MelonEnvironment.GameRootDirectory, "Mods", "myassetbundle"); // Путь к AssetBundle в папке Mods
    private bool First = true;

    public void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        Debug.Log("21213312");
        if (First)
        {
            LoadAnimationClipsContainingMita();
            First = false;
        }
    }

    public string LoadAnimationClipsContainingMita()
    {
        // Загружаем AssetBundle
        AssetBundle assetBundle = AssetBundle.LoadFromFile(assetBundlePath);

        if (assetBundle == null)
        {
            Debug.Log("Failed to load AssetBundle.");
            return "Fail";
        }

        // Загружаем все AnimationClip из AssetBundle
        AnimationClip[] clips = assetBundle.LoadAllAssets<AnimationClip>();

        foreach (var clip in clips)
        {
            if (clip.name.Contains("Mita"))
            {
                Debug.Log($"Found animation clip: {clip.name}");
            }
        }

        // Не забываем освободить AssetBundle после использования
        assetBundle.Unload(false);
        return "Succes";
    }
}
