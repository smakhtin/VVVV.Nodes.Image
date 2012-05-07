#region usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using LibVlcWrapper;
using SlimDX.Direct3D9;
using VVVV.Core.Logging;
using VVVV.Nodes.OpenCV;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.SlimDX;

#endregion usings

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate IntPtr VlcLockHandlerDelegate(ref IntPtr data, ref IntPtr pixels);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate void VlcUnlockHandlerDelegate(ref IntPtr data, ref IntPtr id, ref IntPtr pixels);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate void VlcDisplayHandlerDelegate(ref IntPtr data, ref IntPtr id);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
delegate void VlcEventHandlerDelegate(ref libvlc_event_t libvlc_event, IntPtr userData);


namespace VVVV.Nodes.EmguCV
{
	#region PluginInfo
	[PluginInfo(Name = "Vlc", Category = "EmguCV", Version = "0.3", Help = "Vlc video player for CVImageLink ecosystem", Tags = "")]
	#endregion PluginInfo
	public class VlcNode : IPluginEvaluate, IDisposable
	{
		#region pins
		[Input("Filename", DefaultString = "C:\\video.avi | deinterlace=1 | video-filter=gradient{type=1}")]
		IDiffSpread<string> FFileNameIn;

		[Input("NextFilename", DefaultString = "")]
		IDiffSpread<string> FNextFileNameIn;

		[Input("Seek Time", DefaultValue = 0)]
		IDiffSpread<float> FSeekTimeIn;

		[Input("Do Seek", DefaultValue = 0, IsBang = true)]
		IDiffSpread<bool> FDoSeekIn;

		[Input("Play", DefaultValue = 1)]
		IDiffSpread<bool> FPlayIn;

		[Input("Speed", DefaultValue = 1)]
		IDiffSpread<float> FSpeedIn;

		[Input("Loop", DefaultValue = 0)]
		IDiffSpread<bool> FLoopIn;

		[Input("Rotate", DefaultValue = 0, Visibility = PinVisibility.False)]
		IDiffSpread<int> FRotateIn;

		[Input("Forced Width", DefaultValue = 0)]
		IDiffSpread<int> FWidthIn;

		[Input("Forced Height", DefaultValue = 0)]
		IDiffSpread<int> FHeightIn;

		[Input("Volume", DefaultValue = 1)]
		IDiffSpread<float> FVolumeIn;

		[Output("Position")]
		ISpread<float> FPositionOut;

		[Output("Duration")]
		ISpread<float> FDurationOut;

		[Output("Frame")]
		ISpread<int> FFrameOut;

		[Output("FrameCount")]
		ISpread<int> FFrameCountOut;

		[Output("Width")]
		ISpread<int> FWidthOut;

		[Output("Height")]
		ISpread<int> FHeightOut;

		[Output("Texture Aspect Ratio", DefaultValue = 1)]
		ISpread<float> FTextureAspectRatioOut;

		[Output("Pixel Aspect Ratio", DefaultValue = 1)]
		ISpread<float> FPixelAspectRatioOut;

		[Output("Next Ready")]
		ISpread<bool> FNextReadyOut;

		[Output("Image")]
		ISpread<CVImageLink> FImageOut;
		#endregion pins

		#region private classes
		private class MediaRenderer : IDisposable
		{
			#region MediaRenderer fields
			//needed to access pins (at the right slice)
			private VlcNode parent;
			private int mediaRendererIndex = 0; //slice index


			private IntPtr libVLC = IntPtr.Zero;

			string currFileNameIn;
			string newFileNameIn = ""; //COPY OF CURRFILENAMEIN FOR USING IN THE (THREADED) UpdateMediaPlayerStatus
			string prevFileNameIn;
			bool currPlayIn;
			bool currLoopIn;
			float currSpeedIn;
			float currSeekTimeIn;
			bool currDoSeekIn;
			int currRotateIn;
			int currWidthIn;
			int currHeightIn;
			float currVolumeIn;

			Thread evaluateThread; //will work when signalled by evaluateEventWaitHandle
			private EventWaitHandle evaluateEventWaitHandle;
			private EventWaitHandle evaluateStopThreadWaitHandle;
			private Mutex mediaPlayerBusyMutex; //used for starting and stopping etc. in separate thread

			private IntPtr media;
			private IntPtr preloadMedia;
			private IntPtr mediaPlayer;

			private IntPtr opaqueForCallbacks;

			private CVImageLink imageAandB = new CVImageLink();

			private int preloadingStatus;
			private const int STATUS_INACTIVE = -11;
			private const int STATUS_NEWFILE = -10;
			private const int STATUS_OPENINGFILE = -9;
			private const int STATUS_GETPROPERTIES = -8;
			private const int STATUS_GETPROPERTIESOK = -7;
			private const int STATUS_GETFIRSTFRAME = -6;
			private const int STATUS_IMAGE = -1;
			private const int STATUS_READY = 0;
			private const int STATUS_PLAYING = 1;

			private int videoWidth;
			private int videoHeight;
			private float videoLength;
			private float videoFps;

			private Mutex decodeLock; //lock used for decoding => locks the writePixelPlane
			private int displayCalled = 0; //how many times display has been called
			private int prevDisplayCalled = 0; //last value of display when rendering
			private int lockCalled = 0; //how many times LOCK has been called
			private int unlockCalled = 0; //how many times UNLOCK has been called
			private int preloadDisplayCalled = 0;
			private int currentFrame = 0; //current video frame that has been decoded

			//VLC options
			//make sure garbage collector doesn't remove this
			private VlcLockHandlerDelegate vlcLockHandlerDelegate;
			private VlcUnlockHandlerDelegate vlcUnlockHandlerDelegate;
			private VlcDisplayHandlerDelegate vlcDisplayHandlerDelegate;

			#endregion MediaRenderer fields

			#region MediaRenderer constructor/destructor
			public MediaRenderer(VlcNode parentObject, int index)
			{
				parent = parentObject;
				mediaRendererIndex = index;

				libVLC = parent.FLibVLC; //LibVlcMethods.libvlc_new(parent.argv.GetLength(0), parent.argv);	//argc, argv

				PrepareMediaPlayer();
			}

			~MediaRenderer()
			{
				Dispose();
			}

			private void PrepareMediaPlayer()
			{

				vlcLockHandlerDelegate = VlcLockCallBack;
				vlcUnlockHandlerDelegate = VlcUnlockCallBack;
				vlcDisplayHandlerDelegate = VlcDisplayCallBack;

				opaqueForCallbacks = Marshal.AllocHGlobal(4);

				media = new IntPtr();
				preloadMedia = new IntPtr();
				try
				{
					mediaPlayer = LibVlcMethods.libvlc_media_player_new(libVLC);
					LibVlcMethods.libvlc_media_player_retain(mediaPlayer);
				}
				catch (Exception)
				{
					
				}
				

				//CREATE A THREAD THAT WILL TRY TO LOAD NEW FILES ETC. 
				//when signalled by evaluateEventWaitHandle
				evaluateEventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
				evaluateStopThreadWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
				evaluateThread = new Thread(EvaluateThreadProc);
				evaluateThread.Start();

				//this mutex will protect the mediaPlayer when accessed by different threads
				mediaPlayerBusyMutex = new Mutex();

				decodeLock = new Mutex();

				preloadingStatus = STATUS_INACTIVE;
				videoWidth = 2;
				videoHeight = 2;

				videoLength = 0;
				videoFps = 1;

				CreateNewPixelPlanesAandB(videoWidth, videoHeight);

				preloadingStatus = STATUS_NEWFILE;
			}

			public void Dispose()
			{
				//parent.FLogger.Log(LogType.Debug, "[Dispose] Disposing media renderer " + mediaRendererIndex);
				preloadingStatus = STATUS_INACTIVE;
				evaluateStopThreadWaitHandle.Set();
				evaluateThread.Join();

				try { LibVlcMethods.libvlc_media_player_stop(mediaPlayer); }
				catch { }
				try { LibVlcMethods.libvlc_media_player_release(mediaPlayer); }
				catch { }

				//deallocate video memory
				decodeLock.WaitOne();
				try
				{
					Marshal.FreeHGlobal(opaqueForCallbacks);
				}
				catch { }
				decodeLock.ReleaseMutex();
			}
			#endregion MediaRenderer constructor/destructor

