using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Net.Sockets;
using System.Text;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using Unity.Collections;

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
        private const float interval = 10; // Интервал в секундах (8 секунд)

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
                     response = await Task.Run(() => GetResponseFromPythonSocketAsync("Привет, Мита!"));
                }
                catch (Exception ex)
                {
                    LoggerInstance.Msg("Problem with getting anwer");
                    LoggerInstance.Msg($"Error: {ex.Message}");
                    LoggerInstance.Msg($"StackTrace: {ex.StackTrace}");
                }
                // Отправка асинхронного запроса в отдельном потоке
                

                // Убедитесь, что строка декодируется с использованием UTF-8
                //string decodedResponse = Encoding.UTF8.GetString(Encoding.Default.GetBytes(response));
                try
                {


                    // Создаем копию объекта
                    GameObject LocalDialog = GameObject.Instantiate(CustomDialog,CustomDialog.transform.parent);

                    // Получаем компонент Dialogue_3DText
                    Dialogue_3DText Answer = LocalDialog.GetComponent<Dialogue_3DText>();
                    if (Answer == null)
                    {
                        Debug.LogWarning("Компонент Dialogue_3DText не найден в объекте 'DMita 1'.");
                    }

                    LoggerInstance.Msg("Initializing speaker property.");
                    Answer.speaker = Mita.gameObject;

                    // Uncomment this line if you want to use a specific facial emotion.
                    //LoggerInstance.Msg("Setting facial emotion.");
                    //Mita.FaceEmotion("smilestrange");

                    LoggerInstance.Msg("Setting text for the response.");
                   // Answer.textPrint = $"{response}";
                    Answer.textPrintNow = $"{response}";
                    Answer.indexString = -1;

                    LocalDialog.active = true;
                    LoggerInstance.Msg("Setting dialogue theme.");
                    Answer.themeDialogue = Dialogue_3DText.Dialogue3DTheme.Mita;

                    LoggerInstance.Msg("Setting starting emotion.");
                    Answer.emotionStart = GetRandomEmotion();



                }
                catch (Exception ex)
                {
                    LoggerInstance.Msg($"Error: {ex.Message}");
                    LoggerInstance.Msg($"StackTrace: {ex.StackTrace}");
                }


            }

            base.OnFixedUpdate();
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

                return response;
            }
        }

    }
}
