using MelonLoader;
using MelonLoader.Utils;
using System.IO;
using UnityEngine;


// Не работает, нужен ассент бендл
public class AnimationLoader
{

    public List<AnimationClip> InitAnimations()
    {

        // Получаем путь к папке модов
        string modFolderPath = MelonEnvironment.ModsDirectory;

        string[] animationFiles = Directory.GetFiles(modFolderPath, "*.anim", SearchOption.AllDirectories);

        List<AnimationClip> AnimationClips = new List<AnimationClip>();
        foreach (string animationPath in animationFiles)
        {
            // Пример загрузки из AssetBundle
            AnimationClip clip = LoadAnimationClip(animationPath);

            if (clip != null)
            {
                MelonLogger.Msg($"Loaded animation: {clip.name}");
                AnimationClips.Add(clip);
                // Применяйте анимацию, как требуется
            }
            else
            {
                MelonLogger.Warning($"Failed to load animation from {animationPath}");
            }
        }

        return AnimationClips;
    }

    private AnimationClip LoadAnimationClip(string path)
    {
        // Реализация загрузки анимации
        return null; // Замените на подходящий способ загрузки
    }
}