			public void Evaluate(bool active)
			{
				//Log(LogType.Debug, "[Evaluate Called] for " + (active ? "FRONT " : "BACK ") + "renderer " + mediaRendererIndex);

				//				if (evaluateCalled < 10) {
				//					evaluateCalled++;
				//					return;
				//				}

				try
				{

					if (GetFileNameIn(true).IsChanged || GetFileNameIn(false).IsChanged)
					{
						//prevFileNameIn = currFileNameIn;
						currFileNameIn = GetFileNameIn(active)[mediaRendererIndex];

						if (currFileNameIn == null)
						{
							Log(LogType.Debug, (active ? "FileNameIn" : "NextFileNameIn") + "[" + mediaRendererIndex + "] IS NULL!");
							currFileNameIn = "";
						}

						if (currFileNameIn.Length > 0)
						{
							string[] splitFileName = currFileNameIn.Split("|".ToCharArray());
							//Log( LogType.Debug, "Path = " + currFileNameIn );

							currFileNameIn = GetFullPath(splitFileName[0]);
							for (int i = 1; i < splitFileName.GetLength(0); i++)
							{
								currFileNameIn += "|" + splitFileName[i];
							}
							//Log( LogType.Debug, "FULL Path = " + currFileNameIn );
						}
					}

					currPlayIn = IsPlaying(active);
					currLoopIn = parent.FLoopIn[mediaRendererIndex];
					currSpeedIn = parent.FSpeedIn[mediaRendererIndex];
					if (parent.FDoSeekIn[mediaRendererIndex])
					{
						currSeekTimeIn = parent.FSeekTimeIn[mediaRendererIndex];
						currDoSeekIn = true;
					}
					currRotateIn = parent.FRotateIn[mediaRendererIndex];
					currWidthIn = parent.FWidthIn[mediaRendererIndex];
					currHeightIn = parent.FHeightIn[mediaRendererIndex];
					currVolumeIn = parent.FVolumeIn[mediaRendererIndex];


					// -------- FOR REPORT ELAPSED TIME --------
					prevTime = DateTime.Now.Ticks;

					//Log( LogType.Debug, "Evaluate_Threaded( " + active + " )");
					Evaluate_Threaded(active);

					//Log( LogType.Debug, "UpdateParent( " + active + " )");
					UpdateParent(active);

				}
				catch (Exception e)
				{
					Log(LogType.Error, "[MediaRenderer Evaluate Exception] " + e.Message + "\n\n" + e.StackTrace);
				}

			}

			#region MediaRenderer Vlc Callback functions

			//////////////////////////////////////////////////
			// Next 3 functions are used for PLAYING the video
			//////////////////////////////////////////////////
			public IntPtr VlcLockCallBack(ref IntPtr data, ref IntPtr pixelPlane)
			{
				//if (lockCalled != unlockCalled) Log(LogType.Error, (parent.IsFrontMediaRenderer(this) ? "FRONT " : "BACK ") + "(lock/unlock=" + lockCalled  + "/" + unlockCalled + ")" );

				try
				{
					lockCalled++;
					pixelPlane = imageAandB.BackImage.Data; //writePixelPlane;

					//if (data.ToInt32() < 0) {
					//	Log( LogType.Error, ("VlcLockCallback(" + data.ToInt32() + ") : Hoe kan data nu < 0 zijn allee? Heeft er iemand in zitten schrijven?") );
					//}

				}
				catch (Exception e)
				{
					Log(LogType.Error, ("[VlcLockCallback(" + data.ToInt32() + ") Exception] " + e.Message));
				}

				decodeLock.WaitOne();

				return pixelPlane;
			}

			public void VlcUnlockCallBack(ref IntPtr data, ref IntPtr id, ref IntPtr pixelPlane)
			{
				try
				{
					// VLC just rendered the video (RGBA), but we can also render stuff
					///////////////////////////////////////////////////////////////////
					unlockCalled++;

					//if (data.ToInt32() < 0) {
					//	Log( LogType.Error, ("VlcUnlockCallback(" + data.ToInt32() + ") : Hoe kan data nu < 0 zijn allee? Heeft er iemand in zitten schrijven?") );
					//}
				}
				catch (Exception e)
				{
					Log(LogType.Error, ("[VlcUnlockCallback(" + data.ToInt32() + ") Exception] " + e.Message));
				}

				decodeLock.ReleaseMutex();
			}

			public void VlcDisplayCallBack(ref IntPtr data, ref IntPtr id)
			{
				try
				{
					if (preloadingStatus == STATUS_GETFIRSTFRAME)
					{
						preloadDisplayCalled++;
						AllowDisplay(data);

						//Log(LogType.Debug, (parent.IsFrontMediaRenderer(this) ? "FRONT " : "BACK ") + "[VlcDisplayCallBack] Setting STATUS_READY (from VlcDisplayCallback)");
						preloadingStatus = STATUS_READY;
						//Log(LogType.Debug, (parent.IsFrontMediaRenderer(this) ? "FRONT " : "BACK ") + "[VlcDisplayCallBack] Setting STATUS_READY (from VlcDisplayCallback) DONE !!!");
					}
					else if (preloadingStatus == STATUS_PLAYING)
					{
						// VLC wants to display the video
						displayCalled++;

						AllowDisplay(data);
					}
				}
				catch (Exception e)
				{
					Log(LogType.Error, ("[VlcDisplayCallback(" + data.ToInt32() + ") Exception] " + e.Message));
				}
			}

			private void AllowDisplay(IntPtr data)
			{
				imageAandB.Swap();
			}

			#endregion MediaRenderer Vlc Callback functions

			private void EvaluateThreadProc()
			{
				while (true)
				{
					int waitHandleIndex = WaitHandle.WaitAny(new EventWaitHandle[2] { evaluateEventWaitHandle, evaluateStopThreadWaitHandle });

					if (waitHandleIndex == 0)
					{
						//Log( (evaluateCurrentActiveParameter ? "[signalled FRONT player] " : "[signalled BACK player] ") );
						UpdateMediaPlayerStatus_Threaded(null);
						//Thread.Sleep(2);
					}
					else if (waitHandleIndex == 1)
					{
						break;
					}
				}
				//Log(LogType.Debug, "... exiting evaluate thread for renderer " + mediaRendererIndex + " ... " );				
			}

			private void VlcEventHandler(ref libvlc_event_t libvlc_event, IntPtr userData)
			{
				Log(LogType.Debug, "======== VLC SENT A " + libvlc_event.ToString() + " SIGNAL ======");
				evaluateEventWaitHandle.Set();
			}

			private void Evaluate_Threaded(Boolean active)
			{
				//ONE METHOD WOULD BE TO DO SOME STUFF USING THE THREADPOOL
				//ThreadPool.QueueUserWorkItem( UpdateMediaPlayerStatus_Threaded, active );

				//BUT THE BETTER CHOICE I THINK IS TO SIGNAL A RUNNING THREAD
				// (because we can signal it at any time we need)
				evaluateEventWaitHandle.Set();

				//FOR TESTING -> NO THREADS
				//UpdateMediaPlayerStatus( );

			}

			private void UpdateMediaPlayerStatus_Threaded(object active)
			{
				if (mediaPlayerBusyMutex.WaitOne())
				{
					//ReportElapsedTime("locking mediaPlayerBusyMutex");

					UpdateMediaPlayerStatus();

					mediaPlayerBusyMutex.ReleaseMutex();
					//ReportElapsedTime("releasing mediaPlayerBusyMutex");
				}
			}

