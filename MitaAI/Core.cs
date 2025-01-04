using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(MitaAI.Core), "MitaAI", "1.0.0", "Dmitry", null)]
[assembly: MelonGame("AIHASTO", "MiSideFull")]

namespace MitaAI
{
    public class Core : MelonMod
    {
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Scene 4 - StartSecret")
            {
                LoggerInstance.Msg($"Мы в нужном месте");

                // Проверка и логирование для Миты
                MitaPerson Mita = GameObject.Find("Mita")?.GetComponent<MitaPerson>();
                if (Mita != null)
                {
                    LoggerInstance.Msg("Мита найдена!");
                }
                else
                {
                    LoggerInstance.Msg("Мита не найдена.");
                }

                // Проверка и логирование для игрока (персонажа)
                PlayerPerson Target = GameObject.Find("Person")?.GetComponent<PlayerPerson>();
                if (Target != null)
                {
                    LoggerInstance.Msg("Игрок найден!");
                    Mita?.AiWalkToTarget(Target.GetComponent<Transform>());
                }
                else
                {
                    LoggerInstance.Msg("Игрок не найден.");
                }
            }
        }
    }
}
