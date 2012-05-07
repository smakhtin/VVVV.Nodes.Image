using System;
using System.Drawing;
using Emgu.CV;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.OpenCV.Filters
{
	public class GammaCorrectInstance : IFilterInstance
	{
		private readonly CVImage FGrayScaleImage = new CVImage();

		public double Gamma { private get; set; }

		public override void Initialise()
		{
			Gamma = 1.8d;
			Size size = FInput.Image.ImageAttributes.Size;

			FGrayScaleImage.Initialise(size, TColorFormat.L32F);
			FOutput.Image.Initialise(size, TColorFormat.L8);
		}

		public override void Process()
		{
			if (!FInput.LockForReading()) return;

			FInput.Image.GetImage(TColorFormat.L32F, FGrayScaleImage);
			IntPtr grayScalePtr = FGrayScaleImage.CvMat;
			CvInvoke.cvPow(grayScalePtr, grayScalePtr, Gamma);

			FGrayScaleImage.GetImage(TColorFormat.L8, FOutput.Image);
			IntPtr grayCodePtr = FOutput.Image.CvMat;
			CvInvoke.cvEqualizeHist(grayCodePtr, grayCodePtr);
			
			FInput.ReleaseForReading();
			FOutput.Send();
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "GammaCorrect", Category = "OpenCV", Version = "", Help = "Normilizes brightness and contrast of image", Author = "alg", Credits = "", Tags = "")]
	#endregion PluginInfo
	public class GammaCorrectNode : IFilterNode<GammaCorrectInstance>
	{
		[Input("Gamma", DefaultValue = 1.8)] private ISpread<double> FGammaInput;

		protected override void Update(int instanceCount, bool spreadChanged)
		{
			CheckParams(instanceCount);
		}

		private void CheckParams(int instanceCount)
		{
			for (int i = 0; i < instanceCount; i++)
			{
				FProcessor[i].Gamma = FGammaInput[i];
			}
		}
	}
}
