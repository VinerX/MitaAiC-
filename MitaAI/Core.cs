using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Net.Sockets;
using System.Text;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using Unity.Collections;
using UnityEngine.UI;
using System.Text.RegularExpressions;

[assembly: MelonInfo(typeof(MitaAI.Core), "MitaAI", "1.0.0", "Dmitry", null)]
[assembly: MelonGame("AIHASTO", "MiSideFull")]

namespace MitaAI
{

    public class Core : MelonMod
    {
        MitaPerson Mita = GameObject.Find("Mita")?.GetComponent<MitaPerson>();
        PlayerPerson Target = GameObject.Find("Person")?.GetComponent<PlayerPerson>();
        GameObject CustomDialog;
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Scene 4 - StartSecret")
            {
                Mita = GameObject.Find("Mita")?.GetComponent<MitaPerson>();
                Target = GameObject.Find("Person")?.GetComponent<PlayerPerson>();
                LoggerInstance.Msg($"We in right place scene 4 ");

                // Проверка и логирование для Миты

                if (Mita != null)
                {
                    LoggerInstance.Msg("Mita found!");
                }
                else
                {
                    LoggerInstance.Msg("Mita not found");
                }

                // Проверка и логирование для игрока (персонажа)

                if (Target != null)
                {
                    LoggerInstance.Msg("player found !");
                    // Mita?.AiWalkToTarget(Target.GetComponent<Transform>());
                }
                else
                {
                    LoggerInstance.Msg("player not found.");
                }


                LoggerInstance.Msg("Creating Answer object.");
                Transform world = GameObject.Find("World")?.transform;

                if (world == null)
                {
                    Debug.LogError("Объект 'World' не найден.");

                }

                // Спускаемся по дереву детей
                Transform dialogOriginal = world.Find("Quests/Quest 1/Dialogues/Dialogue Mita/Dialogue Finish Aihastion/DMita 2");

                if (dialogOriginal == null)
                {
                    Debug.LogError("Целевой объект 'DMita 2' не найден.");

                }

                // Создаем копию объекта
                CustomDialog = GameObject.Instantiate(dialogOriginal.gameObject, world.Find("Quests/Quest 1/Dialogues"));
                CustomDialog.name = "Custom Dialoge";
            }
        }

        private float timer = 0f; // Время прошедшее с последнего запроса
        private const float interval = 20; // Интервал в секундах (8 секунд)

        async public override void OnFixedUpdate()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.name != "Scene 4 - StartSecret") { return; }

            // Увеличиваем таймер на время прошедшее с последнего кадра
            timer += Time.fixedDeltaTime;



            // Проверяем, прошло ли 8 секунд
            if (timer >= interval)
            {
                // Сбрасываем таймер и отправляем запрос
                timer = 0f;
                var response = "";
                try
                {
                    LoggerInstance.Msg("Trying get resuly from pythin");
                    response = await Task.Run(() => GetResponseFromPythonSocketAsync("Привет, Мита!"));
                }
                catch (Exception ex)
                {
                    LoggerInstance.Msg("Problem with getting answer");
                    LoggerInstance.Msg($"Error: {ex.Message}");
                    LoggerInstance.Msg($"StackTrace: {ex.StackTrace}");
                }
                // Отправка асинхронного запроса в отдельном потоке
                try
                {
                    EmotionType emotion = ExtractEmotions(ref response);
                }
                catch (Exception)
                {
                    LoggerInstance.Msg("Problem with amotions");
                    throw;
                }


                try
                {
                    CreateDialogue(response, "");
                }
                catch (Exception ex)
                {
                    LoggerInstance.Msg("Problem with using dialoge.");
                    LoggerInstance.Msg($"Error: {ex.Message}");
                    LoggerInstance.Msg($"StackTrace: {ex.StackTrace}");
                }


            }

            base.OnFixedUpdate();
        }
        private void CreateDialogue(string response, string emotion = "")
        {
            LoggerInstance.Msg("Dialog start");

            if (CustomDialog == null || Mita == null) return;

            GameObject LocalDialog = GameObject.Instantiate(CustomDialog, CustomDialog.transform.parent);

            Dialogue_3DText Answer = LocalDialog.GetComponent<Dialogue_3DText>();
            if (Answer == null)
            {
                LoggerInstance.Msg("Dialogue_3DText component not found.");
                return;
            }

            Answer.speaker = Mita.gameObject;
            Answer.textPrint = response;
            Answer.indexString = -1;
            Answer.themeDialogue = Dialogue_3DText.Dialogue3DTheme.Mita;

            if (string.IsNullOrEmpty(emotion))
            {
                LoggerInstance.Msg("Error: emotion string is null or empty.");
            }
            else
            {
                try
                {
                    Enum.TryParse<EmotionType>(emotion, true, out var result);
                    Answer.emotionStart = result;
                }
                catch (Exception)
                {
                    Answer.emotionStart = GetRandomEmotion();
                    LoggerInstance.Msg($"Error: invalid emotion '{emotion}'");
                    throw;
                }

            }



            LocalDialog.SetActive(true);

            LoggerInstance.Msg("Dialog end");
        }

        public static List<string> ExtractCommands(ref string response)
        {
            List<string> commands = new List<string>();
            // Регулярное выражение для поиска текста между <c></c>
            string pattern = @"<c>(.*?)</c>";

            // Используем регулярное выражение для поиска всех команд
            MatchCollection matches = Regex.Matches(response, pattern);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    commands.Add(match.Groups[1].Value); // Добавляем команду без тега
                }
            }

            // Удаляем все теги <c></c> из строки
            response = Regex.Replace(response, pattern, "");

            return commands;
        }

        // Функция для извлечения анимаций из <a></a> и удаления тегов
        public static List<string> ExtractEmotions(ref string response)
        {
            List<string> emotions = new List<string>();
            // Регулярное выражение для поиска текста между <a></a>
            string pattern = @"<e>(.*?)</e>";

            // Используем регулярное выражение для поиска всех анимаций
            MatchCollection matches = Regex.Matches(response, pattern);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    emotions.Add(match.Groups[1].Value); // Добавляем анимацию без тега
                }
            }

            // Удаляем все теги <a></a> из строки
            response = Regex.Replace(response, pattern, "");

            return emotions;
        }


        private EmotionType GetRandomEmotion()
        {
            // Получаем все значения из перечисления EmotionType
            EmotionType[] emotions = (EmotionType[])Enum.GetValues(typeof(EmotionType));

            // Генерируем случайный индекс
            int randomIndex = UnityEngine.Random.Range(0, emotions.Length);

            // Возвращаем случайное значение
            return emotions[randomIndex];
        }
        public async Task<string> GetResponseFromPythonSocketAsync(string input)
        {
            string serverAddress = "127.0.0.1";
            int port = 12345;

            // Создаём и подключаем сокет
            using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                await Task.Factory.FromAsync(
                    clientSocket.BeginConnect(serverAddress, port, null, null),
                    clientSocket.EndConnect
                );

                // Отправляем сообщение на сервер
                byte[] messageBytes = Encoding.UTF8.GetBytes(input);
                await clientSocket.SendAsync(messageBytes, SocketFlags.None);

                // Получаем ответ от сервера
                byte[] buffer = new byte[1024];
                int bytesRead = await clientSocket.ReceiveAsync(buffer, SocketFlags.None);

                // Декодируем ответ
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                timer = 0f;
                return response;
            }
        }

    }


}