			private int test = 0;
			private bool isStream;
			private void UpdateMediaPlayerStatus()
			{
				///////////////////////////////////////////////////////////////////////
				// !!! NEVER SURROUND ANY LibVLC calls with 
				// parent.LockBackFrontMediaRenderer(mediaRendererIndex); 
				// and 
				// parent.UnlockBackFrontMediaRenderer(mediaRendererIndex);
				//
				// because this lock is needed in the display function, and if 
				// that blocks, the call (for example media_player_stop) will block too
				///////////////////////////////////////////////////////////////////////

				//Log(LogType.Debug, (parent.IsFrontMediaRenderer(this) ? "FRONT " : "BACK ") + "[UpdateMediaPlayerStatus BEGIN] "  + StatusToString(preloadingStatus) + " " + currFileNameIn);

				try
				{
					//stop player if in error
					if (LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == LibVlcWrapper.libvlc_state_t.libvlc_Error)
					{
						Log(LogType.Debug, "LibVlc STATUS = " + LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) + " Trying to stop mediaPlayer...");
						LibVlcMethods.libvlc_media_player_stop(mediaPlayer);
					}

					//then set everything right
					int w = 2;
					int h = 2;
					/*if ( currFileNameIn.Length == 0 ) {
						parent.FNextReadyOut[mediaRendererIndex] = true;
						if (	LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_Playing 
							 || LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_Paused 
							 || LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_Ended 
							 || LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_Error) {
							
							Log( LogType.Debug, "Filename empty, STOP mediaPlayer" + (this == parent.mediaRendererA ? "A " : "B ") + (this == parent.mediaRendererCurrent[mediaRendererIndex] ? "(FRONT) " : "(BACK) " ) + currFileNameIn );
							LibVlcMethods.libvlc_media_player_stop(mediaPlayer);
							Log( LogType.Debug, ( LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_Stopped ? "STOPPED!!!" : "" ) );
						}
					}
					else */

					if ((currFileNameIn != prevFileNameIn)
							&& (prevFileNameIn == null || prevFileNameIn.CompareTo(currFileNameIn) != 0)
					   )
					{
						newFileNameIn = string.Copy(currFileNameIn);
						preloadingStatus = STATUS_NEWFILE;
					}

					if (preloadingStatus == STATUS_NEWFILE || (preloadingStatus == STATUS_OPENINGFILE && newFileNameIn.Length > 0))
					{
						//Log(LogType.Debug, "Trying to load " + newFileNameIn + "...");

						parent.LockBackFrontMediaRenderer(mediaRendererIndex);
						if (!parent.IsFrontMediaRenderer(this)) parent.FNextReadyOut[mediaRendererIndex] = (newFileNameIn.Length == 0);
						parent.UnlockBackFrontMediaRenderer(mediaRendererIndex);


						try
						{
							prevFileNameIn = newFileNameIn;

							if (LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_Playing
								 || LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_Paused
								 || LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_Ended
								 || LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_Error)
							{
								//Log( LogType.Debug, "Calling STOP first");
								LibVlcMethods.libvlc_media_player_stop(mediaPlayer);
								//Log( LogType.Debug, "STOPPED...");
							}
						}
						catch (Exception e)
						{
							Log(LogType.Error, "[Evaluate PRELOAD Exception 1] " + e.Message);
						}
						try
						{
							//|| LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_Ended
							//|| LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_Error //maybe previous file empty or non-existant
							if (LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_NothingSpecial
								 || LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_Stopped)
							{

								if (IsImageFileName(newFileNameIn))
								{
									//Log(LogType.Debug, "Trying to load image '" + newFileNameIn + "'");
									LoadImage(newFileNameIn);
									preloadingStatus = STATUS_IMAGE;
								}
								else
								{
									//Log(LogType.Debug, "Trying to load VIDEO '" + newFileNameIn + "'");

									//example: c:\video.avi | video-filter=adjust {           hue=120 ,          gamma=2.} | video-filter=gradient{type=1}
									//         filename     | option              {optionflagname=optionflagvalue, ...   }
									preloadMedia = ParseFilename(newFileNameIn);

									string[] tmp = newFileNameIn.Split("|".ToCharArray());
									isStream = tmp.Length > 0 && tmp[0].Length > 0 && tmp[0].Contains("://");
									//isStream = true;

									if (preloadMedia != IntPtr.Zero)
									{
										//only get the file's description without actually playing it
										LibVlcMethods.libvlc_media_add_option(preloadMedia, "sout=#description");

										LibVlcMethods.libvlc_media_player_set_media(mediaPlayer, preloadMedia);
										LibVlcMethods.libvlc_media_player_play(mediaPlayer);

										//Log(LogType.Debug, "SETTING STATUS_GETPROPERTIES");
										preloadingStatus = STATUS_GETPROPERTIES;
									}
									//else {
									//	Log( LogType.Debug, "Error opening file: " + newFileNameIn );
									//}
								}
							}
							else
							{
								preloadingStatus = STATUS_OPENINGFILE;

								if (LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_Buffering)
								{
									Log(LogType.Debug, "=== BUFFERING");
								}
								else if (LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_Opening)
								{
									Log(LogType.Debug, "=== OPENING");
								}
							}
						}
						catch (Exception e)
						{
							Log(LogType.Error, "[Evaluate PRELOAD Exception] " + e.Message);
						}
					}
					else if ((preloadingStatus == STATUS_GETPROPERTIES)
							 && ((LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_Ended)
								 || (LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_Stopped)
								 || (isStream && (LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_Playing)))
							)
					{
						//Log(LogType.Debug, "STATUS_GETPROPERTIES");
						try
						{
							unsafe
							{
								IntPtr trackInfoArray;
								int nrOfStreams = LibVlcMethods.libvlc_media_get_tracks_info(preloadMedia, out trackInfoArray);

								bool hasAudio = false;
								bool hasVideo = false;
								//Log(LogType.Debug, "streams " + nrOfStreams + " trackInfo size = " + sizeof(LibVlcWrapper.libvlc_media_track_info_t) );
								for (int i = 0; i < nrOfStreams; i++)
								{
									LibVlcWrapper.libvlc_media_track_info_t trackInfo = ((LibVlcWrapper.libvlc_media_track_info_t*)trackInfoArray)[i];


									if (trackInfo.i_type == LibVlcWrapper.libvlc_track_type_t.libvlc_track_audio)
									{
										hasAudio = true;
										//Log(LogType.Debug, "Detected AUDIO track with samplerate " + trackInfo.audio.i_rate + " and " + trackInfo.audio.i_channels + " channels");
									}
									else if (!hasVideo && trackInfo.i_type == LibVlcWrapper.libvlc_track_type_t.libvlc_track_video)
									{
										hasVideo = true;
										w = trackInfo.video.i_width;
										h = trackInfo.video.i_height;
										//Log(LogType.Debug, "Detected VIDEO track with size " + w + "x" + h);
									}
									else if (trackInfo.i_type == LibVlcWrapper.libvlc_track_type_t.libvlc_track_text)
									{
										Log(LogType.Debug, "Detected TEXT track with size " + trackInfo.video.i_width + "x" + trackInfo.video.i_height);
									}
									else if (trackInfo.i_type == LibVlcWrapper.libvlc_track_type_t.libvlc_track_unknown)
									{
										Log(LogType.Debug, "Detected UNKNOWN track with size " + trackInfo.video.i_width + "x" + trackInfo.video.i_height);
									}
								}
								if (nrOfStreams > 0)
								{
									Marshal.DestroyStructure(trackInfoArray, typeof(LibVlcWrapper.libvlc_media_track_info_t*));
								}

								if (hasAudio || hasVideo)
								{
									try { LibVlcMethods.libvlc_media_release(media); }
									catch { }
									try { LibVlcMethods.libvlc_media_release(preloadMedia); }
									catch { }
									preloadMedia = ParseFilename(newFileNameIn);
									media = ParseFilename(newFileNameIn);
									if (media != IntPtr.Zero)
									{
										if (hasAudio && !hasVideo)
										{
											//Log(LogType.Debug, "AUDIO only -> start playing");

											videoLength = LibVlcMethods.libvlc_media_player_get_length(mediaPlayer) / 1000;
											videoFps = -1;

											LibVlcMethods.libvlc_media_player_stop(mediaPlayer);

											try
											{
												parent.LockBackFrontMediaRenderer(mediaRendererIndex);
												UpdateVideoSize(2, 2, parent.IsFrontMediaRenderer(this));
												parent.UnlockBackFrontMediaRenderer(mediaRendererIndex);
											}
											catch (Exception e)
											{
												Log(LogType.Error, "[UpdateMediaPlayerStatus UpdateVideoSize (audio only) Exception] " + e.Message);
												parent.UnlockBackFrontMediaRenderer(mediaRendererIndex);
											}

											//parent.currentFillTextureFunction = TransparentFillTexure;
											lockCalled = 0; unlockCalled = 0; displayCalled = 0;
											//reset "frames drawn"
											//Log(LogType.Debug, "Calling PLAY (after getting properties the right way)");
											UpdateVolume();

											if (currPlayIn)
											{
												try { LibVlcMethods.libvlc_media_release(preloadMedia); }
												catch { }
												// ! clean up
												LibVlcMethods.libvlc_media_player_set_media(mediaPlayer, media);
												LibVlcMethods.libvlc_media_player_set_pause(mediaPlayer, 0);
												LibVlcMethods.libvlc_media_player_play(mediaPlayer);
												//Log(LogType.Debug, "SETTING STATUS_PLAYING");
												preloadingStatus = STATUS_PLAYING;
											}
											else
											{
												LibVlcMethods.libvlc_media_add_option(preloadMedia, "no-audio"); //dshow-adev=none
												LibVlcMethods.libvlc_media_player_set_media(mediaPlayer, preloadMedia);

												LibVlcMethods.libvlc_media_player_set_pause(mediaPlayer, 1);
												LibVlcMethods.libvlc_media_player_play(mediaPlayer);
												//Log(LogType.Debug, "SETTING STATUS_READY");
												preloadingStatus = STATUS_READY;
											}
										}
										else if (hasVideo)
										{
											//Log(LogType.Debug, "VIDEO " + (hasAudio ? "(+ AUDIO)" : "") + "-> start playing! " + LibVlcMethods.libvlc_video_get_aspect_ratio(mediaPlayer));
											//string ar = LibVlcMethods.libvlc_video_get_aspect_ratio( mediaPlayer );
											//Log( LogType.Debug, "video.width = " + videoWidth + " height = " + videoHeight  + " ar = " + ar + " scale = " + LibVlcMethods.libvlc_video_get_scale( mediaPlayer ) );

											videoLength = LibVlcMethods.libvlc_media_player_get_length(mediaPlayer) / 1000;
											videoFps = LibVlcMethods.libvlc_media_player_get_fps(mediaPlayer);
											//Log(LogType.Debug, "video length = " + videoLength + " fps=" + videoFps);

											LibVlcMethods.libvlc_media_player_stop(mediaPlayer);

											LibVlcMethods.libvlc_media_player_set_media(mediaPlayer, media);

											LibVlcMethods.libvlc_video_set_callbacks(mediaPlayer, Marshal.GetFunctionPointerForDelegate(vlcLockHandlerDelegate), Marshal.GetFunctionPointerForDelegate(vlcUnlockHandlerDelegate), Marshal.GetFunctionPointerForDelegate(vlcDisplayHandlerDelegate), opaqueForCallbacks);

											try
											{
												//Log(LogType.Debug, "try to update video size...");
												parent.LockBackFrontMediaRenderer(mediaRendererIndex);
												//Log(LogType.Debug, "REALLY try to update video size...");

												Size newVideoSize = GetWantedSize(w, h);
												UpdateVideoSize(newVideoSize.Width, newVideoSize.Height, parent.IsFrontMediaRenderer(this));

												parent.UnlockBackFrontMediaRenderer(mediaRendererIndex);
												//Log(LogType.Debug, "finished update video size...");
											}
											catch (Exception e)
											{
												Log(LogType.Error, "[UpdateMediaPlayerStatus UpdateVideoSize (audio only) Exception] " + e.Message);
												parent.UnlockBackFrontMediaRenderer(mediaRendererIndex);
											}

											//UpdateRotation();
											lockCalled = 0; unlockCalled = 0; displayCalled = 0;
											//reset "frames drawn"
											//Log(LogType.Debug, "Calling PLAY -> getfirstframe (after getting properties the right way)");
											if (currPlayIn)
											{
												try { LibVlcMethods.libvlc_media_release(preloadMedia); }
												catch { }
												// ! clean up
												LibVlcMethods.libvlc_media_player_set_media(mediaPlayer, media);
												LibVlcMethods.libvlc_media_player_set_pause(mediaPlayer, 0);
												LibVlcMethods.libvlc_media_player_play(mediaPlayer);
												//Log(LogType.Debug, "SETTING STATUS_PLAYING");
												preloadingStatus = STATUS_PLAYING;
											}
											else
											{
												LibVlcMethods.libvlc_media_add_option(preloadMedia, "no-audio"); //dshow-adev=none
												LibVlcMethods.libvlc_media_player_set_media(mediaPlayer, preloadMedia);

												LibVlcMethods.libvlc_media_player_set_pause(mediaPlayer, 1);
												LibVlcMethods.libvlc_media_player_play(mediaPlayer);

												//Log(LogType.Debug, "SETTING STATUS_GETFIRSTFRAME");
												preloadingStatus = STATUS_GETFIRSTFRAME;
											}
											//LibVlcMethods.libvlc_media_player_next_frame(mediaPlayer);
										}
									}
								}
							}
						}
						catch (Exception e)
						{
							Log(LogType.Error, "[UpdateMediaPlayerStatus GetProperties Exception] " + e.Message);
						}
					}
					/* DEBUG
					else if ( preloadingStatus == STATUS_GETPROPERTIES ) {
						libvlc_state_t state = LibVlcMethods.libvlc_media_player_get_state(mediaPlayer);
						string stateDescription = "unknown";
						switch (state) {
							case libvlc_state_t.libvlc_Buffering: stateDescription = "buffering"; break;
							case libvlc_state_t.libvlc_Ended: stateDescription = "ended"; break;
							case libvlc_state_t.libvlc_Error: stateDescription = "error"; break;
							case libvlc_state_t.libvlc_NothingSpecial: stateDescription = "nothing special"; break;
							case libvlc_state_t.libvlc_Opening: stateDescription = "opening"; break;
							case libvlc_state_t.libvlc_Paused: stateDescription = "paused"; break;
							case libvlc_state_t.libvlc_Playing: stateDescription = "playing"; break;
							case libvlc_state_t.libvlc_Stopped: stateDescription = "stopped"; break;
						}
						Log(LogType.Debug, "STATUS_GETPROPERTIES but libvlc_media_player_get_state != ended or playing. It's " + stateDescription);
					}
					*/
					else if (preloadingStatus == STATUS_GETFIRSTFRAME)
					{
						//Log(LogType.Debug, "STATUS_GETFIRSTFRAME: set to ready in VlcCallback functions!!!");
					}
					else if (preloadingStatus == STATUS_READY)
					{

						parent.LockBackFrontMediaRenderer(mediaRendererIndex);
						if (!parent.IsFrontMediaRenderer(this)) parent.FNextReadyOut[mediaRendererIndex] = true;
						parent.UnlockBackFrontMediaRenderer(mediaRendererIndex);

						//Log(LogType.Debug, "STATUS_READY");
						//at this stage we have been playing preloadMedia with the "noaudio" option, so set the media to media here!!!
						try
						{
							if (LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_Playing
								 || LibVlcMethods.libvlc_media_player_get_state(mediaPlayer) == libvlc_state_t.libvlc_Paused)
							{

								LibVlcMethods.libvlc_media_player_set_pause(mediaPlayer, 1);

								if (prevDisplayCalled != displayCalled)
								{
									prevDisplayCalled = displayCalled;
								}

								if (currPlayIn)
								{
									//Log(LogType.Debug, "Still on pause after getting first frame, but now we want to start playing !!!");
									try { LibVlcMethods.libvlc_media_release(preloadMedia); }
									catch { } // ! clean up
									LibVlcMethods.libvlc_media_player_set_media(mediaPlayer, media);

									lockCalled = 0; unlockCalled = 0; displayCalled = 0; //reset "frames drawn"
									UpdateVolume();
									UpdateSpeed();

									LibVlcMethods.libvlc_media_player_play(mediaPlayer);
									preloadingStatus = STATUS_PLAYING;

									LibVlcMethods.libvlc_audio_set_mute(mediaPlayer, false);
									UpdateVolume();
									UpdateSpeed();
								}
							}
						}
						catch (Exception e)
						{
							Log(LogType.Error, "[Evaluate READY FOR PLAYING Exception] " + e.Message);
						}
					}


					if (parent.IsFrontMediaRenderer(this) && preloadingStatus == STATUS_PLAYING)
					{

						//Log(LogType.Debug, "STATUS_PLAYING");
						try
						{
							libvlc_state_t mediaPlayerState = LibVlcMethods.libvlc_media_player_get_state(mediaPlayer);
							if (mediaPlayerState == libvlc_state_t.libvlc_Playing)
							{
								if (prevDisplayCalled != displayCalled)
								{
									prevDisplayCalled = displayCalled;
								}
							}
							else if (mediaPlayerState == libvlc_state_t.libvlc_Paused)
							{
								displayCalled = prevDisplayCalled;
							}

							if ((currPlayIn && mediaPlayerState == libvlc_state_t.libvlc_Paused)
								 || (!currPlayIn && mediaPlayerState == libvlc_state_t.libvlc_Playing))
							{

								LibVlcMethods.libvlc_media_player_set_pause(mediaPlayer, currPlayIn ? 0 : 1);
							}

							if (mediaPlayerState == libvlc_state_t.libvlc_Paused
								 || mediaPlayerState == libvlc_state_t.libvlc_Playing
								 || mediaPlayerState == libvlc_state_t.libvlc_Ended)
							{

								if (currDoSeekIn)
								{
									//float relativePosition = currSeekTimeIn * 1000 / LibVlcMethods.libvlc_media_get_duration(media);

									lockCalled = (int)(currSeekTimeIn * videoFps); unlockCalled = lockCalled; displayCalled = lockCalled;

									//Log( LogType.Debug, "Seeking to position RELATIVE = " + (currSeekTimeIn*1000) + "/" + LibVlcMethods.libvlc_media_get_duration(media) + "=" + relativePosition + " ABSOLUTE = " + currSeekTimeIn + (0 == LibVlcMethods.libvlc_media_player_is_seekable(mediaPlayer) ? " NOT seekable" : "") );
									if (0 != LibVlcMethods.libvlc_media_player_is_seekable(mediaPlayer))
									{
										if (mediaPlayerState == libvlc_state_t.libvlc_Ended)
										{
											LibVlcMethods.libvlc_media_player_stop(mediaPlayer);
											LibVlcMethods.libvlc_media_player_play(mediaPlayer);
											UpdateVolume();
											UpdateSpeed();
										}
										//LibVlcMethods.libvlc_media_player_set_position(mediaPlayer, relativePosition );
										LibVlcMethods.libvlc_media_player_set_time(mediaPlayer, (long)(currSeekTimeIn * 1000));
									}
									currDoSeekIn = false;
								}

								UpdateVolume();
								UpdateSpeed();
							}


							// || mediaPlayerState == libvlc_state_t.libvlc_Playing
							if ((mediaPlayerState == libvlc_state_t.libvlc_Ended) && currLoopIn)
							{
								LibVlcMethods.libvlc_media_player_stop(mediaPlayer);
								lockCalled = 0; unlockCalled = 0; displayCalled = 0;
								LibVlcMethods.libvlc_media_player_play(mediaPlayer);
								UpdateVolume();
								UpdateSpeed();
								//LibVlcMethods.libvlc_media_player_set_position( mediaPlayer, 0 );
							}

							//LibVlcMethods.libvlc_video_set_int(mediaPlayer, "adjust", null, 1);
							//if ( test++ > 100) { test = 0; }
							//LibVlcMethods.libvlc_video_set_int(mediaPlayer, "adjust", "hue", test);
						}
						catch (Exception e)
						{
							Log(LogType.Error, "[Evaluate PLAYING Exception] " + e.Message);
						}
					}

				}
				catch (Exception e)
				{
					Log(LogType.Error, "[UpdateMediaPlayerStatus Exception] " + e.Message);
				}

