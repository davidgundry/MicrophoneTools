using UnityEngine;
using System.Collections;
using MicTools;

namespace MicTools
{

    [RequireComponent(typeof(FFTPitchDetector))]
    [AddComponentMenu("MicrophoneTools/VowelFinder")]
    public class VowelFinder : MonoBehaviour
    {

        private FFTPitchDetector formantFinder;
        private readonly VowelRecord[] vowels;

        public string vowel;

        VowelFinder()
        {
            vowels = new VowelRecord[4];
            vowels[0] = new VowelRecord("i", 240, 2400);
            //vowels[1] = new VowelRecord("y", 235, 2100);
            //vowels[2] = new VowelRecord("e", 390, 2300);
            //vowels[3] = new VowelRecord("\u00F8", 370, 1900);
            //vowels[4] = new VowelRecord("\u025B", 610, 1900);
            //vowels[5] = new VowelRecord("\u0153", 585, 1710);
            vowels[1] = new VowelRecord("a", 850, 1610);
            //vowels[7] = new VowelRecord("\u0276", 820, 1530);
            vowels[2] = new VowelRecord("\u0251", 750, 940);
            //vowels[9] = new VowelRecord("\u0252", 700, 760);
            //vowels[10] = new VowelRecord("\u028C", 600, 1170);
            //vowels[11] = new VowelRecord("\u0254", 500, 700);
            //vowels[12] = new VowelRecord("\u0264", 460, 1310);
            //vowels[13] = new VowelRecord("o", 360, 640);
            //vowels[14] = new VowelRecord("\u026F", 300, 1390);
            vowels[3] = new VowelRecord("u", 250, 595);
        }

        // Use this for initialization
        void Start()
        {
            formantFinder = GetComponent<FFTPitchDetector>();
        }

        // Update is called once per frame
        void Update()
        {
            /*FormantRecord[] formants = formantFinder.Formants;
            if (formants.Length >= 3)
            {
                int lowestIndex = 0;
                float lowestValue = 1000000;
                for (int i = 0; i < vowels.Length; i++)
                {
                    float distance = Mathf.Sqrt(Mathf.Pow(vowels[i].F1 - (int)formants[1].PeakFrequency, 2) + Mathf.Pow(vowels[i].F2 - (int)formants[2].PeakFrequency, 2));
                    if (distance < lowestValue)
                    {
                        lowestIndex = i;
                        lowestValue = distance;
                    }
                }
                vowel = vowels[lowestIndex].ToString();
                //if (lowestValue > 200)
                //    vowel = "";
            }*/


        }
    }
}