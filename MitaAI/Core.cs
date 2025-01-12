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
using Harmony;
using System.Diagnostics.Tracing;
using UnityEngine.Events;
using System.Reflection;
using static MelonLoader.InteropSupport;
using Microsoft.VisualBasic;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.Playables;
[assembly: MelonInfo(typeof(MitaAI.MitaCore), "MitaAI", "1.0.0", "Dmitry", null)]
[assembly: MelonGame("AIHASTO", "MiSideFull")]

namespace MitaAI
{
    public class MitaCore : MelonMod
    {
        private MitaPerson Mita;
        private PlayerPerson Player;
        float distance = 0f;

        Location34_Communication Location34_Communication;
        private GameObject CustomDialog;

        private const string ServerAddress = "127.0.0.1";
        private const int Port = 12345;
        private const int PassivePort = 12346;


        private const float Interval = 0.66f;
        private float timer = 0f;

        private const float MitaBoringInterval = 60f;
        private float MitaBoringtimer = 0f;

        string playerMessage = "";
        string patch_to_sound_file = "";
        List<string> RequiredSceneNames = new List<string> { "Scene 4 - StartSecret", "Scene 3 - WeTogether" };
        string CurrentSceneName;

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)

        {
            CurrentSceneName = sceneName;
            if (CurrentSceneName == "AiHasto")
            {
                //GameObject GameObject = GameObject.Find("MenuGame")>
            }

            else if (CurrentSceneName == "SceneMenu")
            {
                Menu MainMenu = GameObject.Find("MenuGame").GetComponent<Menu>();
                MainMenu.Alternative();
                MainMenu.ButtonLoadScene("Scene 4 - StartSecret");
                //MainMenu.ButtonLoadScene("Scene 3 - WeTogether");

            }

            else if (RequiredSceneNames.Contains(CurrentSceneName) )
            {
                try
                {
                    GameObject Trigger = GameObject.Find("World/Quests/Quest 1/Trigger Entry Kitchen");
                    Location34_Communication = GameObject.Find("World/Quests/Quest 1/Addon").GetComponent<Location34_Communication>();
                    Location34_Communication.indexSwitchAnimation = 1;
                    Trigger.SetActive(false);
                }
                catch
                {
                    
                }
                InitializeGameObjects();
            }
        }

        private void InitializeGameObjects()
        {
            Mita = GameObject.Find("Mita")?.GetComponent<MitaPerson>();

            try {
                //AnimationClip animation = AnimationClip.;
            }
            catch (Exception)
            {

            }
            LoggerInstance.Msg("Animation 'Mita Idle' not found.");


            //Mita.gameObject.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = true;

            Player = GameObject.Find("Person")?.GetComponent<PlayerPerson>();
            LoggerInstance.Msg(Mita != null ? "Mita found!" : "Mita not found.");
            LoggerInstance.Msg(Player != null ? "Player found!" : "Player not found.");

            if (Mita == null || Player == null) return;

            Transform world = GameObject.Find("World")?.transform;
            if (world == null)
            {
                LoggerInstance.Msg("World object not found.");

            }


            Transform dialogOriginal = world.Find("Quests/Quest 1/Dialogues/Dialogue Mita/Dialogue Finish Aihastion/DMita 2");

            if (dialogOriginal == null)
            {
                LoggerInstance.Msg("Target object 'DMita 2' not found.");
                dialogOriginal = world.Find("Quests/Quest 1 Start/3D Text 5");

                if (dialogOriginal == null)
                {
                    
                    LoggerInstance.Msg("Target object '3D Text 5' not found.");
                }

            }

            CustomDialog = GameObject.Instantiate(dialogOriginal.gameObject, world.Find("Quests/Quest 1/Dialogues"));
            CustomDialog.name = "Custom Dialogue";
            Dialogue_3DText CustomDialogText = CustomDialog.GetComponent<Dialogue_3DText>();
            
            CustomDialogText.nextText = null;
            CustomDialogText.sizeHeight = 0.0687f;
            CustomDialogText.sizeSymbol = 0.0014f;
            CustomDialogText.sizeWidth =  0.75f;
            CustomDialogText.xPrint = 0.413f;
            CustomDialogText.indexString = -1;


        }

        public override async void OnFixedUpdate()
        {
            if ( !RequiredSceneNames.Contains(CurrentSceneName) ) return;

            timer += Time.fixedDeltaTime;
            MitaBoringtimer += Time.fixedDeltaTime;

            if (timer >= Interval)
            {
                timer = 0f;
                await HandleDialogueAsync();
            }

            base.OnFixedUpdate();
        }