				//Log(LogType.Debug, (parent.IsFrontMediaRenderer(this) ? "FRONT " : "BACK ") + "[UpdateMediaPlayerStatus END] "  + StatusToString(preloadingStatus) + " " + newFileNameIn);

			}

			private void UpdateParent(bool active)
			{
				try
				{
					if (active && preloadingStatus == STATUS_PLAYING)
					{
						if (mediaPlayerBusyMutex.WaitOne(0))
						{
							try
							{
								libvlc_state_t mediaPlayerState = LibVlcMethods.libvlc_media_player_get_state(mediaPlayer);

								if (mediaPlayerState == libvlc_state_t.libvlc_Playing
									 || mediaPlayerState == libvlc_state_t.libvlc_Paused
									 || mediaPlayerState == libvlc_state_t.libvlc_Ended
								)
								{

									try
									{
										videoFps = LibVlcMethods.libvlc_media_player_get_fps(mediaPlayer);
										//float relativePosition = currentFrame / videoFps / ( (float)LibVlcMethods.libvlc_media_player_get_time(mediaPlayer) / 1000 ); //LibVlcMethods.libvlc_media_player_get_position(mediaPlayer);
										float absolutePosition = currentFrame / videoFps; //(float)LibVlcMethods.libvlc_media_player_get_time(mediaPlayer) / 1000;
										parent.FPositionOut[mediaRendererIndex] = absolutePosition;
										//Log(LogType.Debug, "setting FPositionOut " + videoLength + " * " + LibVlcMethods.libvlc_media_player_get_position( mediaPlayer ) + " @" + videoFps + "fps => position = " + FPositionOut);
										parent.FDurationOut[mediaRendererIndex] = videoLength;
										parent.FFrameOut[mediaRendererIndex] = currentFrame; //Convert.ToInt32(absolutePosition * videoFps);
										parent.FFrameCountOut[mediaRendererIndex] = Convert.ToInt32(videoLength * videoFps);

									}
									catch (Exception e)
									{
										Log(LogType.Error, "[UpdateParent (position) Exception] " + e.Message);
									}
								}
								UpdateOutput_TextureInfo();

							}
							catch (Exception e)
							{
								Log(LogType.Error, "[UpdateParent Exception] " + e.Message);
							}
							mediaPlayerBusyMutex.ReleaseMutex();
						}
						else
						{
							//Log(LogType.Warning, "[UpdateParent] Media Player Busy");
						}
					}
					else if (active && preloadingStatus == STATUS_IMAGE)
					{
						if (mediaPlayerBusyMutex.WaitOne(0))
						{
							try
							{
								parent.FPositionOut[mediaRendererIndex] = 0;
								parent.FDurationOut[mediaRendererIndex] = 0;
								parent.FFrameOut[mediaRendererIndex] = 1;
								parent.FFrameCountOut[mediaRendererIndex] = 1;

								UpdateOutput_TextureInfo();
							}
							catch (Exception e)
							{
								Log(LogType.Error, "[UpdateParent Exception] " + e.Message);
							}
							mediaPlayerBusyMutex.ReleaseMutex();
						}
						else
						{
							//Log(LogType.Warning, "[UpdateParent] Media Player Busy");
						}
					}

					//						if ( currRotateIn ) {
					//							if (videoWidth > 0 && videoHeight > 0)
					//								UpdateRotation();
					//						}
				}
				catch (Exception e)
				{
					Log(LogType.Error, "[UpdateParent Exception] " + e.Message);
				}

			}

