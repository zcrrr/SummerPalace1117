using UnityEngine;
using System.Collections;

public class WaterFlow : MonoBehaviour {
	public Renderer rend;

	// Use this for initialization
	void Start () {
		rend = GetComponent<Renderer>();
	}
	
	// Update is called once per frame
	void Update () {
		float offset = Time.time * 0.05f;
		rend.material.SetTextureOffset("_MainTex", new Vector2(offset, 0));
	}
}
