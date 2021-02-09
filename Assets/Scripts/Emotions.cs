using UnityEngine;
using Unity.Barracuda;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.UI;

public class Emotions : MonoBehaviour {

	const int IMAGE_SIZE = 64;
	const string INPUT_NAME = "Input3";
	const string OUTPUT_NAME = "Plus692_Output_0";

	readonly List<string> OutputLabels = new List<string>() { "neutral", "happiness", "surprise", "sadness", "anger", "disgust", "fear", "contempt" };

	public CameraView CameraView;
	public Preprocess preprocess;
	public NNModel modelFile;
	public Text uiText;

	IWorker worker;

	void Start() {
		var model = ModelLoader.Load(modelFile);
		worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
	}

    void Update() {

		WebCamTexture webCamTexture = CameraView.GetCamImage();

		if (webCamTexture.didUpdateThisFrame && webCamTexture.width > 100) {
			preprocess.ScaleAndCropImage(webCamTexture, IMAGE_SIZE, RunModel);
		}
	}

	void RunModel(byte[] pixels) {
		StartCoroutine(RunModelRoutine(pixels));
	}

	IEnumerator RunModelRoutine(byte[] pixels) {
		Tensor tensor = TransformInput(pixels);

		var inputs = new Dictionary<string, Tensor> {
			{ INPUT_NAME, tensor }
		};

		worker.Execute(inputs);
		Tensor outputTensor = worker.PeekOutput(OUTPUT_NAME);

		//get largest output
		List<float> temp = outputTensor.ToReadOnlyArray().ToList();
		float max = temp.Max();
		int index = temp.IndexOf(max);

		//set UI text
		uiText.text = OutputLabels[index];

		//dispose tensors
		tensor.Dispose();
		outputTensor.Dispose();
		yield return null;
	}

	//This model requires a single channel greyscale from 0-255
	Tensor TransformInput(byte[] pixels){
		float[] singleChannel = new float[IMAGE_SIZE * IMAGE_SIZE];
		for (int i = 0; i < singleChannel.Length; i++) {
			Color color = new Color32(pixels[i * 3 + 0], pixels[i * 3 + 1], pixels[i * 3 + 2], 255);
			singleChannel[i] = color.grayscale * 255;
		}
		return new Tensor(1, IMAGE_SIZE, IMAGE_SIZE, 1, singleChannel);
	}
}