			private void Log(LogType logType, string message)
			{
				parent.Log(logType, "[" + (this == parent.mediaRendererA[mediaRendererIndex] ? "A" : "B") + mediaRendererIndex + (parent.IsFrontMediaRenderer(this) ? "+" : "-") + "] " + message);
			}

			private string StatusToString(int status)
			{
				if (status == STATUS_INACTIVE) { return "INACTIVE"; }
				if (status == STATUS_NEWFILE) { return "NEWFILE"; }
				if (status == STATUS_OPENINGFILE) { return "OPENINGFILE"; }
				if (status == STATUS_GETPROPERTIES) { return "GETPROPERTIES"; }
				if (status == STATUS_GETPROPERTIESOK) { return "STATUS_GETPROPERTIESOK"; }
				if (status == STATUS_GETFIRSTFRAME) { return "GETFIRSTFRAME"; }
				if (status == STATUS_READY) { return "READY"; }
				if (status == STATUS_PLAYING) { return "PLAYING"; }
				return "UNKNOWN";
			}

			//TODO
			private void UpdateColorSettings()
			{
				//brightness, contrast, hue, saturation, ...
				//LibVlcMethods.libvlc_video_set_adjust_float(mediaPlayer, libvlc_video_adjust_option_t.libvlc_adjust_Brightness, currBrightnessIn);
			}

			private void UpdateSpeed()
			{
				//Log( LogType.Debug, "Setting SPEED to " + parent.FSpeedIn[mediaPlayerIndex] );
				LibVlcMethods.libvlc_media_player_set_rate(mediaPlayer, currSpeedIn);
			}

			private void UpdateVolume()
			{
				//Log( LogType.Debug, "Setting Volume to " + Convert.ToInt32( Math.Pow ( Math.Max ( Math.Min( FVolumeIn[mediaRendererIndex], 1), 0 ), Math.E ) * 100 ) );
				LibVlcMethods.libvlc_audio_set_volume(mediaPlayer, Convert.ToInt32(Math.Pow(Math.Max(Math.Min(currVolumeIn, 2), 0), Math.E) * 100));
			}

			private void UpdateVideoSize(int newWidth, int newHeight, bool active)
			{
				decodeLock.WaitOne();
				try
				{
					//only allocate new if different from the one before
					if ((newWidth * newHeight) != (videoWidth * videoHeight))
					{
						CreateNewPixelPlanesAandB(newWidth, newHeight);
						//readPixelPlane = pixelPlaneA;
						//writePixelPlane = pixelPlaneB;
					}

					videoWidth = newWidth;
					videoHeight = newHeight;

					//Log(LogType.Debug, "[Update Video Size] CALLING parent.UpdateVideoSize(...)!");

					UpdateOutput_TextureInfo();

					//"RV32" = RGBA I think, "RV24"=RGB
					int pitch = videoWidth * 4; //depends on pixelformat ( = width * nrOfBytesPerPixel) !!!
					LibVlcMethods.libvlc_video_set_format(mediaPlayer, "RV32", videoWidth, videoHeight, pitch);
				}
				catch (Exception e)
				{
					Log(LogType.Error, "[UpdateVideoSize Exception] " + e.Message);
				}

				decodeLock.ReleaseMutex();

				//Log(LogType.Debug, "[Update Video Size] " + newWidth + "x" +  newHeight + " done!");				
			}

