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
using System.Linq;
using UnityEngine.Networking.Match;
[assembly: MelonInfo(typeof(MitaAI.MitaCore), "MitaAI", "1.0.0", "Dmitry", null)]
[assembly: MelonGame("AIHASTO", "MiSideFull")]

namespace MitaAI
{
    public class MitaCore : MelonMod
    {
        public MitaPerson Mita;
        public PlayerPerson playerPerson;
        float distance = 0f;

        Location34_Communication Location34_Communication;

        Transform worldHouse;
        Transform worldBasement;
        Menu MainMenu;
        private GameObject CustomDialog;
        private GameObject CustomDialogPlayer;
        
        GameObject AnimationKiller;
        
        private const string ServerAddress = "127.0.0.1";
        private const int Port = 12345;
        private const int PassivePort = 12346;


        private const float Interval = 0.66f;
        private float timer = 0f;



        string playerMessage = "";
        string patch_to_sound_file = "";
        Queue<string> patches_to_sound_file = new Queue<string>();

        List<string> RequiredSceneNames = new List<string> { "Scene 4 - StartSecret", "Scene 3 - WeTogether" };
        string CurrentSceneName;

        private HashSet<string> additiveLoadedScenes = new HashSet<string>();
        private bool AllLoaded = false;


        private readonly object _lockObj = new object();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Синхронизируем доступ ко всем этим операциям


        private const float MitaBoringInterval = 60f;
        private float MitaBoringtimer = 0f;
        public async Task UpdateBoringTimerAsync(float delta)
        {
            await _semaphore.WaitAsync();
            try
            {
                MitaBoringtimer += delta; // Операции с полем
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ResetBoringTimerAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                MitaBoringtimer = 0f; // Операции с полем
            }
            finally
            {
                _semaphore.Release();
            }
        }


        public void AddBasement()
        {
            string sceneToLoad = "Scene 6 - BasementFirst";
            string knifePath = "Quests/Quest2 HideAndSeek/Animation Killer/Mita/MitaPerson Mita/Armature/Hips/Spine/Chest/Right shoulder/Right arm/Right elbow/Right wrist/Right item/Knife";
            string objectPath = "World";

            // Запускаем корутину для ожидания загрузки сцены
            additiveLoadedScenes.Add( sceneToLoad );
            MelonCoroutines.Start(WaitForSceneAndInstantiate(sceneToLoad, objectPath, knifePath));
        }

        private IEnumerator WaitForSceneAndInstantiate(string sceneToLoad, string objectPath, string knifePath)
        {
            // Загружаем сцену
            MelonLogger.Msg($"Loading scene: {sceneToLoad}");
            additiveLoadedScenes.Add( sceneToLoad );
            SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Additive);

            // Ожидание завершения загрузки сцены
            Scene scene;
            do
            {
                scene = SceneManager.GetSceneByName(sceneToLoad);
                yield return null; // Ждем следующий кадр
            } while (!scene.isLoaded);

            MelonLogger.Msg($"Scene {sceneToLoad} loaded.");

            // Находим объект в загруженной сцене
            worldBasement = FindObjectInScene(scene.name, objectPath);
            if (worldBasement == null)
            {
                MelonLogger.Msg("World object not found.");
                yield break; // Прерываем выполнение, если объект не найден
            }

            MelonLogger.Msg($"Object found: {worldBasement.name}");

            // Находим нож
            Transform knifeTransform = worldBasement.Find(knifePath);
            if (knifeTransform == null)
            {
                MelonLogger.Msg("Knife object not found.");
                yield break;
            }

            GameObject knife = knifeTransform.gameObject;

            // Перемещаем нож к Mita
            GameObject.Instantiate(knife, Mita.transform);
            MelonLogger.Msg("Knife instantiated on Mita.");

            InitializeGameObjectsWhenReady();
        }

