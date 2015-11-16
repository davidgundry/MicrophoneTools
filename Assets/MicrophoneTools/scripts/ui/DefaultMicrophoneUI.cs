using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;
using MicTools;

namespace MicTools
{

    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(GraphicRaycaster))]
    [RequireComponent(typeof(MicrophoneController))]
    [AddComponentMenu("MicrophoneTools/DefaultMicrophoneUI")]
    public class DefaultMicrophoneUI : MonoBehaviour, MicrophoneUI
    {

        public bool askPermission = true;
        public bool useDefaultMic = true;

        public bool AskPermission() //Implementation of interface method
        {
            return askPermission;
        }

        public bool UseDefaultMic() //Implementation of interface method
        {
            return useDefaultMic;
        }

        private Canvas canvas;
        private MicrophoneController microphoneController;

        void Awake()
        {
            microphoneController = this.GetComponent<MicrophoneController>();

            if (FindObjectOfType<EventSystem>() == null)
                CreateEventSystem();

            CanvasScaler canvasScaler = GetComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }


        void OnSoundEvent(SoundEvent soundEvent)
        {
            switch (soundEvent)
            {
                case SoundEvent.PermissionRequired:
                    MicrophoneWarning();
                    break;
            }
        }


        /*
         *  This is called before first requesting user authorisation for use of the microphone.
         */
        public void MicrophoneWarning()
        {
            Transform panel = Instantiate(Resources.Load("defaultui/MicWarningPanel", typeof(Transform))) as Transform;
            panel.SetParent(transform, false);
            panel.Find("OkayButton").GetComponent<Button>().onClick.AddListener(delegate
                {
                    gameObject.SendMessage("OnSoundEvent", SoundEvent.PermissionGranted);
                    CloseAll();
                });
            panel.Find("LearnMoreButton").GetComponent<Button>().onClick.AddListener(delegate
            {
                Transform infoPanel = Instantiate(Resources.Load("defaultui/MicInfoPanel", typeof(Transform))) as Transform;
                infoPanel.SetParent(transform, false);
                infoPanel.Find("ScrollPanel/ScrollContent/ReturnButton").GetComponent<Button>().onClick.AddListener(delegate
                {
                    GameObject.Destroy(infoPanel.gameObject);
                });
            });
        }

        /*
         *  This is called if no available microphones were found when setting the microphone.
         */
        public void NoMicrophonesFound()
        {
            Transform noMicPanel = Instantiate(Resources.Load("defaultui/NoMicFoundPanel", typeof(Transform))) as Transform;
            noMicPanel.SetParent(transform, false);
            noMicPanel.GetChild(0).GetComponent<Button>().onClick.AddListener(delegate
                {
                    CloseAll();
                    // TODO: Go to exit spash.
                });
        }

        /*
         *  This is called if multiple devices were found when setting the microphone, and useDefaultMic is false.
         */
        public void ChooseDevice(string[] devices)
        {
            Transform panel = Instantiate(Resources.Load("defaultui/MicSelectPanel", typeof(Transform))) as Transform;
            panel.SetParent(transform, false);
            float height = ((Transform)Resources.Load("defaultui/MicOptionButton", typeof(Transform))).GetComponent<RectTransform>().rect.height;
            panel.GetChild(0).GetChild(0).GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, devices.Length * height + 20);
            panel.GetChild(0).GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -(devices.Length * height + 20) / 2);
            for (int i = devices.Length - 1; i >= 0; i--)
            {
                Transform button = Instantiate(Resources.Load("defaultui/MicOptionButton", typeof(Transform))) as Transform;
                button.SetParent(panel.GetChild(0).GetChild(0), false);
                button.localPosition = new Vector2(0, devices.Length - i * height - height / 2 - 4 + (devices.Length * height) / 2);
                button.GetComponentInChildren<Text>().text = devices[i];
                int index = i;
                button.GetComponent<Button>().onClick.AddListener(delegate
                    {
                        microphoneController.SetDevice(index);
                        gameObject.SendMessage("OnSoundEvent", SoundEvent.MicrophoneReady);
                        CloseAll();
                    });
            }
        }

        private void CloseAll()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject.Destroy(transform.GetChild(i).gameObject);
            }
        }

        private void CreateEventSystem()
        {
            Debug.LogWarning("DefaultMicrophoneUI: No EventSystem found, creating one");
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.AddComponent<StandaloneInputModule>();
            eventSystem.AddComponent<TouchInputModule>();
        }
    }
}