			unsafe private void UpdateOutput_TextureInfo()
			{
				//if ( parent.currentFillTextureFunction == parent.FillTexure || parent.currentFillTextureFunction == parent.Rotate180FillTexure ) {
				parent.FWidthOut[GetMediaRendererIndex()] = GetVideoWidth();
				parent.FHeightOut[GetMediaRendererIndex()] = GetVideoHeight();
				parent.FTextureAspectRatioOut[GetMediaRendererIndex()] = (float)GetVideoWidth() / (float)GetVideoHeight();
				//TODO
				parent.FPixelAspectRatioOut[GetMediaRendererIndex()] = 1.0F;
				//}
				/*
				else if ( parent.currentFillTextureFunction == parent.RotateLeftFillTexure || parent.currentFillTextureFunction == parent.RotateRightFillTexure ) {
					parent.FWidthOut[GetMediaRendererIndex()] = GetVideoHeight();
					parent.FHeightOut[GetMediaRendererIndex()] = GetVideoWidth();
					parent.FTextureAspectRatioOut[GetMediaRendererIndex()] = (float)GetVideoHeight() / (float)GetVideoWidth();
					//TODO
					parent.FPixelAspectRatioOut[GetMediaRendererIndex()] = 1.0F;
				}
				*/
			}

			protected string GetFullPath(string path)
			{
				return parent.GetFullPath(path);
			}

			private IntPtr ParseFilename(string fileName)
			{
				//Log(LogType.Debug, "ParseFilename( " + fileName + " )" );
				if (fileName.Length == 0)
				{
					return IntPtr.Zero;
				}

				string[] mediaOptions = fileName.Split("|".ToCharArray());
				if (mediaOptions[0].TrimEnd().Length == 0)
				{
					return IntPtr.Zero;
				}

				IntPtr retVal = new IntPtr();
				try
				{

					retVal = LibVlcMethods.libvlc_media_new_location(libVLC, mediaOptions[0].TrimEnd());

					for (int moIndex = 1; moIndex < mediaOptions.Length; moIndex++)
					{
						LibVlcMethods.libvlc_media_add_option(retVal, mediaOptions[moIndex].Trim());
					}
					/*
					string[] mediaOptionParts = mediaOptions[moIndex].Trim().Split("{".ToCharArray());
					LibVlcMethods.libvlc_media_add_option( media, mediaOptionParts.Trim() );

					if ( mediaOptionParts.Length == 2) {
						string[] flags = mediaOptionParts[1].Replace("}", "").Split(",".ToCharArray());
						for (int flagIndex = 1; flagIndex < mediaOptionParts.Length; flagIndex++ ) {
							string[] flagParts = flags[flagIndex].Split("=".ToCharArray());
							if ( flagParts.Length == 2) {
								Log( LogType.Debug, "adding option " + flagParts + " = " + flagParts[1] );
								LibVlcMethods.libvlc_media_add_option_flag( media, flagParts, LibVlcWrapper.libvlc_video_adjust_option_t .libvlc_media_option_trusted ); //Convert.ToInt32(flagParts[1])
							}
							else {
								Log(LogType.Debug, "Something strange when parsing filename options...");
							}
						}
					}
					*/
				}
				catch
				{
					retVal = IntPtr.Zero;
				}

				return retVal;
			}

			private bool IsImageFileName(string fileName)
			{
				try
				{
					if (fileName.Contains("|"))
					{
						return false;
					}

					string ext = Path.GetExtension(fileName).ToLower();
					bool retVal = (!fileName.Contains("|"))
								&& (ext.CompareTo(".png") == 0 || ext.CompareTo(".gif") == 0 || ext.CompareTo(".bmp") == 0 || ext.CompareTo(".tif") == 0 || ext.CompareTo(".tiff") == 0 || ext.CompareTo(".jpg") == 0 || ext.CompareTo(".jpeg") == 0);

					//Log(LogType.Debug, "[IsImagefileName] Checking if '" + fileName + "' with extension '" + ext + "' is an image... " + (retVal ? "YES" : "NO"));

					return retVal;
				}
				catch (Exception e)
				{
					Log(LogType.Error, "[IsImageFileName] exception: " + e.Message);
				}
				return false;
			}

			public void LoadImage(string path)
			{
				try
				{
					Image image = Image.FromFile(path);
					Size newSize = GetWantedSize(image.Width, image.Height);

					//lock as short as possible!
					//Log(LogType.Debug, "[LoadImage] LOCKING before UpdateVideoSize");
					parent.LockBackFrontMediaRenderer(mediaRendererIndex);
					try
					{
						//Log(LogType.Debug, "[LoadImage] start UpdateVideoSize");
						UpdateVideoSize(newSize.Width, newSize.Height, parent.IsFrontMediaRenderer(this));
						//Log(LogType.Debug, "[LoadImage] stop UpdateVideoSize");
					}
					catch (Exception e)
					{
						Log(LogType.Error, "[LoadImage Exception] " + " UpdateVideoSize " + e.Message);
					}
					parent.UnlockBackFrontMediaRenderer(mediaRendererIndex);

					//Graphics objects can not be created from bitmaps with an Indexed Pixel Format, use RGB instead.
					Bitmap newImage = new Bitmap(newSize.Width, newSize.Height, PixelFormat.Format32bppArgb);
					Graphics canvas = Graphics.FromImage(newImage);
					canvas.SmoothingMode = SmoothingMode.AntiAlias;
					canvas.InterpolationMode = InterpolationMode.HighQualityBicubic;
					canvas.PixelOffsetMode = PixelOffsetMode.HighQuality;
					canvas.DrawImage(image, new Rectangle(new Point(0, 0), newSize));

					//copy to memory buffer (slow)
					int writeIndex = 0;
					for (int y = 0; y < newSize.Height; y++)
					{
						for (int x = 0; x < newSize.Width; x++)
						{
							//Marshal.WriteInt32(imageAandB.BackImage.Data, writeIndex, newImage.GetPixel(x, y).ToRgba() );
							//writeIndex += 4;


							Color c = new Color();
							try
							{
								c = newImage.GetPixel(x, y);
							}
							catch (Exception e)
							{
								Log(LogType.Error, "[LoadImage Exception] " + " newImage.GetPixel(" + x + "," + y + ") " + e.Message);
							}
							try
							{
								//writeIndex = (y * newSize.Width + x) * 4;
								Marshal.WriteByte(imageAandB.FrontImage.Data, writeIndex, c.B);
								Marshal.WriteByte(imageAandB.FrontImage.Data, writeIndex + 1, c.G);
								Marshal.WriteByte(imageAandB.FrontImage.Data, writeIndex + 2, c.R);
								Marshal.WriteByte(imageAandB.FrontImage.Data, writeIndex + 3, c.A);

								writeIndex += 4;
							}
							catch (Exception e)
							{
								Log(LogType.Error, "[LoadImage Exception] " + " WriteByte " + writeIndex + " (" + x + "," + y + ") " + e.Message);
							}
						}
					}

					imageAandB.Swap();
					imageAandB.Swap();

					//ThreadedDisplay(null);

					//lock as short as possible!
					//Log(LogType.Debug, "[LoadImage] LOCKING before setting NextReady");
					parent.LockBackFrontMediaRenderer(mediaRendererIndex);
					try
					{
						//Log(LogType.Debug, "[LoadImage] setting NextReady");
						if (!parent.IsFrontMediaRenderer(this)) parent.FNextReadyOut[mediaRendererIndex] = true;
						//Log(LogType.Debug, "[LoadImage] NextReady set");
					}
					catch (Exception e)
					{
						Log(LogType.Error, "[LoadImage Exception] " + " set NextReadyOut " + e.Message);
					}
					parent.UnlockBackFrontMediaRenderer(mediaRendererIndex);

				}
				catch (Exception e)
				{
					Log(LogType.Error, "[LoadImage Exception] " + e.Message);
				}

			}


