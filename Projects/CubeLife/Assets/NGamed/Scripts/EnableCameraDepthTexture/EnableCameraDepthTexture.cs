using UnityEngine;

public class EnableCameraDepthTexture : MonoBehaviour {
	private void Start() {
		Camera.main.depthTextureMode = DepthTextureMode.Depth;
	}
}
