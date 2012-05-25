﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VVVV.Nodes.OpenCV
{
	public abstract class IGeneratorInstance : IInstance, IInstanceOutput, IDisposable
	{
		protected CVImageOutput FOutput;

		/// <summary>
		/// This is invalid for generators
		/// </summary>
		public override void Initialise() {}

		/// <summary>
		/// Open the device for capture. This is called from inside the thread
		/// </summary>
		protected abstract bool Open();
		/// <summary>
		/// Close the capture device. This is called from inside the thread
		/// </summary>
		protected abstract void Close();

		private bool FNeedsOpen = false;
		private bool FNeedsClose = false;
		private bool FOpen = false;

		/// <summary>
		/// Message the thread to start the capture device. This is called from outside the thread (e.g. the plugin node)
		/// </summary>
		public void Start()
		{
			FNeedsOpen = true;
		}
		/// <summary>
		/// Message the thread to stop the capture device. This is called from outside the thread (e.g. the plugin node)
		/// </summary>
		public void Stop()
		{
			FNeedsClose = true;
		}
		/// <summary>
		/// Used to restart the device (e.g. you change a setting)
		/// </summary>
		public void Restart()
		{
			FNeedsClose = true;
			FNeedsOpen = true;
		}

		override public void Process()
		{
			lock (FLockProperties)
			{
				if (FNeedsClose)
				{
					FNeedsClose = false;
					if (FOpen)
						Close();
					FEnabled = false;
					FOpen = false;
					return;
				}

				if (FNeedsOpen)
				{
					FNeedsOpen = false;
					if (FOpen)
						Close();
					FOpen = Open();
				}

				if (FOpen)
				{
					if (FOutput.Image.Allocated == false)
						ReInitialise();
					else
					{
						FOutput.Image.Timestamp = DateTime.UtcNow.Ticks - TimestampDelay * 10000;
						Generate();
					}
				}
			}
		}

		public void SetOutput(CVImageOutput output)
		{
			FOutput = output;
		}

		public int TimestampDelay = 0;

		/// <summary>
		/// For threaded generators you must override this function
		/// For non-threaded generators, you use your own function
		/// </summary>
		protected virtual void Generate() { }

		private bool FEnabled = false;
		public bool Enabled
		{
			get
			{
				return FEnabled;
			}
			set
			{
				lock (FLockProperties)
				{
					if (FEnabled == value)
						return;

					if (value)
					{

						FEnabled = true;
						Start();
					}
					else
					{
						Stop();
					}
				}
			}
		}

		override public void Dispose()
		{
			Enabled = false;
		}
	}
}
