using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuGamePlay : MonoBehaviour
{


	public static MenuGamePlay instance;

	public float moveSpeed, distance_Factor = 1f;
	private float distance_Move;
	private bool gameJustStarted;

	public GameObject obstacles_Obj;
	public GameObject[] obstacle_List;

	[HideInInspector]
	public bool obstacles_Is_Active;

	private string Coroutine_Name = "SpawnObstacles";



	public GameObject pause_Panel;
	public Animator pause_Anim;

	public GameObject gameOver_Panel;
	public Animator gameOver_Anim;



	public bool IsMenu;

    void Awake () {
		MakeInstance ();

	}

	void Start() {
		gameJustStarted = true;

		GetObstacles ();
		StartCoroutine (Coroutine_Name);

	}

	void Update () {
		MoveCamera ();
	}

	void MakeInstance() {
		if (instance == null) {
			instance = this;

		} else if (instance != null) {
			Destroy (gameObject);
		}
	}

	void MoveCamera() {

		if (gameJustStarted) {

			if (!PlayerController.instance.player_Died) {
				// check if player is alive
				if (moveSpeed < 12.0f) {
					moveSpeed += Time.deltaTime * 5.0f;

				} else {
					moveSpeed = 12f;
					gameJustStarted = false;
				}
			}
		}

		// check if player is alive
		if(!PlayerController.instance.player_Died) {
			Camera.main.transform.position += new Vector3(moveSpeed * Time.deltaTime, 0f, 0f);
			UpdateDistance ();
		}

	}

	void UpdateDistance() {
		distance_Move += Time.deltaTime * distance_Factor;
		float round = Mathf.Round (distance_Move);

		
		if (round >= 30.0f && round < 60.0f) {
			moveSpeed = 14f;

		} else if (round >= 60f) {
			moveSpeed = 16f;
		}
	}

	void GetObstacles() {
		obstacle_List = new GameObject[obstacles_Obj.transform.childCount];

		for (int i = 0; i < obstacle_List.Length; i++) {
			obstacle_List [i] = 
				obstacles_Obj.GetComponentsInChildren<ObstacleHolder> (true) [i].gameObject;
		}
	}

	IEnumerator SpawnObstacles() {

		while (true) {

			if (!PlayerController.instance.player_Died) {

				if (!obstacles_Is_Active) {

					if (Random.value <= 0.85f) {

						int randomIndex = 0;

						do {
							
							randomIndex = Random.Range(0, obstacle_List.Length);

						} while(obstacle_List[randomIndex].activeInHierarchy);

						obstacle_List [randomIndex].SetActive (true);
						obstacles_Is_Active = true;

					}

				}

			}

			yield return new WaitForSeconds (0.6f);
		}
	}
}
