using UnityEngine;
using System.Collections;

namespace Collect
{

    public class ListenerBehaviour : MonoBehaviour
    {

        private Vector3 target;
        public float speed;
        public float turnSpeed;
        private bool wandering;

        private float confidence;

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (confidence > 2)
            {
                wandering = false;
                if (Vector3.Distance(target,transform.position) > 7.5f)
                {
                    float speed = (int)confidence/3;
                    float step = speed * Time.deltaTime;
                    transform.position = Vector3.MoveTowards(transform.position, target - new Vector3(0, 1, 0), step);

                    transform.LookAt(target, Vector3.up);
                    transform.Rotate(new Vector3(0, 180, 0));
                }
            }
            else if (wandering)
            {
                float step = Time.deltaTime;
                transform.position = Vector3.MoveTowards(transform.position, target - new Vector3(0, 1, 0), step);

                transform.LookAt(target, Vector3.up);
                transform.Rotate(new Vector3(0, 180, 0));

                if (Vector3.Distance(target, transform.position) < 1f)
                    Wander();
            }
            else if (confidence < 0)
                Wander();

            if (confidence > 0)
                confidence -= Time.deltaTime;
        }

        public void HearNoise(Vector3 position)
        {
            if (position == target)
                confidence += 2f;
            else
            {
                confidence -= 1f;
                if (confidence <= 0)
                {
                    target = position;
                    confidence = 2f;
                }
            }
        }

        private void Wander()
        {
            target = new Vector3(transform.position.x + Random.Range(-20, 20), transform.position.y, transform.position.z + Random.Range(-20, 20));
            wandering = true;
        }
    }
}