        public static Transform FindObjectInScene(string sceneName, string objectPath)
        {
            // Получаем сцену по имени
            Scene scene = SceneManager.GetSceneByName(sceneName);

            // Проверяем, загружена ли сцена
            if (!scene.IsValid() || !scene.isLoaded)
            {
                MelonLoader.MelonLogger.Msg($"Scene {sceneName} not loaded or broken");
                return null;
            }

            // Получаем корневые объекты сцены
            var rootObjects = scene.GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                MelonLogger.Msg(rootObject.name);
                if (rootObject.name == "World")
                {
                    return rootObject.transform;
                }
            }

            MelonLoader.MelonLogger.Msg($" {objectPath} not found in {sceneName}.");
            return null;
        }
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)

        {
;           

            LoggerInstance.Msg("Scene loaded "+ sceneName);
            if ( !additiveLoadedScenes.Contains(sceneName) )
            {
                LoggerInstance.Msg("Scene loaded not addictive" + sceneName);
                CurrentSceneName = sceneName;
            }
            else{
                LoggerInstance.Msg("Scene loaded addictive!!! " + sceneName);
                return; }
            

            if (CurrentSceneName == "AiHasto")
            {
                //GameObject GameObject = GameObject.Find("MenuGame")>
            }

            else if (CurrentSceneName == "SceneMenu")
            {
                MainMenu = GameObject.Find("MenuGame").GetComponent<Menu>();
                MainMenu.Alternative();
                MainMenu.ButtonLoadScene("Scene 4 - StartSecret");
                //MainMenu.ButtonLoadScene("Scene 3 - WeTogether");

            }

            else if (RequiredSceneNames.Contains(CurrentSceneName) )
            {
                try
                {
                    GameObject Trigger = GameObject.Find("World/Quests/Quest 1/Trigger Entry Kitchen");
                    GameObject Location34_CommunicationObject = GameObject.Find("World/Quests/Quest 1/Addon");
                    Location34_Communication = GameObject.Find("World/Quests/Quest 1/Addon").GetComponent<Location34_Communication>();
                    Location34_Communication.play = true;
                    Location34_Communication.mitaCanWalk = true;
                    Location34_Communication.indexSwitchAnimation = 1;
                    Trigger.SetActive(false);

                    CollectChildObjects(Location34_CommunicationObject);
                }
                catch
                {
                    
                }
                InitializeGameObjects();
            }
        }
        
        int roomIDPlayer = -1;
        int roomIDMita = -1;
        // Первая функция, которая принимает PlayerMove
        public void CheckRoom(PlayerMove playerMove)
        {
            if (playerMove == null)
            {
                return;
            }

            // Передаем трансформ игрока во вторую функцию
            roomIDPlayer = GetRoomID(playerMove.transform);
        }

        // Вторая функция, которая принимает Transform и возвращает roomID
        public int GetRoomID(Transform playerTransform)
        {
            if (playerTransform == null)
            {
                return -1;
            }

            var posX = playerTransform.position.x;
            var posZ = playerTransform.position.z;
            var posY = playerTransform.position.y;
            if (posY < 0) {
                return 4;
            }
            else
            {
                // Логика определения комнаты
                if (posX > 5.3000002f && posZ >= 0) return 0; // Kitchen
                else if (posX > 5.3000002f && posZ < 0) return 2; // Bedroom
                else if (posX > -4 && posX < 5) return 1; // Main
                else if (posX > -11.0f && posX < -4.3000002f) return 3; // Toilet 
            }
            return -1; // Если не нашли подходящей комнаты
        }
        private void InitializeGameObjects()
        {
            Mita = GameObject.Find("Mita")?.GetComponent<MitaPerson>();




            //Mita.gameObject.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = true;

            playerPerson = GameObject.Find("Person")?.GetComponent<PlayerPerson>();
            LoggerInstance.Msg(Mita != null ? "Mita found!" : "Mita not found.");
            LoggerInstance.Msg(playerPerson != null ? "Player found!" : "Player not found.");

            if (Mita == null || playerPerson == null) return;

            worldHouse = GameObject.Find("World")?.transform;
            World worldSettings = worldHouse.gameObject.GetComponent<World>();
            worldSettings.limitFloor = -200f;
            if (worldHouse == null)
            {
                LoggerInstance.Msg("World object not found.");

            }


            Transform dialogOriginal = worldHouse.Find("Quests/Quest 1/Dialogues/Dialogue Mita/Dialogue Finish Aihastion/DMita 2");

            if (dialogOriginal == null)
            {
                LoggerInstance.Msg("Target object 'DMita 2' not found.");
                dialogOriginal = worldHouse.Find("Quests/Quest 1 Start/3D Text 5");

                if (dialogOriginal == null)
                {

                    LoggerInstance.Msg("Target object '3D Text 5' not found.");
                }

            }

            CustomDialog = GameObject.Instantiate(dialogOriginal.gameObject, worldHouse.Find("Quests/Quest 1/Dialogues"));
            CustomDialog.name = "Custom Dialogue";
            Dialogue_3DText CustomDialogText = CustomDialog.GetComponent<Dialogue_3DText>();

            CustomDialogText.nextText = null;
            CustomDialogText.sizeHeight = 0.0687f;
            CustomDialogText.sizeSymbol = 0.0014f;
            CustomDialogText.sizeWidth = 0.75f;
            CustomDialogText.xPrint = 0.413f;
            CustomDialogText.indexString = -1;

            dialogOriginal = worldHouse.Find("Quests/Quest 1/Dialogues/Dialogue Player/Dialogue Hello/DPlayer 3");
            CustomDialogPlayer = GameObject.Instantiate(dialogOriginal.gameObject, worldHouse.Find("Quests/Quest 1/Dialogues"));
            CustomDialogPlayer.name = "Custom Dialogue Player";

            CustomDialogText = CustomDialogPlayer.GetComponent<Dialogue_3DText>();

            CustomDialogText.nextText = null;
            CustomDialogText.sizeHeight = 0.0687f;
            CustomDialogText.sizeSymbol = 0.0014f;
            CustomDialogText.sizeWidth = 0.75f;
            CustomDialogText.xPrint = 0.413f;
            CustomDialogText.indexString = -1;
            CustomDialogText.showSubtitles = true;




            try
            {
                AddBasement();
            }
            catch { }

            AllLoaded = true;
            

            

        }

        private void InitializeGameObjectsWhenReady()
        {
            LoggerInstance.Msg("Waiting for worldBasement to be initialized...");

            // Ждем, пока worldBasement не станет не null
            //while (worldBasement == null)
            //{
                // Здесь можно поставить небольшую паузу, чтобы избежать блокировки потока
              //  System.Threading.Thread.Sleep(100); // Задержка в 100 мс
            //}

            LoggerInstance.Msg("worldBasement is now initialized. Proceeding with object setup...");

            // Ваши действия после инициализации worldBasement
            try
            {
                // Пробуем найти и безопасно преобразовать объект в Transform
                var wardrobeTransform = worldHouse.Find("House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Bedroom/Bedroom Wardrobe");

                try
                {
                    if (wardrobeTransform != null)
                    {
                        var wardrobeGameObject = wardrobeTransform.gameObject;
                        wardrobeGameObject.GetComponent<BoxCollider>().enabled = false;

                        foreach (Transform child in wardrobeTransform)
                        {
                            child.gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        LoggerInstance.Msg("Wardrobe Transform not found.");
                    }
                }
                catch (Exception)
                {
                    LoggerInstance.Msg("Error while handling wardrobe transform.");
                }


                TryfindChild(worldBasement, "Act/ContinueScene").SetActive(false);

                //TryfindChild(worldBasement, "/Act/ContinueScene\"").SetActive(false);



                // Работа с AnimationKiller
                LoggerInstance.Msg("AnimationKiller start");
                string objectPath = "Quests/Quest2 HideAndSeek/Animation Killer";
                AnimationKiller = FindAndInstantiateObject(worldBasement, objectPath, "InitializeGameObjects");
                AnimationKiller.GetComponent<Location6_MitaKiller>().mita = Mita.transform;
                AnimationKiller.SetActive(false);
                if (AnimationKiller != null)
                {
                    LoggerInstance.Msg("AnimationKiller instantiated and ready for use.");
                    var mitaChild = AnimationKiller.transform.Find("Mita");

                    if (mitaChild != null)
                    {
                        mitaChild.gameObject.SetActive(false);
                        //Mita.transform.SetParent(AnimationKiller.transform, true);
                        LoggerInstance.Msg("Child object 'Mita' deactivated.");
                    }
                    else
                    {
                        LoggerInstance.Msg("Child object 'Mita' not found.");
                    }
                }
                else
                {
                    LoggerInstance.Msg("Failed to initialize AnimationKiller.");
                }
            }
            catch (Exception e)
            {
                LoggerInstance.Msg($"Error in InitializeGameObjectsWhenReady: {e.Message}");
            }
        }
        public GameObject TryfindChild(Transform parent,string path)
        {
            try
            {
                return worldBasement.Find(path).gameObject;
            }
            catch (Exception e)
            {

                LoggerInstance.Msg(e);
                return null;
            }
        }


        private GameObject FindAndInstantiateObject(Transform parent, string path, string logPrefix)
        {
            try
            {
                LoggerInstance.Msg($"{logPrefix}: Attempting to find object at path: {path}");

                // Проверяем, что родитель не null
                if (parent == null)
                {
                    LoggerInstance.Msg($"{logPrefix}: Parent is null. Cannot search for object.");
                    return null;
                }

                // Ищем объект по указанному пути
                Transform target = parent.Find(path);
                if (target == null)
                {
                    LoggerInstance.Msg($"{logPrefix}: Object not found at path: {path}");
                    return null;
                }

                // Логируем успешный поиск
                LoggerInstance.Msg($"{logPrefix}: Object found. Instantiating...");
                GameObject instance = GameObject.Instantiate(target.gameObject);

                // Проверяем успешную инстанциацию
                if (instance == null)
                {
                    LoggerInstance.Msg($"{logPrefix}: Failed to instantiate object.");
                    return null;
                }

                LoggerInstance.Msg($"{logPrefix}: Object instantiated successfully.");
                return instance;
            }
            catch (Exception e)
            {
                LoggerInstance.Msg($"{logPrefix}: Exception occurred - {e.Message}");
                return null;
            }
        }



        bool FirstTime = true;
        public override async void OnFixedUpdate()
        {

            //LoggerInstance.Msg("1"+ CurrentSceneName);
            if ( CurrentSceneName != "Scene 4 - StartSecret") { return; };
            //LoggerInstance.Msg("2" + CurrentSceneName);
            if ( !AllLoaded ) { return;  };

            timer += Time.fixedDeltaTime;
            await UpdateBoringTimerAsync(Time.fixedDeltaTime);


            if (timer >= Interval)
            {
                timer = 0f;
                await HandleDialogueAsync();
            }

            if ( getDistance()<0.75f)
            {
                Mita.AiShraplyStop();
            }

            base.OnFixedUpdate();
        }

        float getDistance()
        {
            if (Mita == null || playerPerson == null) { return 0f; }
            return Vector3.Distance(Mita.transform.GetChild(0).position, playerPerson.transform.position);
        }

        private async Task HandleDialogueAsync()
        {
            
            string dataToSent = "waiting";
            if (playerMessage != "")
            {
                await ResetBoringTimerAsync();
                dataToSent = playerMessage;
                playerMessage = "";
            }
            else if (MitaBoringtimer >= MitaBoringInterval)
            {
                await ResetBoringTimerAsync();
                dataToSent = "boring";
            }
            string response = "";
            try
            {
                response = await GetResponseFromPythonSocketAsync(dataToSent);
                if (response != "")
                {
                    LoggerInstance.Msg("after GetResponseFromPythonSocketAsync");

                    // Теперь используем MelonCoroutines.Start для запуска корутины
                    MelonCoroutines.Start(DisplayResponseAndEmotionCoroutine(response));
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Msg($"Error in HandleDialogueAsync: {ex.Message}");
            }
            
        }


        // Глобальный список для хранения дочерних объектов
        List<GameObject> globalChildObjects = new List<GameObject>();

        // Функция для получения дочерних объектов и добавления их в глобальный список
        void CollectChildObjects(GameObject parentObject)
        {
            // Проверяем, что объект не null
            if (parentObject == null)
            {
                LoggerInstance.Error("Parent object is null!");
                return;
            }

            // Получаем Transform родительского объекта
            Transform parentTransform = parentObject.transform;

            // Перебираем всех детей
            for (int i = 0; i < parentTransform.childCount; i++)
            {
                Transform childTransform = parentTransform.GetChild(i);
                if (childTransform != null)
                {
                    // Добавляем дочерний объект в глобальный список
                    globalChildObjects.Add(childTransform.gameObject);
                    LoggerInstance.Msg($"Child added: {childTransform.name}");
                }
            }

            // Выводим общее количество детей
            LoggerInstance.Msg($"Total children collected: {globalChildObjects.Count}");
        }
        public Transform GetRandomLoc()
        {
            LoggerInstance.Msg("Before try Tring GetRandomLoc");
            try
            {
                LoggerInstance.Msg("Tring GetRandomLoc");
                // Проверяем, что список не пустой
                if (globalChildObjects == null || globalChildObjects.Count == 0)
                {
                    LoggerInstance.Error("globalChildObjects is null or empty!");
                    return null; // Возвращаем null, если список пуст
                }

                // Генерируем случайный индекс
                int randomIndex = UnityEngine.Random.Range(0, globalChildObjects.Count);

                // Получаем случайный объект по индексу
                GameObject randomObject = globalChildObjects[randomIndex];

                // Проверяем, что объект действительно существует и имеет компонент Transform
                if (randomObject == null)
                {
                    LoggerInstance.Error("Random object is null!");
                    return null; // Возвращаем null, если объект не найден
                }

                // Логируем имя объекта
                LoggerInstance.Msg($"Random object selected: {randomObject.name}");

                // Возвращаем компонент Transform
                return randomObject.transform;
            }
            catch (Exception)
            {
                LoggerInstance.Msg("Error with random loc");
                return null;
            }
            
        }




        // Список для хранения созданных диалогов
        private List<GameObject> dialogPool = new List<GameObject>();

        private readonly object syncLock = new object();

        private IEnumerator DisplayResponseAndEmotionCoroutine(string response)
        {
            LoggerInstance.Msg("DisplayResponseAndEmotionCoroutine");

            // Пример кода, который будет выполняться на главном потоке
            yield return null;  // Это нужно для того, чтобы выполнение произошло после завершения текущего кадра

            // Ждем, пока patch_to_sound_file перестанет быть пустым
            while (string.IsNullOrEmpty(patch_to_sound_file) )
            {
                if (patches_to_sound_file.Count > 0)
                {
                    patch_to_sound_file = patches_to_sound_file.Dequeue();
                }
 
                yield return null;  // Пауза до следующего кадра, проверка снова
            }

            // После того как patch_to_sound_file стал пустым, вызываем метод DisplayResponseAndEmotion
            DisplayResponseAndEmotion(response);
        }


        private void DisplayResponseAndEmotion(string response)
        {
            LoggerInstance.Msg("DisplayResponseAndEmotion");

            isInputBlocked = true;

            try
            {

                string modifiedResponse = SetMovementStyle(response);

                AudioClip audioClip = null;

                if (!string.IsNullOrEmpty(patch_to_sound_file))
                {
                    try
                    {
                        LoggerInstance.Msg("patch_to_sound_file not null");
                        audioClip = LoadAudioClipFromFileAsync(patch_to_sound_file).Result;
                        patch_to_sound_file = "";
                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Error($"Error loading audio file: {ex.Message}");
                    }
                }

                int delay = 4000 * modifiedResponse.Length / 50;
                MelonCoroutines.Start(PlayMitaSound(delay, audioClip));

                List<string> dialogueParts = SplitText(modifiedResponse, maxLength: 50);

                // Запуск диалогов последовательно, с использованием await или вложенных корутин
                MelonCoroutines.Start(ShowDialoguesSequentially(dialogueParts));
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error in DisplayResponseAndEmotion: {ex.Message}");
            }
        }

        private IEnumerator ShowDialoguesSequentially(List<string> dialogueParts)
        {
            foreach (string part in dialogueParts)
            {
                LoggerInstance.Msg("foreach foreach " + part);


                int delay = Mathf.Max(3500 * part.Length / 50,500);
                yield return MelonCoroutines.Start(ShowDialogue(part, delay));
            }
        }



        private IEnumerator ShowDialogue(string part, int delay)
        {

            LoggerInstance.Msg("ShowDialogue");

            string modifiedPart = part;
            List<string> commands;
            EmotionType emotion = EmotionType.none; 
            try
            {
                LoggerInstance.Msg("Begin try:"+ modifiedPart);
                modifiedPart = SetFaceStyle(modifiedPart);
                (emotion,modifiedPart) = SetEmotionBasedOnResponse(modifiedPart);
                LoggerInstance.Msg("After SetEmotionBasedOnResponse " + modifiedPart);

                (commands, modifiedPart) = ExtractCommands(modifiedPart);
                if (commands.Count > 0)
                {
                    ProcessCommands(commands);
                }
                LoggerInstance.Msg("After ExtractCommands " + modifiedPart);
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error processing part of response: {ex.Message}");
            }


            GameObject currentDialog = InstantiateDialog();
            
            Dialogue_3DText answer = currentDialog.GetComponent<Dialogue_3DText>();


            answer.textPrint = modifiedPart;
            answer.themeDialogue = Dialogue_3DText.Dialogue3DTheme.Mita;
            answer.timeShow = delay;
            answer.speaker = Mita?.gameObject;
            if (emotion  != EmotionType.none) answer.emotionFinish = emotion;

            currentDialog.SetActive(true);
            yield return new WaitForSeconds(delay / 1000f);

            GameObject.Destroy(currentDialog);
            LoggerInstance.Msg("Dialogue part finished and destroyed.");

            isInputBlocked = false;
        }

        private IEnumerator PlayMitaSound(int delay, AudioClip audioClip)
        {
            LoggerInstance.Msg("PlayMitaSound");

            GameObject currentDialog = InstantiateDialog();
            currentDialog.SetActive(true);
            Dialogue_3DText answer = currentDialog.GetComponent<Dialogue_3DText>();

            // Если есть аудио, проигрываем его до начала текста
            if (audioClip != null)
            {
                LoggerInstance.Msg("Loading voice...");
                answer.timeSound = delay;
                answer.LoadVoice(audioClip);
                audioClip = null;
            }
            answer.speaker = Mita?.gameObject;

            yield return new WaitForSeconds(delay);

            GameObject.Destroy(currentDialog);
            LoggerInstance.Msg("Dialogue part finished and destroyed.");
        }

        private void PlayerTalk(string text)
        {
            GameObject currentDialog = null;

            try
            {
              
                int delay = 4000 * text.Length / 50;

                currentDialog = InstantiateDialog(false);
                if (currentDialog != null)
                {
                    Dialogue_3DText answer = currentDialog.GetComponent<Dialogue_3DText>();
                    if (answer == null)
                    {
                        throw new Exception("Dialogue_3DText component not found.");
                    }

                    answer.speaker = playerPerson.gameObject;
                    answer.textPrint = text;
                    answer.themeDialogue = Dialogue_3DText.Dialogue3DTheme.Player;
                    answer.timeShow = delay;
                    
                    currentDialog.SetActive(true);

                    MitaBoringtimer = 0f;
                }
                else
                {
                    LoggerInstance.Msg("currentDialog is null.");
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
                                Mita.AiWalkToTarget(playerPerson.transform);
                                Location34_Communication.indexSwitchAnimation = 1;
                                break;

                            case "подойти к случайной точке":
                                Mita.AiWalkToTarget( GetRandomLoc() );
                                Location34_Communication.indexSwitchAnimation = 1;
                                break;
                            case "телепортироваться к случайной точке":
                                Mita.MitaTeleport( GetRandomLoc() );
                                Location34_Communication.indexSwitchAnimation = 1;
                                break;
                            case "телепортироваться к игроку":
                                Mita.MitaTeleport(playerPerson.transform);
                                Location34_Communication.indexSwitchAnimation = 1;
                                break;
                            case "зарезать игрока":
                                AnimationKiller.SetActive(true);
                                AnimationKiller.GetComponent<Location6_MitaKiller>().Kill();
                                AnimationKiller.SetActive(false);
                                break;
                            case "выключить игрока":
                                MainMenu.ButtonLoadScene("SceneMenu");
                                break;
                            default:
                                if (command.Contains("PositionMita"))
                                {
                                    try
                                    {
                                        GameObject point = GameObject.Find(command);
                                        Mita.MitaTeleport(point.transform);
                                        Location34_Communication.indexSwitchAnimation = 1;
                                    }
                                    catch (Exception e)
                                    {
                                        LoggerInstance.Msg($"Unknown point: {command}");
                                    }
                                }
                                else
                                {
                                    LoggerInstance.Msg($"Unknown command: {command}");
                                }
                                
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
        public static (List<string>, string) ExtractCommands(string response)
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

            string result = Regex.Replace(response, @"<c>.*?</c>", "");
            // Удаляем теги эмоций из текста
            return (commands, result);

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


        private GameObject InstantiateDialog(bool Mita = true)
        {
            Transform world = GameObject.Find("World")?.transform;
            if (world == null)
            {
                LoggerInstance.Msg("World object not found.");
                return null;
            }
            if (Mita)
            {
                return GameObject.Instantiate(CustomDialog, world.Find("Quests/Quest 1/Dialogues"));
            }
            else
            {
                return GameObject.Instantiate(CustomDialogPlayer, world.Find("Quests/Quest 1/Dialogues"));
            }
        }

        private (EmotionType,string) SetEmotionBasedOnResponse(string response)
        {
            LoggerInstance.Msg($"Inside SetEmotionBasedOnResponse: "+response);
            try
            {
                var (emotions, cleanedResponse) = ExtractEmotions(response);
                EmotionType emotion = EmotionType.none;
                if (emotions.Count > 0)
                {
                    Enum.TryParse<EmotionType>(emotions[0], true, out var result);

                    Mita.FaceEmotionType(result);
                    if (emotions.Count > 1) { 
                        Enum.TryParse<EmotionType>(emotions[1], true, out var result2);
                        emotion = result2;
                    }

                }
                LoggerInstance.Msg($"Inside SetEmotionBasedOnResponse end: " + cleanedResponse);
                return (emotion,cleanedResponse);
            }
            catch (Exception ex)
            {
                LoggerInstance.Msg($"Error extracting emotion: {ex.Message}");
                //Mita.FaceEmotionType(GetRandomEmotion());
                return (EmotionType.none, response); // Если произошла ошибка, возвращаем исходный текст
            }
        }


        public static (List<string>, string) ExtractEmotions(string response)
        {

            MelonLogger.Msg($"Inside ExtractEmotions start: " + response);
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

            MelonLogger.Msg($"Inside ExtractEmotions end: " + cleanedResponse);
            return (emotions, cleanedResponse);
            
        }
        public string SetFaceStyle(string response)
        {

            // Регулярное выражение для извлечения эмоций
            string pattern = @"<f>(.*?)</f>";
            Match match = Regex.Match(response, pattern);

            string faceStyle = string.Empty;
            string cleanedResponse = Regex.Replace(response, @"<f>.*?</f>", ""); // Очищаем от всех тегов

            if (match.Success)
            {
                // Если эмоция найдена, устанавливаем её в переменную faceStyle
                faceStyle = match.Groups[1].Value;
            }

            try
            {
                // Проверка на наличие объекта Mita перед применением эмоции
                if (Mita == null || Mita.gameObject == null)
                {
                    LoggerInstance.Error("Mita object is null or Mita.gameObject is not active.");
                    return cleanedResponse; // Возвращаем faceStyle и очищенный текст
                }

                // Устанавливаем лицо, если оно найдено
                switch (faceStyle)
                {
                    case "Смущаться":
                        //Mita.FaceColorUpdate();
                        //Mita.FaceLayer(1);
                        break;
                    case "Маска грусти":
                        //Mita.FaceColorUpdate();
                        //Mita.FaceLayer(2);
                        break;
                    default:
                        //Mita.FaceColorUpdate();
                        //Mita.FaceLayer(0);
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Problem with FaceStyle: {ex.Message}");
            }

            // Возвращаем кортеж: лицо и очищенный текст
            return cleanedResponse;
        }

        public string SetMovementStyle(string response)
        {
            // Регулярное выражение для извлечения эмоций
            string pattern = @"<m>(.*?)</m>";
            Match match = Regex.Match(response, pattern);

            string MovementStyle = string.Empty;
            string cleanedResponse = Regex.Replace(response, @"<m>.*?</m>", ""); // Очищаем от всех тегов

            if (match.Success)
            {
                // Если эмоция найдена, устанавливаем её в переменную faceStyle
                MovementStyle = match.Groups[1].Value;
            }
            try
            {
                // Проверка на наличие объекта Mita перед применением эмоции
                if (Mita == null || Mita.gameObject == null)
                {
                    LoggerInstance.Error("Mita object is null or Mita.gameObject is not active.");
                    return cleanedResponse; // Возвращаем faceStyle и очищенный текст
                }
                // Устанавливаем лицо, если оно найдено
                switch (MovementStyle)
                {
                    case "Следовать рядом с игроком":
                        Location34_Communication.ActivationCanWalk(true);

                        break;
                    case "Стоять на месте":
                        Location34_Communication.ActivationCanWalk(false);
                        break;
                    default:
                        //Mita.FaceColorUpdate();
                        //Mita.FaceLayer(0);
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Problem with FaceStyle: {ex.Message}");
            }

            // Возвращаем кортеж: лицо и очищенный текст
            return cleanedResponse;
        }

       

        private EmotionType GetRandomEmotion()
        {
            EmotionType[] emotions = (EmotionType[])Enum.GetValues(typeof(EmotionType));
            int randomIndex = UnityEngine.Random.Range(0, emotions.Length);
            return emotions[randomIndex];
        }

        public async Task<string> GetResponseFromPythonSocketAsync(string input)
        {
            // Ожидаем, чтобы получить доступ к ресурсу (сокету)
            

            
            
            using (Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                bool connected = await TryConnectAsync(clientSocket, ServerAddress, Port);
                if (!connected)
                {
                    return string.Empty; // Возвращаем пустой ответ, если не удалось подключиться
                }

                // Дополнительная логика для подготовки данных
                distance = getDistance();
                roomIDPlayer = GetRoomID(playerPerson.transform);
                roomIDMita = GetRoomID(Mita.transform);
                input = $"{input}|||{distance}|||{roomIDPlayer}|||{roomIDMita}";

                byte[] messageBytes = Encoding.UTF8.GetBytes(input);
                await clientSocket.SendAsync(messageBytes, SocketFlags.None);

                byte[] buffer = new byte[2500];
                try
                {
                    int bytesRead = await clientSocket.ReceiveAsync(buffer, SocketFlags.None);

                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    string[] parts = receivedMessage.Split(new string[] { "|||" }, StringSplitOptions.None);

                    // Логируем ответ
                    string response = parts[0];

                    patches_to_sound_file.Enqueue(parts[1]);
                    //patch_to_sound_file = parts[1];
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
                        LoggerInstance.Msg($"Max retries reached. Unable to connect to {address}:{port}.{ex}");
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

                // Удаление исходного файла
                try
                {
                    await Task.Delay(300);
                    File.Delete(filePath);
                    MelonLoader.MelonLogger.Msg("Original file deleted: " + filePath);
                }
                catch (Exception ex)
                {
                    MelonLoader.MelonLogger.Error("Error deleting original file: " + ex.Message);
                }

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