			public Size GetWantedSize(int sourceWidth, int sourceHeight)
			{
				Size wantedSize = new Size(sourceWidth, sourceHeight);
				double sar = 1;
				if (sourceWidth == 0 && sourceHeight == 0 && currWidthIn == 0 && currHeightIn == 0)
				{
					Log(LogType.Debug, "STRANGE wxh = 0x0");
					wantedSize.Width = 320;
					wantedSize.Height = 240;
				}
				else
				{
					sar = (double)sourceWidth / sourceHeight;
				}
				//if width or height forced, calculate the other one autoamticlly (keep aspect ratio)
				if (currWidthIn > 0 && currHeightIn == 0)
				{
					wantedSize.Width = currWidthIn;
					wantedSize.Height = Math.Max(1, (int)((double)currWidthIn / sar));
				}
				else if (currWidthIn == 0 && currHeightIn > 0)
				{
					wantedSize.Width = Math.Max(1, (int)((double)currHeightIn * sar));
					wantedSize.Height = currHeightIn;
				}
				else if (currWidthIn > 0 && currHeightIn > 0)
				{
					wantedSize.Width = currWidthIn;
					wantedSize.Height = currHeightIn;
				}
				return wantedSize;
			}

			private bool IsPlaying(bool active)
			{
				return active ? parent.FPlayIn[mediaRendererIndex] : false;
			}
			private IDiffSpread<String> GetFileNameIn(bool active)
			{
				return active ? parent.FFileNameIn : parent.FNextFileNameIn;
			}

			public CVImageLink GetCVImageLink()
			{
				return imageAandB;
			}

			public int GetVideoWidth() { return videoWidth; }
			public int GetVideoHeight() { return videoHeight; }

			public int GetMediaRendererIndex() { return mediaRendererIndex; }

			private long prevTime = DateTime.Now.Ticks;
			private long currTime = DateTime.Now.Ticks;
			private void ReportElapsedTime(string description)
			{
				currTime = DateTime.Now.Ticks;
				Log(LogType.Debug, description + " took " + (currTime - prevTime) + " ticks.");
				prevTime = currTime;
			}

			private void CreateNewPixelPlanesAandB(int w, int h)
			{

				CVImageAttributes cvImageAttributes = new CVImageAttributes();
				cvImageAttributes.ColourFormat = TColorFormat.RGBA8;
				cvImageAttributes.FSize = new Size(w, h);

				imageAandB.Initialise(cvImageAttributes);
				//Log(LogType.Debug, "[CreateNewPixelPlanesAandB(" + w + "," + h + ")] ");

				try
				{
					//GCHandle pinnedImageAData = GCHandle.Alloc(imageA.Data, GCHandleType.Pinned);
					GCHandle pinnedImageAData = GCHandle.Alloc(imageAandB.FrontImage.Data, GCHandleType.Pinned);
					//pixelPlaneA = pinnedImageAData.AddrOfPinnedObject();

					GCHandle pinnedImageBData = GCHandle.Alloc(imageAandB.BackImage.Data, GCHandleType.Pinned);
					//pixelPlaneB = pinnedImageBData.AddrOfPinnedObject();

					pinnedImageAData.Free();
					pinnedImageBData.Free();
				}
				catch (Exception e)
				{
					Log(LogType.Error, "[CreateNewPixelPlanesAandB Exception (pinnedData)] " + e.Message);
					throw e;
				}
			}

		}

		#endregion private classes

		#region fields
		ISpread<string> FPrevFileNameIn;
		ISpread<string> FPrevNextFileNameIn;

		private IntPtr FLibVLC = IntPtr.Zero;

		private ISpread<MediaRenderer> mediaRendererA;
		private ISpread<MediaRenderer> mediaRendererB;
		private ISpread<MediaRenderer> mediaRendererCurrent;  //points to A or B, depending on which one is the 'visible' mediaRenderer
		private ISpread<MediaRenderer> mediaRendererNext; //points to B or A, depending on which one is the 'invisible' mediaRenderer

		private ISpread<Mutex> mediaRendererBackFrontMutex; //used to make sure which one is the back and which one the front renderer status is always correct

		private string[] argv = {
			"--no-video-title",
			"--no-one-instance",
			"--directx-audio-speaker=5.1"
		};

		private String FLogMe = ""; //used in Callback functions, because logging from there crashes

		private TexturedVertex[] FMyQuad;
		private int myQuadSize;
		private Dictionary<Int64, VertexBuffer> device2QuadVertexBuffer;
		
		[Import]
		IPluginHost FHost;

		[Import]
		ILogger FLogger;
		#endregion fields


		// import host and hand it to base constructor
		[ImportingConstructor]
		public VlcNode()
		{
			//fill the quad
			///////////////
			float xy = 1.0f;
			float z = 0;
			float u = 0;
			float v = 0;

			FMyQuad = new TexturedVertex[4];
			FMyQuad[0].Position.X = -xy; FMyQuad[0].Position.Y = xy; FMyQuad[0].Position.Z = z;
			FMyQuad[0].TextureCoordinate.X = u; FMyQuad[0].TextureCoordinate.Y = v;
			FMyQuad[1].Position.X = -xy; FMyQuad[1].Position.Y = -xy; FMyQuad[1].Position.Z = z;
			FMyQuad[1].TextureCoordinate.X = u; FMyQuad[1].TextureCoordinate.Y = 1 + v;
			FMyQuad[2].Position.X = xy; FMyQuad[2].Position.Y = xy; FMyQuad[2].Position.Z = z;
			FMyQuad[2].TextureCoordinate.X = 1 + u; FMyQuad[2].TextureCoordinate.Y = v;
			FMyQuad[3].Position.X = xy /*- (float)(FBlendIn[0] / 2)*/; FMyQuad[3].Position.Y = -xy /*+ (float)(FBlendIn[0] / 2)*/; FMyQuad[3].Position.Z = z;
			FMyQuad[3].TextureCoordinate.X = 1 + u; FMyQuad[3].TextureCoordinate.Y = 1 + v;

			myQuadSize = FMyQuad.GetLength(0) * Marshal.SizeOf(typeof(TexturedVertex));

			device2QuadVertexBuffer = new Dictionary<Int64, VertexBuffer>();

			FLibVLC = LibVlcMethods.libvlc_new(argv.GetLength(0), argv);	//argc, argv

			int initialSpreadCount = 0; //0 because we can only initialize on first Evaluate()
			mediaRendererA = new Spread<MediaRenderer>(initialSpreadCount);
			mediaRendererB = new Spread<MediaRenderer>(initialSpreadCount);
			mediaRendererCurrent = new Spread<MediaRenderer>(initialSpreadCount);
			mediaRendererNext = new Spread<MediaRenderer>(initialSpreadCount);

			mediaRendererBackFrontMutex = new Spread<Mutex>(initialSpreadCount);
		}

		~VlcNode()
		{
			Dispose();
		}

		public void Dispose()
		{
			for (int index = 0; index < mediaRendererA.SliceCount; index++)
			{
				try
				{
					DisposeMediaRenderer(index);
				}
				catch (Exception)
				{
				}
			}
		}


		#region helper functions
		private void UpdateSliceCount(int spreadMax)
		{
			//change everything that has an influence if the spreadMax value changes, like the nr of mediaplayers
			Log(LogType.Debug, "EXISTING MEDIA RENDERERS: --------------------------------");
			for (int i = 0; i < mediaRendererA.SliceCount; i++)
			{
				Log(LogType.Debug, "    " + "A" + mediaRendererA[i].GetMediaRendererIndex() + " B" + mediaRendererB[i].GetMediaRendererIndex() + " C" + mediaRendererCurrent[i].GetMediaRendererIndex() + " N" + mediaRendererNext[i].GetMediaRendererIndex());
			}
			
			int c = spreadMax;
			int prevc = Math.Max(0, mediaRendererA.SliceCount);

			//if shrinking -> dispose first before resizing spreads
			if (c < prevc)
			{
				for (int j = prevc - 1; j >= c; j--)
				{
					DisposeMediaRenderer(j);
				}
				//SetSliceCount(spreadMax);
			}

			mediaRendererA.SliceCount = c;
			mediaRendererB.SliceCount = c;
			mediaRendererCurrent.SliceCount = c;
			mediaRendererNext.SliceCount = c;
			mediaRendererBackFrontMutex.SliceCount = c;
			try
			{
				FDurationOut.SliceCount = c;
				FFrameCountOut.SliceCount = c;
				FFrameOut.SliceCount = c;
				FNextReadyOut.SliceCount = c;
				FPixelAspectRatioOut.SliceCount = c;
				FTextureAspectRatioOut.SliceCount = c;
				FPositionOut.SliceCount = c;
				FWidthOut.SliceCount = c;
				FHeightOut.SliceCount = c;
				//FTextureOut.SliceCount = c;
				FImageOut.SliceCount = c;
			}
			catch { }

			//if growing -> resize spreads first before creating new mediaRenderers
			if (c >= prevc)
			{
				//SetSliceCount(spreadMax);
				for (int i = prevc; i < c; i++)
				{
					CreateMediaRenderer(i);
				}
			}

			Log(LogType.Debug, "NEW MEDIA RENDERERS: --------------------------------");
			for (int i = 0; i < mediaRendererA.SliceCount; i++)
			{
				Log(LogType.Debug, "    " + "A" + mediaRendererA[i].GetMediaRendererIndex() + " B" + mediaRendererB[i].GetMediaRendererIndex() + " C" + mediaRendererCurrent[i].GetMediaRendererIndex() + " N" + mediaRendererNext[i].GetMediaRendererIndex());
			}

		}

