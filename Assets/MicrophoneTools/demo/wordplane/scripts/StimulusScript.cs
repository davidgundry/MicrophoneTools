using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using WordPlane;

namespace WordPlane
{

    public class StimulusScript : MonoBehaviour
    {

        public Sprite[] sprites;
        private int currentSpriteIndex = 0;

        public int displayTime;
        private float timer;

        private Image imageScript;
        public bool newInput;
        private GameController gameController;
        private RectTransform timeBarRect;

        // Use this for initialization
        void Start()
        {
            imageScript = GetComponent<Image>();
            gameController = GameObject.Find("GameController").GetComponent<GameController>();
            timeBarRect = GameObject.Find("TimeBar").GetComponent<RectTransform>();
            timer = displayTime;
        }

        // Update is called once per frame
        void Update()
        {
            if ((displayTime > 0) && (timer > 0))
            {
                timer -= Time.deltaTime;
                timeBarRect.sizeDelta = new Vector2(timeBarRect.sizeDelta.x, 100 * (timer / displayTime));
            }
        }

        void StimulusClick()
        {
            if ((newInput) && (timer <= 0))
            {
                currentSpriteIndex = (currentSpriteIndex + 1) % sprites.Length;
                imageScript.sprite = sprites[currentSpriteIndex];
                newInput = false;
                gameController.AddPoints(1);
                timer = displayTime;

            }
        }
    }
}