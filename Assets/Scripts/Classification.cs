using UnityEngine;
using Unity.Barracuda;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.UI;
using System;

public class Classification : MonoBehaviour {

	const int IMAGE_SIZE = 224;
	const string INPUT_NAME = "images";
	const string OUTPUT_NAME = "Softmax";

	[Header("Model Stuff")]
	public NNModel modelFile;
	public TextAsset labelAsset;

	[Header("Scene Stuff")]
	public CameraView CameraView;
	public Preprocess preprocess;
	public Text uiText;

	string[] labels;
	IWorker worker;

	void Start() {
        var model = ModelLoader.Load(modelFile);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
        LoadLabels();
	}

	void LoadLabels() {
		//get only items in quotes
		var stringArray = labelAsset.text.Split('"').Where((item, index) => index % 2 != 0);
		//get every other item
		labels = stringArray.Where((x, i) => i % 2 != 0).ToArray();
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
        uiText.text = labels[index];

        //dispose tensors
        tensor.Dispose();
		outputTensor.Dispose();
		yield return null;
	}

	//transform from 0-255 to -1 to 1
	Tensor TransformInput(byte[] pixels){
		float[] transformedPixels = new float[pixels.Length];

		for (int i = 0; i < pixels.Length; i++){
			transformedPixels[i] = (pixels[i] - 127f) / 128f;
		}
		return new Tensor(1, IMAGE_SIZE, IMAGE_SIZE, 3, transformedPixels);
	}
}