		private void CreateMediaRenderer(int index)
		{
			Log(LogType.Debug, "++++++++ creating renderer pair " + index + " ++++++++");
			mediaRendererBackFrontMutex[index] = new Mutex();
			mediaRendererBackFrontMutex[index].WaitOne();

			mediaRendererA[index] = new MediaRenderer(this, index);
			mediaRendererB[index] = new MediaRenderer(this, index);
			mediaRendererCurrent[index] = mediaRendererA[index];
			mediaRendererNext[index] = mediaRendererB[index];

			FImageOut[index] = mediaRendererCurrent[index].GetCVImageLink();

			mediaRendererBackFrontMutex[index].ReleaseMutex();
			Log(LogType.Debug, "++++++++ renderer pair " + index + " created ++++++++");
		}

		private void DisposeMediaRenderer(int index)
		{
			Log(LogType.Debug, "++++++++ disposing of renderer pair " + index + " ++++++++");
			mediaRendererBackFrontMutex[index].WaitOne();

			mediaRendererA[index].Dispose();
			mediaRendererB[index].Dispose();

			mediaRendererBackFrontMutex[index].ReleaseMutex();
		}

		private void CloneSpread(ISpread<string> src, ref ISpread<string> dst)
		{
			dst = new Spread<string>(src.SliceCount);
			dst.SliceCount = src.SliceCount;
			for (int i = 0; i < src.SliceCount; i++)
			{
				try
				{
					dst[i] = src[i] == null ? null : (string)(src[i].Clone());
				}
				catch (Exception e)
				{
					Log(LogType.Error, "[CloneSpread (string) Exception] FOR LOOP " + i + " " + e.Message);
				}
			}
		}
		private void CloneSpread(ISpread<int> src, ref ISpread<int> dst)
		{
			try
			{
				dst = new Spread<int>(src.SliceCount);
				dst.SliceCount = src.SliceCount;
				for (int i = 0; i < src.SliceCount; i++)
				{
					dst[i] = src[i];
				}
			}
			catch (Exception e)
			{
				Log(LogType.Error, "[CloneSpread (int) Exception] " + e.Message);
			}
		}

		private void LockBackFrontMediaRenderer(int index)
		{
			mediaRendererBackFrontMutex[index].WaitOne();
		}

		private void UnlockBackFrontMediaRenderer(int index)
		{
			mediaRendererBackFrontMutex[index].ReleaseMutex();
		}

		private void FlipMediaRenderers(int index)
		{
			//LogNow( LogType.Debug, "[FlipMediaRenderers] LockBackFrontMediaRenderer " + index);
			LockBackFrontMediaRenderer(index);

			//Log(LogType.Debug, "Flipping mediaRenderers");
			if (mediaRendererCurrent[index] == mediaRendererA[index])
			{
				mediaRendererCurrent[index] = mediaRendererB[index];
				mediaRendererNext[index] = mediaRendererA[index];
			}
			else
			{
				mediaRendererCurrent[index] = mediaRendererA[index];
				mediaRendererNext[index] = mediaRendererB[index];
			}

			try
			{
				FImageOut[index] = mediaRendererCurrent[index].GetCVImageLink();

				Log(LogType.Debug, "[FlipMediaRenderers] Setting Image for renderer " + index + " "
					+ FImageOut[index].FrontImage.Width + "x" + FImageOut[index].FrontImage.Height
					/* + (FImageOut[index].FrameChanged ? " framechanged=true" : " framechanged=false") */ );
			}
			catch (Exception e)
			{
				Log(LogType.Error, "[FlipMediaRenderers Exception] While setting Image: " + e.Message);
			}

			UnlockBackFrontMediaRenderer(index);
		}

		//REMEMBER TO LOCK / UNLOCK FrontBackMediaRenderer
		private bool IsFrontMediaRenderer(MediaRenderer r)
		{
			return r == mediaRendererCurrent[r.GetMediaRendererIndex()];
		}


		public void Log(LogType logType, string message)
		{
			FLogMe += "\n" + (logType == LogType.Error ? "ERR " : (logType == LogType.Warning ? "WARN " : "")) + message;
		}

		public void LogNow(LogType logType, string message)
		{
			FLogger.Log(logType, message);
		}

		protected string GetFullPath(string path)
		{
			if (Path.IsPathRooted(path) || path.Contains("://")) return path;

			string fullPath = path;
			try
			{
				string patchPath;
				FHost.GetHostPath(out patchPath);
				patchPath = Path.GetDirectoryName(patchPath);
				fullPath = Path.GetFullPath(Path.Combine(patchPath, path));
			}
			catch (Exception e)
			{
				Log(LogType.Error, e.Message);
				return path;
			}

			return fullPath;
		}
		#endregion helper functions

		///////////////////////////
		//called each frame by vvvv
		///////////////////////////
		private int evaluateCalled = 0;
		public void Evaluate(int spreadMax)
		{
			if (spreadMax == 0)
			{
				Log(LogType.Debug, "! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! !");
				Log(LogType.Debug, "! ! !                SPREADMAX == 0                 ! ! !");
				Log(LogType.Debug, "! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! ! !");
			}


			if (FLogMe.Length > 0)
			{
				FLogger.Log(LogType.Debug, FLogMe);
				FLogMe = "";
			}

			if (spreadMax != mediaRendererCurrent.SliceCount)
			{
				Log(LogType.Debug, "new spreadMax = " + spreadMax);
				//AddMediaRenderer(spreadMax - 1);
				try
				{
					UpdateSliceCount(spreadMax);
				}
				catch (Exception e)
				{
					Log(LogType.Error, "[Evaluate Exception] (UpdateSliceCount) " + e.Message + "\n\n" + e.StackTrace);
					throw e;
				}
				//AddMediaRenderer(spreadMax - 1);
				//CreateMediaRenderer(spreadMax - 1);
			}

			for (int index = 0; index < mediaRendererA.SliceCount; index++)
			{
				try
				{
					if (FPrevFileNameIn == null)
					{
						CloneSpread(FFileNameIn, ref FPrevFileNameIn);
					}
					if (FPrevNextFileNameIn == null)
					{
						CloneSpread(FNextFileNameIn, ref FPrevNextFileNameIn);
					}
				}
				catch (Exception e)
				{
					Log(LogType.Error, "[Evaluate Exception] (FileName) " + e.Message);
				}

				try
				{
					if (FFileNameIn.IsChanged)
					{
						if ((FPrevFileNameIn[index] == null || FFileNameIn[index] == null) || FPrevFileNameIn[index].CompareTo(FFileNameIn[index]) != 0)
						{
							if (FPrevNextFileNameIn[index] != null && FFileNameIn[index] != null && FPrevNextFileNameIn[index].CompareTo(FFileNameIn[index]) == 0)
							{
								FlipMediaRenderers(index);
							}
						}
					}
				}
				catch (Exception e)
				{
					Log(LogType.Error, "[Evaluate Exception] (FileName & FlipMediaRenderers) " + e.Message);
				}

				try
				{
					mediaRendererCurrent[index].Evaluate(true);
					mediaRendererNext[index].Evaluate(false);
				}
				catch (Exception e)
				{
					Log(LogType.Error, "[Evaluate Exception] (MediaRenderer.Evaluate) " + e.Message);
				}
			}

			if (FFileNameIn.IsChanged)
			{
				CloneSpread(FFileNameIn, ref FPrevFileNameIn);
			}
			if (FNextFileNameIn.IsChanged)
			{
				CloneSpread(FNextFileNameIn, ref FPrevNextFileNameIn);
			}
		}

	}
}