        float getDistance()
        {
            return Vector3.Distance(Mita.transform.position, Player.transform.position);
        }

        private async Task HandleDialogueAsync()
        {
            
            string dataToSent = "waiting";
            if (playerMessage != "")
            {
                MitaBoringtimer = 0f;
                dataToSent = playerMessage;
                playerMessage = "";
            }
            else if (MitaBoringtimer >= MitaBoringInterval)
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
            
        }

        // Список для хранения созданных диалогов
        private List<GameObject> dialogPool = new List<GameObject>();

        private GameObject GetOrCreateDialog()
        {
            // Ищем неактивный диалог в пуле
            foreach (var dialog in dialogPool)
            {
                if (!dialog.activeSelf)
                {
                    ClearChildren(dialog.gameObject);
                    return dialog;
                }
            }

            // Если нет доступного, создаем новый
            GameObject newDialog = InstantiateDialog();
            dialogPool.Add(newDialog);
            return newDialog;

            // Метод для очистки дочерних объектов
            void ClearChildren(GameObject parent)
            {
                foreach (Transform child in parent.transform)
                {
                    GameObject.Destroy(child.gameObject);
                }
            }
        }
        // Метод для очистки дочерних объектов

        private async Task DisplayResponseAndEmotionAsync(string response)
        {
            while (string.IsNullOrEmpty(patch_to_sound_file))
            {
                await Task.Delay(100);
            }

            isInputBlocked = true;
            try
            {
                string modifiedResponse = await SetEmotionBasedOnResponse(response);

                AudioClip audioClip = null;
                if (!string.IsNullOrEmpty(patch_to_sound_file))
                {
                    try
                    {
                        LoggerInstance.Msg("patch_to_sound_file not null");
                        audioClip = await LoadAudioClipFromFileAsync(patch_to_sound_file);
                        patch_to_sound_file = "";
                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Msg($"Error loading audio file: {ex.Message}");
                    }
                }

                List<string> dialogueParts = SplitText(modifiedResponse, maxLength: 50);

                foreach (string part in dialogueParts)
                {
                    try
                    {
                        (List<string> commands, string MofifiedPart) = await ExtractCommandsAsync(part);

                        if (commands.Count > 0)
                        {
                            ProcessCommands(commands);
                        }

                        int delay = 4000 * MofifiedPart.Length / 50;

                        GameObject currentDialog = GetOrCreateDialog();
                        currentDialog.SetActive(true);

                        Dialogue_3DText answer = currentDialog.GetComponent<Dialogue_3DText>();
                        if (answer == null)
                        {
                            throw new Exception("Dialogue_3DText component not found.");
                        }

                        if (Mita?.gameObject == null)
                        {
                            throw new Exception("Mita or Mita.gameObject is null.");
                        }

                        answer.textPrint = MofifiedPart;
                        answer.themeDialogue = Dialogue_3DText.Dialogue3DTheme.Mita;
                        answer.timeShow = delay;
                        answer.speaker = Mita.gameObject;

                        if (audioClip != null)
                        {
                            answer.LoadVoice(audioClip);
                            audioClip = null;
                        }

                        MitaBoringtimer = 0f;
                        await Task.Delay(delay);

                        currentDialog.SetActive(false);
                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Msg($"Error processing part of response: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Msg($"Error in DisplayResponseAndEmotionAsync: {ex.Message}");
            }
            finally
            {
                isInputBlocked = false;
            }
        }

        private void PlayerTalk(string text)
        {
            GameObject currentDialog = null;

            try
            {
              
                int delay = 4000 * text.Length / 50;

                currentDialog = InstantiateDialog();
                if (currentDialog != null)
                {
                    Dialogue_3DText answer = currentDialog.GetComponent<Dialogue_3DText>();
                    if (answer == null)
                    {
                        throw new Exception("Dialogue_3DText component not found.");
                    }

                    answer.speaker = Player.gameObject;
                    answer.textPrint = text;
                    answer.themeDialogue = Dialogue_3DText.Dialogue3DTheme.Player;
                    answer.timeShow = delay;
                    
                    currentDialog.SetActive(true);

                    MitaBoringtimer = 0f;
                }
                else
                {
                    throw new Exception("currentDialog is null.");
                }
                
            }
            catch (Exception ex)
            {
                LoggerInstance.Msg($"PlayerTalk: {ex.Message}");
            }
            finally
            {
                if (currentDialog != null)
                {

                }
            }
        }

        private void ProcessCommands(List<string> commands)
        {
            try
            {
                foreach (string command in commands)
                {
                    try
                    {
                        switch (command.ToLower())
                        {
                            case "подойти к игроку":
                                Mita.AiWalkToTarget(Player.transform);
                                Location34_Communication.indexSwitchAnimation = 1;
                                break;

                            case "телепортироваться к игроку":
                                Mita.MitaTeleport(Player.transform);
                                Location34_Communication.indexSwitchAnimation = 1;
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

                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Msg($"Error in ProcessCommandsAsync: {ex.Message}");
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
                LoggerInstance.Msg("World object not found.");
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
                //Mita.FaceEmotionType(GetRandomEmotion());
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
                getDistance();
                input = $"{input}|||{distance}";

                byte[] messageBytes = Encoding.UTF8.GetBytes(input);
                await clientSocket.SendAsync(messageBytes, SocketFlags.None);

                byte[] buffer = new byte[2500];
                try
                {
                    int bytesRead = await clientSocket.ReceiveAsync(buffer, SocketFlags.None);

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    string[] parts = receivedMessage.Split(new string[] { "|||" }, StringSplitOptions.None);

                    //LoggerInstance.Msg($"Got from piton  {receivedMessage}");
                    string response = parts[0];

                    
                    patch_to_sound_file = parts[1];
                    //LoggerInstance.Msg($"patch_to_sound_file from piton  {patch_to_sound_file}");
                    return response;
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
                        //LoggerInstance.Msg($"Max retries reached. Unable to connect to {address}:{port}.");
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


        // Ввод

        private bool isInputBlocked = false; // Флаг для блокировки
        private static GameObject InputFieldComponent;

        public override void OnUpdate()
        {
           

            // Обрабатываем нажатие Tab для переключения InputField
            if (Input.GetKeyDown(KeyCode.Tab)) // Используем GetKeyDown для одноразового срабатывания
            {
                if (InputFieldComponent == null)
                {
                    try
                    {
                        CreateInputComponent();
                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Msg("CreateInputComponent ex:" + ex);
                    }
                }
                else
                {

                    if (isInputBlocked) return;

                    // Переключаем видимость InputField
                    bool isActive = InputFieldComponent.activeSelf;
                    InputFieldComponent.SetActive(!isActive);

                    // Если объект стал активным, активируем InputField
                    if (InputFieldComponent.activeSelf)
                    {
                        var ifc = InputFieldComponent.GetComponent<InputField>();
                        if (ifc != null)
                        {
                            ifc.Select();
                            ifc.ActivateInputField();
                        }
                    }
                }
            }

            // Обрабатываем нажатие Enter для передачи текста в функцию
            else if (Input.GetKeyDown(KeyCode.Return) && InputFieldComponent != null)
            {
                var ifc = InputFieldComponent.GetComponent<InputField>();
                if (ifc.text != "")
                {
                    ProcessInput(ifc.text); // Пустышка для обработки текста
                    ifc.text = "";
                }
            }

        }
        private async void BlockInputFieldAsync(float t)
        {
            isInputBlocked = true; // Устанавливаем блокировку
            InputFieldComponent?.SetActive(false); // Отключаем поле ввода, если оно активно

            await Task.Delay((int)(t)); // Ждём указанное время (в миллисекундах)

            InputFieldComponent.SetActive(true); // Отключаем поле ввода, если оно активно
            isInputBlocked = false; // Снимаем блокировку
        }

        private void CreateInputComponent()
        {
            LoggerInstance.Msg("1");

            // Создаем объект InputField
            InputFieldComponent = new GameObject("InputFieldComponent");

            var ifc = InputFieldComponent.AddComponent<InputField>();
            var _interface = GameObject.Find("Interface");
            if (_interface == null) return;

            InputFieldComponent.transform.parent = _interface.transform;
            LoggerInstance.Msg("2");

            var rect = InputFieldComponent.AddComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;

            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);

            LoggerInstance.Msg("3");

            var image = InputFieldComponent.AddComponent<UnityEngine.UI.Image>();
/*            try
            {
                var KeyRun = _interface.transform.Find("GameController/Interface/SubtitlesFrame/Text 2").GetComponent<UnityEngine.UI.Image>();
                LoggerInstance.Msg("KeyRun");
                image.sprite = KeyRun.sprite;
            }
            catch (Exception ex)
            {*/
                Sprite blackSprite = CreateBlackSprite(100, 100);
                image.sprite = blackSprite;
            //}

            image.color = new Color(0f, 0f, 0f,0.7f);
            ifc.image = image;

            LoggerInstance.Msg("4");

            var TextLegacy = new GameObject("TextLegacy");
            var textComponent = TextLegacy.AddComponent<Text>();
            TextLegacy.transform.parent = InputFieldComponent.transform;
            var rectText = TextLegacy.GetComponent<RectTransform>();
            rectText.sizeDelta = new Vector2(500, 100);
            rectText.anchoredPosition = Vector2.zero;
            var texts = GameObject.FindObjectsOfType<Text>();

            LoggerInstance.Msg("5");

            foreach (var text in texts)
            {
                textComponent.font = text.font;
                textComponent.fontStyle = text.fontStyle;
                textComponent.fontSize = 35;
                if (textComponent.font != null) break;
            }

            
            LoggerInstance.Msg("6");

            var textInputField = InputFieldComponent.GetComponent<InputField>();
            textInputField.textComponent = TextLegacy.GetComponent<Text>();
            textInputField.text = "Введи текст";
            textInputField.textComponent.color = Color.yellow;
            textInputField.textComponent.alignment = TextAnchor.MiddleCenter;

            LoggerInstance.Msg("7");

            // Устанавливаем 70% ширины от родителя
            RectTransform parentRect = _interface.GetComponent<RectTransform>();
            float parentWidth = parentRect.rect.width;
            rect.sizeDelta = new Vector2(parentWidth * 0.7f, rect.sizeDelta.y);
            rectText.sizeDelta = rect.sizeDelta;
            textInputField.Select();
            textInputField.ActivateInputField();

            LoggerInstance.Msg("8");
        }


        // Пустышка для обработки ввода
        private void ProcessInput(string inputText)
        {
            LoggerInstance.Msg("Input received: " + inputText);
            PlayerTalk(inputText);
            playerMessage = inputText;

        }
        public Sprite CreateBlackSprite(int width, int height)
        {
            // Создаем текстуру с заданными размерами
            Texture2D texture = new Texture2D(width, height);

            // Задаем все пиксели как черные
            Color darkColor = new Color(0f, 0f, 0f,0f);  // Черный цвет
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    texture.SetPixel(x, y, darkColor);
                }
            }

            // Применяем изменения
            texture.Apply();

            // Создаем и возвращаем спрайт из текстуры
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }
        private async Task<AudioClip> LoadAudioClipFromFileAsync(string filePath)
        {
            try
            {
                MelonLoader.MelonLogger.Msg("Loading audio file: " + filePath);

                // Читаем все байты файла
                byte[] fileData = File.ReadAllBytes(filePath);

                // Конвертируем байты в массив float
                float[] audioData = ConvertByteArrayToFloatArray(fileData);

                // Применяем нормализацию
                audioData = NormalizeAudioData(audioData);

                // Применяем fade-in (0.1 секунды на 44100 Hz)
                audioData = ApplyFadeIn(audioData, 44100 / 10);

                // Создаем аудиоклип
                AudioClip audioClip = AudioClip.Create("LoadedAudio", audioData.Length, 2, 44100, false);
                audioClip.SetData(audioData, 0);

                MelonLoader.MelonLogger.Msg("Audio returned");
                return audioClip;
            }
            catch (Exception ex)
            {
                MelonLoader.MelonLogger.Error("Error loading audio: " + ex.Message);
                return null;
            }
        }

        // Нормализация аудиоданных
        private float[] NormalizeAudioData(float[] audioData)
        {
            float maxAmplitude = 0f;
            foreach (var sample in audioData)
            {
                if (Math.Abs(sample) > maxAmplitude)
                {
                    maxAmplitude = Math.Abs(sample);
                }
            }
            if (maxAmplitude > 0)
            {
                for (int i = 0; i < audioData.Length; i++)
                {
                    audioData[i] /= maxAmplitude;
                }
            }
            return audioData;
        }

        // Применение fade-in
        private float[] ApplyFadeIn(float[] audioData, int fadeSamples)
        {
            for (int i = 0; i < fadeSamples && i < audioData.Length; i++)
            {
                float fadeFactor = (float)i / fadeSamples;
                audioData[i] *= fadeFactor;
            }
            return audioData;
        }

        // Преобразование байтов в массив float
        private float[] ConvertByteArrayToFloatArray(byte[] byteArray)
        {
            float[] floatArray = new float[byteArray.Length / 2]; // предполагаем, что файл в 16-битах
            for (int i = 0; i < floatArray.Length; i++)
            {
                short sample = BitConverter.ToInt16(byteArray, i * 2);
                floatArray[i] = sample / 32768f; // Нормализация в диапазон [-1, 1]
            }
            return floatArray;
        }




    }


}
