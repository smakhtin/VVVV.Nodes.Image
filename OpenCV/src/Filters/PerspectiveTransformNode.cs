using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace VVVV.Nodes.OpenCV.Filters
{
	public class PerspectiveTransformInstance : IFilterInstance
	{
		public Bgr BackgroundColor { set; private get; }
		public float[] TransformationMatrix { set; get; }

		public List<PointF> SourcePoints { get; set; }

		public List<PointF> DestenationPoints { get; set; }

		public override void Initialise()
		{
			TransformationMatrix = new float[9];
			BackgroundColor = new Bgr(0, 0, 0);
			FOutput.Image.Initialise(FInput.Image.ImageAttributes);

			SourcePoints = new List<PointF>(4);

			DestenationPoints = new List<PointF>(4);

			SourcePoints.Add(new PointF(0, 0));
			SourcePoints.Add(new PointF(FInput.Image.Size.Width, 0));
			SourcePoints.Add(new PointF(0, FInput.Image.Size.Height));
			SourcePoints.Add(new PointF(FInput.Image.Size.Width, FInput.Image.Size.Height));

			DestenationPoints.Add(new PointF(0, 0));
			DestenationPoints.Add(new PointF(FInput.Image.Size.Width, 0));
			DestenationPoints.Add(new PointF(0, FInput.Image.Size.Height));
			DestenationPoints.Add(new PointF(FInput.Image.Size.Width, FInput.Image.Size.Height));
		}

		public override void Process()
		{
			FInput.GetImage(FOutput.Image);
			IImage image = FOutput.Image.GetImage();

			if (!FInput.LockForReading()) return;

			if (image is Image<Bgr, byte>)
			{
				//((Image<Bgr, byte>)image).Rotate(Rotation, BackgroundColor);
			}
			else
			{
				Gray grayColor = new Gray((BackgroundColor.Red + BackgroundColor.Green + BackgroundColor.Blue) / 3);

				PointF[] pts1 = new PointF[4];
				PointF[] pts2 = new PointF[4];
				HomographyMatrix homography;
				
				for (int i = 0; i < 4; i++)
				{
					pts1[i] = new PointF(SourcePoints[i].X, SourcePoints[i].Y);
					pts2[i] = new PointF(DestenationPoints[i].X, DestenationPoints[i].Y);
				}

				homography = CameraCalibration.GetPerspectiveTransform(pts1, pts2);
				Image<Gray, byte> transformedImage = ((Image<Gray, byte>)image).WarpPerspective(homography, INTER.CV_INTER_LINEAR, WARP.CV_WARP_DEFAULT, grayColor);
				FOutput.Image.SetImage(transformedImage);
			}

			FOutput.Send();
			FInput.ReleaseForReading();
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "PerspectiveTransform", Category = "OpenCV", Help = "Transform image", Author = "alg", Tags = "")]
	#endregion PluginInfo
	public class PerspectiveTransformNode : IFilterNode<PerspectiveTransformInstance>
	{
		[Input("Destenation Points")] private ISpread<Matrix4x4> FDestPointsInput;

		protected override void Update(int InstanceCount, bool SpreadChanged)
		{
			CheckParams(InstanceCount);
		}

		private void CheckParams(int instanceCount)
		{
			
			for (int i = 0; i < instanceCount; i++)
			{
				if (FProcessor[i].NeedsInitialise()) return;
				
				FProcessor[i].DestenationPoints[0] = new PointF((float) FDestPointsInput[0][0], (float) FDestPointsInput[0][1]);
				FProcessor[i].DestenationPoints[1] = new PointF((float)FDestPointsInput[0][2], (float)FDestPointsInput[0][3]);
				FProcessor[i].DestenationPoints[2] = new PointF((float)FDestPointsInput[0][4], (float)FDestPointsInput[0][5]);
				FProcessor[i].DestenationPoints[3] = new PointF((float)FDestPointsInput[0][6], (float)FDestPointsInput[0][7]);
			}
		}
	}

	
}
