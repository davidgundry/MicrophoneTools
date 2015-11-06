using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class TypedInputBehaviour : MonoBehaviour {

    private InputField inf;
    public Transform player;
    private GameController gameController;
    public Transform popTextPrefab;

	// Use this for initialization
	void Start () {
        inf = GetComponent<InputField>();
        gameController = player.GetComponent<GameController>();
	}
	
	// Update is called once per frame
    void Update() {
        foreach (char c in Input.inputString) {
            if (c == " "[0])
            {
                if (inf.text != " ")
                {
                    gameController.InputEvent();
                    CreatePopText();
                }
                inf.text = "";


            }
            else
                if (c == "\n"[0] || c == "\r"[0])
                {
                    if (inf.text != "")
                    {
                        gameController.InputEvent();
                        CreatePopText();
                    }
                    inf.text = "";
                    inf.Select();
                    inf.ActivateInputField();
                }
        }
	}

    void CreatePopText()
    {
        Transform t = Instantiate(popTextPrefab);
        t.position = player.position + new Vector3(0, 0.5f, 1);
        t.GetComponent<TextMesh>().text = inf.text;
    }
}
