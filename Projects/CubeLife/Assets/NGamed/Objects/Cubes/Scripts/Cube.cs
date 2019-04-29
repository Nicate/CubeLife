using UnityEngine;

public class Cube : MonoBehaviour {
	public float weight;

	public string[] frontConstraints;
	public string[] rightConstraints;
	public string[] backConstraints;
	public string[] leftConstraints;


	public bool isTransition() {
		return frontConstraints.Length > 1 || rightConstraints.Length > 1 || backConstraints.Length > 1 || leftConstraints.Length > 1;
	}
}
