using UnityEngine;
using System.Collections;

namespace Collect
{
    public class PlayerControls : MonoBehaviour
    {

        private Rigidbody rb;
        public float speed;
        public Transform target;
        public Transform soundWave;

        private bool soundWaveEmitting = false;
        private float timer;
        private const float soundWaveTime = 0.5f;

        // Use this for initialization
        void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        // Update is called once per frame
        void Update()
        {
            if (soundWaveEmitting)
            {
                soundWave.localScale += new Vector3(1, 1, 1) * Time.deltaTime * 40;
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    soundWaveEmitting = false;
                    soundWave.localScale = new Vector3(1, 1, 1);
                }
            }

            transform.rotation = new Quaternion();
            float step = speed * Time.deltaTime;
            rb.MovePosition(Vector3.MoveTowards(transform.position, target.position + new Vector3(0, 1, 0), step));


            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out hit))
                {
                    target.position = hit.point + new Vector3(0, 0.01f, 0);
                }
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Began)
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(i).position);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit))
                    {
                        target.position = hit.point + new Vector3(0, 0.01f, 0);
                    }
                }
            }

        }

        public void Speak()
        {
            soundWave.localScale = new Vector3(1, 1, 1);
            soundWaveEmitting = true;
            timer = soundWaveTime;

            GameObject[] listeners = GameObject.FindGameObjectsWithTag("Listener");
            for (int i = 0; i < listeners.Length; i++)
            {
                ListenerBehaviour listenerBehaviour = listeners[i].GetComponent<ListenerBehaviour>();
                listenerBehaviour.HearNoise(transform.position);
            }

        }
    }
}
