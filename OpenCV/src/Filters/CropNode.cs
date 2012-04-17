using System;
using System.Drawing;
using Emgu.CV;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace VVVV.Nodes.OpenCV.Filters
{
	public class CropInstance : IFilterInstance
	{
		public Rectangle CropRectangle { private get; set; }
		public override void Initialise()
		{
			CropRectangle = new Rectangle(0, 0, 100, 100);

			FOutput.Image.Initialise(new Size(CropRectangle.Width, CropRectangle.Height), FInput.Image.ImageAttributes.ColourFormat);
		}

		public override void Process()
		{
			if (!FInput.LockForReading()) return;
			
			IntPtr inputPtr = FInput.Image.CvMat;
			IntPtr outputPtr = FOutput.Image.CvMat;

			CvInvoke.cvSetImageROI(inputPtr, CropRectangle);
			CvInvoke.cvCopy(inputPtr, outputPtr, IntPtr.Zero);

			CvInvoke.cvResetImageROI(inputPtr);
			
			FOutput.Send();
			FInput.ReleaseForReading();
		}
	}

	#region PluginInfo
	[PluginInfo(Name = "Crop", Category = "OpenCV", Help = "Crop image", Author = "alg", Tags = "")]
	#endregion PluginInfo
	public class CropNode :IFilterNode<CropInstance>
	{
		[Input("Crop Rectangle", DefaultValues = new double[] { 0, 0, 100, 100}, DimensionNames = new string[]{"px"})]
		private IDiffSpread<Vector4D> FCropRectangleInput; 
		
		protected override void Update(int instanceCount, bool spreadChanged)
		{
			CheckParams(instanceCount);
		}

		private void CheckParams(int instanceCount)
		{
			if(!FCropRectangleInput.IsChanged) return;

			for (int i = 0; i < instanceCount; i++)
			{
				FProcessor[i].CropRectangle = new Rectangle((int) FCropRectangleInput[i].x, (int) FCropRectangleInput[i].y, (int) FCropRectangleInput[i].z, (int) FCropRectangleInput[i].w);
			}
		}
	}
}
