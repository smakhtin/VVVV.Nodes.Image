using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LibVlcWrapper
{
   public enum libvlc_state_t
   {
      libvlc_NothingSpecial = 0,
      libvlc_Opening,
      libvlc_Buffering,
      libvlc_Playing,
      libvlc_Paused,
      libvlc_Stopped,
      libvlc_Ended,
      libvlc_Error
   }

   public enum libvlc_log_messate_t_severity
   {
      INFO = 0,
      ERR = 1,
      WARN = 2,
      DBG = 3
   }

   public enum libvlc_event_e
   {
      libvlc_MediaMetaChanged = 0,
      libvlc_MediaSubItemAdded,
      libvlc_MediaDurationChanged,
      libvlc_MediaParsedChanged,
      libvlc_MediaFreed,
      libvlc_MediaStateChanged,

      libvlc_MediaPlayerMediaChanged = 0x100,
      libvlc_MediaPlayerNothingSpecial,
      libvlc_MediaPlayerOpening,
      libvlc_MediaPlayerBuffering,
      libvlc_MediaPlayerPlaying,
      libvlc_MediaPlayerPaused,
      libvlc_MediaPlayerStopped,
      libvlc_MediaPlayerForward,
      libvlc_MediaPlayerBackward,
      libvlc_MediaPlayerEndReached,
      libvlc_MediaPlayerEncounteredError,
      libvlc_MediaPlayerTimeChanged,
      libvlc_MediaPlayerPositionChanged,
      libvlc_MediaPlayerSeekableChanged,
      libvlc_MediaPlayerPausableChanged,
      libvlc_MediaPlayerTitleChanged,
      libvlc_MediaPlayerSnapshotTaken,
      libvlc_MediaPlayerLengthChanged,

      libvlc_MediaListItemAdded = 0x200,
      libvlc_MediaListWillAddItem,
      libvlc_MediaListItemDeleted,
      libvlc_MediaListWillDeleteItem,

      libvlc_MediaListViewItemAdded = 0x300,
      libvlc_MediaListViewWillAddItem,
      libvlc_MediaListViewItemDeleted,
      libvlc_MediaListViewWillDeleteItem,

      libvlc_MediaListPlayerPlayed = 0x400,
      libvlc_MediaListPlayerNextItemSet,
      libvlc_MediaListPlayerStopped,

      libvlc_MediaDiscovererStarted = 0x500,
      libvlc_MediaDiscovererEnded,

      libvlc_VlmMediaAdded = 0x600,
      libvlc_VlmMediaRemoved,
      libvlc_VlmMediaChanged,
      libvlc_VlmMediaInstanceStarted,
      libvlc_VlmMediaInstanceStopped,
      libvlc_VlmMediaInstanceStatusInit,
      libvlc_VlmMediaInstanceStatusOpening,
      libvlc_VlmMediaInstanceStatusPlaying,
      libvlc_VlmMediaInstanceStatusPause,
      libvlc_VlmMediaInstanceStatusEnd,
      libvlc_VlmMediaInstanceStatusError,
   }

   public enum libvlc_playback_mode_t
   {
      libvlc_playback_mode_default,
      libvlc_playback_mode_loop,
      libvlc_playback_mode_repeat
   }

   public enum libvlc_meta_t
   {
      libvlc_meta_Title,
      libvlc_meta_Artist,
      libvlc_meta_Genre,
      libvlc_meta_Copyright,
      libvlc_meta_Album,
      libvlc_meta_TrackNumber,
      libvlc_meta_Description,
      libvlc_meta_Rating,
      libvlc_meta_Date,
      libvlc_meta_Setting,
      libvlc_meta_URL,
      libvlc_meta_Language,
      libvlc_meta_NowPlaying,
      libvlc_meta_Publisher,
      libvlc_meta_EncodedBy,
      libvlc_meta_ArtworkURL,
      libvlc_meta_TrackID
   }

   public enum libvlc_track_type_t
   {
      libvlc_track_unknown = -1,
      libvlc_track_audio = 0,
      libvlc_track_video = 1,
      libvlc_track_text = 2,
   }

   public enum libvlc_video_logo_option_t
   {
      libvlc_logo_enable,
      libvlc_logo_file,      
      libvlc_logo_x,
      libvlc_logo_y,
      libvlc_logo_delay,
      libvlc_logo_repeat,
      libvlc_logo_opacity,
      libvlc_logo_position,
   }

   public enum libvlc_video_adjust_option_t
   {
      libvlc_adjust_Enable = 0,
      libvlc_adjust_Contrast,
      libvlc_adjust_Brightness,
      libvlc_adjust_Hue,
      libvlc_adjust_Saturation,
      libvlc_adjust_Gamma,
   }

   public enum libvlc_audio_output_device_types_t
   {
      libvlc_AudioOutputDevice_Error = -1,
      libvlc_AudioOutputDevice_Mono = 1,
      libvlc_AudioOutputDevice_Stereo = 2,
      libvlc_AudioOutputDevice_2F2R = 4,
      libvlc_AudioOutputDevice_3F2R = 5,
      libvlc_AudioOutputDevice_5_1 = 6,
      libvlc_AudioOutputDevice_6_1 = 7,
      libvlc_AudioOutputDevice_7_1 = 8,
      libvlc_AudioOutputDevice_SPDIF = 10
   }

   public enum libvlc_audio_output_channel_t
   {
      libvlc_AudioChannel_Error = -1,
      libvlc_AudioChannel_Stereo = 1,
      libvlc_AudioChannel_RStereo = 2,
      libvlc_AudioChannel_Left = 3,
      libvlc_AudioChannel_Right = 4,
      libvlc_AudioChannel_Dolbys = 5
   }
}
