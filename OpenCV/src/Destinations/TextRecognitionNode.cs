using System;
using Emgu.CV;
using Emgu.CV.OCR;
using Emgu.CV.Structure;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.OpenCV.Destinations
{
	public class TextRecognitionInstance : IDestinationInstance
	{
		private Tesseract FImageProcessor;
		private Object FLockResult = new Object();
		//readonly CVImage FGrayScaleImage = new CVImage();

		public bool Recognise { private get; set; }
		public string RecognisedData { get; private set; }

		public override void Initialise()
		{
			//FGrayScaleImage.Initialise(FInput.Image.ImageAttributes.Size, TColourFormat.L8);
			FImageProcessor = new Tesseract(@"C:\Program Files (x86)\Tesseract-OCR\tessdata", "eng",
													 Tesseract.OcrEngineMode.OEM_DEFAULT);
			FImageProcessor.SetVariable("tessedit_pageseg_mode", "8");
			FImageProcessor.SetVariable("tessedit_char_whitelist", "0123456789");
		}

		public override void Process()
		{
			//FInput.Image.GetImage(TColourFormat.L8, FGrayScaleImage);
			Image<Gray, byte> image = (Image<Gray, byte>)FInput.Image.GetImage();
			FImageProcessor.Recognize(image);
			string data = FImageProcessor.GetText();

			if (string.IsNullOrEmpty(data)) return;

			RecognisedData = data;
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "TextRecognition", Category = "OpenCV", Version = "", Help = "Recognise text in images", Tags = "")]
	#endregion PluginInfo
	public class TextRecognitionNode : IDestinationNode<TextRecognitionInstance>
	{
		[Input("Recognise", IsBang = true, IsSingle = true)]
		private ISpread<bool> FRecogniseInput;
		[Output("Recognised Text")]
		private ISpread<string> FRecognisedTextOutput;

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			CheckParams(InstanceCount);
			Output(InstanceCount);
		}

		private void Output(int instanceCount)
		{
			for (int i = 0; i < instanceCount; i++)
			{
				FRecognisedTextOutput[i] = FProcessor[i].RecognisedData;
			}
		}

		private void CheckParams(int instanceCount)
		{
			for (int i = 0; i < instanceCount; i++)
			{
				FProcessor[i].Recognise = FRecogniseInput[i];
			}
		}
	}
}
