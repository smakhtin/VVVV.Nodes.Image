using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;

namespace VVVV.Nodes.OpenCV.Filters
{

	public class RotateInstace : IFilterInstance
	{
		public double Rotation { set; private get; }
		public Bgr BackgroundColor { set; private get; }

		public override void Initialise()
		{
			BackgroundColor = new Bgr(0, 0, 0);
			FOutput.Image.Initialise(FInput.Image.ImageAttributes);
		}

		public override void Process()
		{	
			FInput.GetImage(FOutput.Image);
			IImage image = FOutput.Image.GetImage();

			if (!FInput.LockForReading()) return;
			
			if(image is Image<Bgr, byte>)
			{
				((Image<Bgr, byte>) image).Rotate(Rotation, BackgroundColor);
			}
			else
			{
				Gray grayColor = new Gray((BackgroundColor.Red + BackgroundColor.Green + BackgroundColor.Blue) / 3);
				//Image<Gray, byte> rotatedImage = ((Image<Gray, byte>) image).Rotate(Rotation, grayColor);
				RotationMatrix2D<float> matrix = new RotationMatrix2D<float>(new PointF(FInput.Image.ImageAttributes.Width / 2, FInput.Image.ImageAttributes.Height / 2), Rotation, 1);
				CvInvoke.cvWarpAffine(FInput.Image.CvMat, FOutput.Image.CvMat, matrix, (int) WARP.CV_WARP_DEFAULT, new MCvScalar(0));
				//FOutput.Image.SetImage(rotatedImage);
			}

			FOutput.Send();
			FInput.ReleaseForReading();
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "Rotate", Category = "OpenCV", Help = "Rotate image", Author = "alg", Tags = "")]
	#endregion PluginInfo
	public class RotateNode : IFilterNode<RotateInstace>
	{
		[Input("Rotation", DefaultValue = 0)] private ISpread<double> FRotationInput;
		[Input("BgColor", DefaultColor = new double[] {0, 0, 0})] private ISpread<RGBAColor> FBgColorInput;
		
		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			CheckParams(InstanceCount);
		}

		private void CheckParams(int instanceCount)
		{
			for (int i = 0; i < instanceCount; i++)
			{
				FProcessor[i].BackgroundColor = new Bgr(FBgColorInput[i].B, FBgColorInput[i].G, FBgColorInput[i].R);
				FProcessor[i].Rotation = FRotationInput[i];
			}
		}
	}
}
