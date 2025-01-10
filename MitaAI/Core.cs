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
using UnityEngine.Animations;
using UnityEngine.UIElements;

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

        private Queue<string> commandQueue = new Queue<string>(); // Очередь для команд
        private bool isProcessingCommands = false; // Флаг, чтобы не запускать обработку повторно

        private const float Interval = 0.66f;
        private float timer = 0f;

        private const float MitaBoringInterval = 30f;
        private float MitaBoringtimer = 0f;

        
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "SceneAihasto")
            {
                //SceneManager.LoadScene(4);
            }

            else if (sceneName == "Scene 4 - StartSecret")
            {
                InitializeGameObjects();
            }
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
            MitaBoringtimer += Time.fixedDeltaTime;

            if (timer >= Interval)
            {
                timer = 0f;
                await HandleDialogueAsync();
            }

            base.OnFixedUpdate();
        }

        private async Task HandleDialogueAsync()
        {
            
            string dataToSent = "waiting";
            if (MitaBoringtimer >= MitaBoringInterval)
            {
                MitaBoringtimer = 0f;
                dataToSent = "boring";
            }
            string response = "";
            try
            {
                 response = await GetResponseFromPythonSocketAsync(dataToSent);
                if (response != "")
                {
                    await DisplayResponseAndEmotionAsync(response);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Msg($"Error in HandleDialogueAsync: {ex.Message}");
            }


            if (response != "")
            {
                await DisplayResponseAndEmotionAsync(response);
            }
            
        }

        private async Task DisplayResponseAndEmotionAsync(string response)
        {
            GameObject currentDialog = null;

            try
            {
                // Изменение response в зависимости от эмоций
                string modifiedResponse = await SetEmotionBasedOnResponse(response);

                // Извлечение команд и их обработка
                (List<string> commands, modifiedResponse) = await ExtractCommandsAsync(modifiedResponse);
                foreach (string command in commands)
                {
                    commandQueue.Enqueue(command); // Добавляем команды в очередь
                }

                if (!isProcessingCommands)
                {
                    _ = ProcessCommandQueueAsync(); // Запуск обработки очереди команд
                }

                // Разбиваем текст на части
                List<string> dialogueParts = SplitText(modifiedResponse, maxLength: 50);

                // Перебираем каждую часть и отображаем
                foreach (string part in dialogueParts)
                {
                    int delay = 3500 * part.Length / 50;

                    if (currentDialog != null)
                    {
                        GameObject.Destroy(currentDialog);
                        currentDialog = null;
                    }

                    currentDialog = InstantiateDialog();
                    Dialogue_3DText answer = currentDialog.GetComponent<Dialogue_3DText>();
                    if (answer == null)
                    {
                        throw new Exception("Dialogue_3DText component not found.");
                    }

                    answer.speaker = Mita.gameObject;
                    answer.textPrint = part;
                    answer.indexString = -1;
                    answer.themeDialogue = Dialogue_3DText.Dialogue3DTheme.Mita;
                    answer.timeShow = delay;

                    currentDialog.SetActive(true);

                    MitaBoringtimer = 0f;
                    await Task.Delay(delay);
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Msg($"Error in DisplayResponseAndEmotionAsync: {ex.Message}");
            }
            finally
            {
                if (currentDialog != null)
                {
                    GameObject.Destroy(currentDialog);
                }
            }
        }

        private async Task ProcessCommandQueueAsync()
        {
            isProcessingCommands = true;

            try
            {
                while (commandQueue.Count > 0)
                {
                    string command = commandQueue.Dequeue();

                    try
                    {
                        switch (command.ToLower())
                        {
                            case "подойти к игроку":
                                Mita.AiWalkToTarget(Player.transform);
                                break;

                            case "телепортироваться к игроку":
                                Mita.MitaTeleport(Player.transform);
                                break;

                            default:
                                LoggerInstance.Msg($"Unknown command: {command}");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Msg($"Error processing command '{command}': {ex.Message}");
                    }

                    await Task.Delay(500); // Задержка между выполнением команд
                }
            }
            finally
            {
                isProcessingCommands = false;
            }
        }

        private List<string> SplitText(string text, int maxLength)
        {
            List<string> parts = new List<string>();

            // Разделяем по строкам
            string[] lines = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                // Если длина строки меньше maxLength, добавляем её сразу
                if (line.Length <= maxLength)
                {
                    parts.Add(line.Trim());
                }
                else
                {
                    // Разделяем на предложения
                    string[] sentences = line.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                    string currentPart = "";

                    foreach (string sentence in sentences)
                    {
                        string trimmedSentence = sentence.Trim();
                        if ((currentPart.Length + trimmedSentence.Length + 1) <= maxLength)
                        {
                            currentPart += (currentPart.Length > 0 ? " " : "") + trimmedSentence + ".";
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(currentPart)) parts.Add(currentPart.Trim());
                            currentPart = trimmedSentence + ".";
                        }
                    }

                    // Добавляем оставшийся текст
                    if (!string.IsNullOrWhiteSpace(currentPart)) parts.Add(currentPart.Trim());
                }
            }

            return parts;
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
                    
                }

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

                return (emotions, cleanedResponse);
            });
        }

        public static async Task<(List<string> commands, string cleanedResponse)> ExtractCommandsAsync(string response)
        {
            return await Task.Run(() =>
            {
                List<string> commands = new List<string>();
                string pattern = @"<c>(.*?)</c>";
                MatchCollection matches = Regex.Matches(response, pattern);

                foreach (Match match in matches)
                {
                    if (match.Success)
                    {
                        commands.Add(match.Groups[1].Value);
                    }
                }

                // Удаляем теги эмоций из текста
                string cleanedResponse = Regex.Replace(response, @"<c>.*?</c>", "");

                return (commands, cleanedResponse);
            });
        }

      

        private EmotionType GetRandomEmotion()
        {
            EmotionType[] emotions = (EmotionType[])Enum.GetValues(typeof(EmotionType));
            int randomIndex = UnityEngine.Random.Range(0, emotions.Length);
            return emotions[randomIndex];
        }

        public async Task<string> GetResponseFromPythonSocketAsync(string input)
        {
            using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                bool connected = await TryConnectAsync(clientSocket, ServerAddress, Port);
                if (!connected)
                {
                    return string.Empty; // Возвращаем пустой ответ, если не удалось подключиться
                }

                byte[] messageBytes = Encoding.UTF8.GetBytes(input);
                await clientSocket.SendAsync(messageBytes, SocketFlags.None);

                byte[] buffer = new byte[1024];
                try
                {
                    int bytesRead = await clientSocket.ReceiveAsync(buffer, SocketFlags.None);
                    return Encoding.UTF8.GetString(buffer, 0, bytesRead);
                }
                catch (Exception ex)
                {
                    LoggerInstance.Msg($"Error receiving data: {ex.Message}");
                    return string.Empty; // Возвращаем пустой ответ в случае ошибки при получении данных
                }
            }
        }

        private async Task<bool> TryConnectAsync(Socket socket, string address, int port, int maxRetries = 3, int delayBetweenRetries = 2000)
        {
            int attempt = 0;
            while (attempt < maxRetries)
            {
                try
                {
                    await Task.Factory.FromAsync(socket.BeginConnect(address, port, null, null), socket.EndConnect);
                    return true; // Если подключение прошло успешно
                }
                catch (SocketException ex)
                {
                    attempt++;
                    if (attempt >= maxRetries)
                    {
                        LoggerInstance.Msg($"Max retries reached. Unable to connect to {address}:{port}. ex: {ex}");
                        return false; // После превышения количества попыток
                    }
                    await Task.Delay(delayBetweenRetries); // Задержка перед повторной попыткой
                }
                catch (Exception ex)
                {
                    LoggerInstance.Msg($"Unexpected error connecting to {address}:{port}: {ex.Message}");
                    return false; // Если произошла неожиданная ошибка
                }
            }
            return false; // В случае завершения цикла
        }


    }


}
