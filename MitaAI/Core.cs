using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System;
using UnityEngine.UI;
using static Il2CppRootMotion.FinalIK.InteractionObject;

[assembly: MelonInfo(typeof(MitaAI.Core), "MitaAI", "1.0.0", "Dmitry", null)]
[assembly: MelonGame("AIHASTO", "MiSideFull")]

namespace MitaAI
{
    public class Core : MelonMod
    {
        private MitaPerson Mita;
        private PlayerPerson Player;
        private GameObject CustomDialog;
        private const string ServerAddress = "127.0.0.1";
        private const int Port = 12345;
        private const int PassivePort = 12346;
        private const float Interval = 15f;
        private float timer = 0f;

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Scene 4 - StartSecret")
            {
                InitializeGameObjects();
                StartListeningForMessages();
                LoggerInstance.Msg("Scene loaded successfully.");
            }
        }
        private async void StartListeningForMessages()
        {
            LoggerInstance.Msg("StartListeningForMessages");
            // Вызываем асинхронный метод для слушания сообщений от сервера
            await ListenForMessagesFromServerAsync(ProcessMessage);
        }
        private void InitializeGameObjects()
        {
            Mita = GameObject.Find("Mita")?.GetComponent<MitaPerson>();
            Player = GameObject.Find("Person")?.GetComponent<PlayerPerson>();
            LoggerInstance.Msg(Mita != null ? "Mita found!" : "Mita not found.");
            LoggerInstance.Msg(Player != null ? "Player found!" : "Player not found.");

            if (Mita == null || Player == null) return;

            Transform world = GameObject.Find("World")?.transform;
            if (world == null)
            {
                Debug.LogError("World object not found.");
                return;
            }

            Transform dialogOriginal = world.Find("Quests/Quest 1/Dialogues/Dialogue Mita/Dialogue Finish Aihastion/DMita 2");
            if (dialogOriginal == null)
            {
                Debug.LogError("Target object 'DMita 2' not found.");
                return;
            }

            CustomDialog = GameObject.Instantiate(dialogOriginal.gameObject, world.Find("Quests/Quest 1/Dialogues"));
            CustomDialog.name = "Custom Dialoge";
        }

        public override async void OnFixedUpdate()
        {
            if (SceneManager.GetActiveScene().name != "Scene 4 - StartSecret") return;

            timer += Time.fixedDeltaTime;

            if (timer >= Interval)
            {
                timer = 0f;
                await HandleDialogueAsync();
            }

            base.OnFixedUpdate();
        }

        private async Task HandleDialogueAsync()
        {
            string response = await GetResponseFromPythonSocketAsync("Привет, Мита! Можешь какую-нибудь случайную эмоцию сделать");
            if (response != "")
            {
                await DisplayResponseAndEmotionAsync(response);
            }
            
        }

        private async Task DisplayResponseAndEmotionAsync(string response)
        {
            try
            {
                // Сначала изменим response в зависимости от эмоций
                string modifiedResponse = await SetEmotionBasedOnResponse(response);

                // Теперь создаем и отображаем диалог с уже измененным ответом
                GameObject localDialog = InstantiateDialog();
                Dialogue_3DText answer = localDialog.GetComponent<Dialogue_3DText>();
                if (answer == null) throw new Exception("Dialogue_3DText component not found.");

                answer.speaker = Mita.gameObject;
                answer.textPrint = modifiedResponse;  // Используем измененный response
                answer.indexString = -1;
                answer.themeDialogue = Dialogue_3DText.Dialogue3DTheme.Mita;
                localDialog.SetActive(true);
            }
            catch (Exception ex)
            {
                LoggerInstance.Msg($"Error displaying response: {ex.Message}");
            }
        }


        private GameObject InstantiateDialog()
        {
            Transform world = GameObject.Find("World")?.transform;
            if (world == null)
            {
                Debug.LogError("World object not found.");
                return null;
            }

            return GameObject.Instantiate(CustomDialog, world.Find("Quests/Quest 1/Dialogues"));
        }

        private async Task<string> SetEmotionBasedOnResponse(string response)
        {
            try
            {
                var (emotions, cleanedResponse) = await ExtractEmotionsAsync(response);

                if (emotions.Count > 0)
                {
                    Enum.TryParse<EmotionType>(emotions[0], true, out var result);
                    Mita.FaceEmotionType(result);
                    LoggerInstance.Msg("Emotion extracted successfully.");
                }
                else
                {
                    LoggerInstance.Msg("No emotions found. Using random emotion.");
                    Mita.FaceEmotionType(GetRandomEmotion());
                }

                // Возвращаем очищенный текст (без эмоций и служебных сообщений)
                return cleanedResponse;
            }
            catch (Exception ex)
            {
                LoggerInstance.Msg($"Error extracting emotion: {ex.Message}");
                Mita.FaceEmotionType(GetRandomEmotion());
                return response; // Если произошла ошибка, возвращаем исходный текст
            }
        }



