using UnityEngine;
using System.Collections;

namespace WordPlane
{
    public class CoinBehaviour : MonoBehaviour
    {

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            transform.Rotate(Vector3.up * Time.deltaTime * 250);
        }
    }
}