        public static async Task<(List<string> emotions, string cleanedResponse)> ExtractEmotionsAsync(string response)
        {
            return await Task.Run(() =>
            {
                List<string> emotions = new List<string>();
                string pattern = @"<e>(.*?)</e>";
                MatchCollection matches = Regex.Matches(response, pattern);

                foreach (Match match in matches)
                {
                    if (match.Success)
                    {
                        emotions.Add(match.Groups[1].Value);
                    }
                }

                // Удаляем теги эмоций из текста
                string cleanedResponse = Regex.Replace(response, @"<e>.*?</e>", "");

                // Также удаляем все служебные сообщения
                cleanedResponse = Regex.Replace(cleanedResponse, @"<service>.*?</service>", "");

                return (emotions, cleanedResponse);
            });
        }


        private EmotionType GetRandomEmotion()
        {
            EmotionType[] emotions = (EmotionType[])Enum.GetValues(typeof(EmotionType));
            int randomIndex = UnityEngine.Random.Range(0, emotions.Length);
            return emotions[randomIndex];
        }

        public async Task ListenForMessagesFromServerAsync(Action<string> onMessageReceived)
        {
            LoggerInstance.Msg("Begin ListenForMessagesFromServerAsync");
            using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                bool connected = await TryConnectAsync(clientSocket, ServerAddress, PassivePort,1000,3000);
                if (!connected)
                {
                    LoggerInstance.Msg("Failed to connect to the server. Exiting message listening.");
                    return; // Если не удалось подключиться, выход из метода
                }

                byte[] buffer = new byte[1024];
                LoggerInstance.Msg("Middle ListenForMessagesFromServerAsync");
                try
                {
                    while (true)
                    {
                        LoggerInstance.Msg("Circle ListenForMessagesFromServerAsync");
                        int bytesRead = await clientSocket.ReceiveAsync(buffer, SocketFlags.None);
                        if (bytesRead == 0) continue;

                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        LoggerInstance.Msg("Circle 2 ListenForMessagesFromServerAsync");
                        if (message != "")
                        {
                            LoggerInstance.Msg("Try to invoke");
                            onMessageReceived?.Invoke(message);
                        }
                        
                    }
                }
                catch (Exception ex)
                {
                    LoggerInstance.Msg($"Error while receiving messages: {ex.Message}");
                }
            }
        }

        public async Task<string> GetResponseFromPythonSocketAsync(string input)
        {
            using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                bool connected = await TryConnectAsync(clientSocket, ServerAddress, Port);
                if (!connected)
                {
                    LoggerInstance.Msg("Failed to connect to the server. Returning empty response.");
                    return string.Empty; // Возвращаем пустой ответ, если не удалось подключиться
                }

                byte[] messageBytes = Encoding.UTF8.GetBytes(input);
                await clientSocket.SendAsync(messageBytes, SocketFlags.None);

                byte[] buffer = new byte[1024];
                int bytesRead = await clientSocket.ReceiveAsync(buffer, SocketFlags.None);
                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }
        }

        private async Task<bool> TryConnectAsync(Socket socket, string address, int port, int maxRetries = 3, int delayBetweenRetries = 2000)
        {
            LoggerInstance.Msg($"{port} Attemp");
            int attempt = 0;
            while (attempt < maxRetries)
            {
                try
                {
                    await Task.Factory.FromAsync(socket.BeginConnect(address, port, null, null), socket.EndConnect);
                    LoggerInstance.Msg($"Successfully connected to the server. {port}");
                    return true; // Если подключение прошло успешно
                }
                catch (SocketException ex)
                {
                    attempt++;
                    LoggerInstance.Msg($"Connection failed port {port} (attempt {attempt}/{maxRetries}): {ex.Message}");
                    if (attempt >= maxRetries)
                    {
                        LoggerInstance.Msg("Max retries reached. Unable to connect.");
                        return false; // Если все попытки не увенчались успехом
                    }
                    await Task.Delay(delayBetweenRetries); // Задержка перед повторной попыткой
                }
                catch (Exception ex)
                {
                    LoggerInstance.Msg($"Unexpected error {port}: {ex.Message}");
                    LoggerInstance.Msg($"Unexpected error {port}: {ex.ToString()}");
                    return false; // Если произошла неожиданная ошибка, выходим
                }
            }
            return false; // В случае завершения цикла
        }

        async void ProcessMessage(string message)
        {
            if (message != "")
            {
                LoggerInstance.Msg($"Received message: {message}");
                await DisplayResponseAndEmotionAsync(message);

                // Отправка подтверждения серверу
                // await SendAcknowledgementToServerAsync("Data is transmitted");

                // Additional message processing logic
            }
        }

        private async Task SendAcknowledgementToServerAsync(string ackMessage)
        {
            using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                bool connected = await TryConnectAsync(clientSocket, ServerAddress, Port);
                if (!connected)
                {
                    LoggerInstance.Msg("Failed to connect to the server. Acknowledgement not sent.");
                    return; // Если не удалось подключиться, выходим из метода
                }

                byte[] messageBytes = Encoding.UTF8.GetBytes(ackMessage);
                await clientSocket.SendAsync(messageBytes, SocketFlags.None);

                LoggerInstance.Msg("Acknowledgement sent to the server.");
            }
        }


    }
}
