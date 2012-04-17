﻿using System;
using System.Runtime.InteropServices;
using System.Security;

namespace LibVlcWrapper
{
   [SuppressUnmanagedCodeSecurity]
	//Handle some VLC events!
	public static class LibVlcMethods
   {
      #region libvlc.h

      [DllImport("libvlc")]
      public static extern string libvlc_errmsg();

      [DllImport("libvlc")]
      public static extern void libvlc_clearerr();

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_new(int argc, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] argv);

      [DllImport("libvlc")]
      public static extern void libvlc_release(IntPtr libvlc_instance_t);

      [DllImport("libvlc")]
      public static extern void libvlc_retain(IntPtr p_instance);

      [DllImport("libvlc")]
      public static extern int libvlc_add_intf(IntPtr p_instance, [MarshalAs(UnmanagedType.LPStr)] string name);

      [DllImport("libvlc")]
      public static extern void libvlc_wait(IntPtr p_instance);

      [DllImport("libvlc")]
      public static extern void libvlc_set_user_agent(IntPtr p_instance, [MarshalAs(UnmanagedType.LPStr)] string name, [MarshalAs(UnmanagedType.LPStr)] string http);

      [DllImport("libvlc")]
      public static extern string libvlc_get_version();

      [DllImport("libvlc")]
      public static extern int libvlc_event_attach(IntPtr p_event_manager, libvlc_event_e i_event_type, IntPtr f_callback, IntPtr user_data);

      [DllImport("libvlc")]
      public static extern void libvlc_event_detach(IntPtr p_event_manager, libvlc_event_e i_event_type, IntPtr f_callback, IntPtr user_data);

      [DllImport("libvlc")]
      [return: MarshalAs(UnmanagedType.AnsiBStr)]
      public static extern string libvlc_event_type_name(libvlc_event_e event_type);

      [DllImport("libvlc")]
      public static extern UInt32 libvlc_get_log_verbosity(IntPtr libvlc_instance_t);

      [DllImport("libvlc")]
      public static extern void libvlc_set_log_verbosity(IntPtr libvlc_instance_t, UInt32 level);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_log_open(IntPtr libvlc_instance_t);

      [DllImport("libvlc")]
      public static extern void libvlc_log_close(IntPtr libvlc_log_t);

      [DllImport("libvlc")]
      public static extern UInt32 libvlc_log_count(IntPtr libvlc_log_t);

      [DllImport("libvlc")]
      public static extern void libvlc_log_clear(IntPtr libvlc_log_t);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_log_get_iterator(IntPtr libvlc_log_t);

      [DllImport("libvlc")]
      public static extern void libvlc_log_iterator_free(IntPtr libvlc_log_iterator_t);

      [DllImport("libvlc")]
      public static extern Int32 libvlc_log_iterator_has_next(IntPtr libvlc_log_iterator_t);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_log_iterator_next(IntPtr libvlc_log_iterator_t, ref libvlc_log_message_t p_buffer);

      #endregion

      #region libvlc_media.h

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_new_location(IntPtr p_instance, [MarshalAs(UnmanagedType.LPStr)] string psz_mrl);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_new_path(IntPtr p_instance, [MarshalAs(UnmanagedType.LPStr)] string psz_mrl);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_new_as_node(IntPtr p_instance, [MarshalAs(UnmanagedType.LPStr)] string psz_mrl);

      [DllImport("libvlc")]
      public static extern void libvlc_media_add_option(IntPtr libvlc_media_inst, [MarshalAs(UnmanagedType.LPStr)] string ppsz_options);

      [DllImport("libvlc")]
      public static extern void libvlc_media_add_option_flag(IntPtr p_md, [MarshalAs(UnmanagedType.LPStr)] string ppsz_options, int i_flags);

      [DllImport("libvlc")]
      public static extern void libvlc_media_retain(IntPtr p_md);

      [DllImport("libvlc")]
      public static extern void libvlc_media_release(IntPtr libvlc_media_inst);

      [DllImport("libvlc")]
      [return: MarshalAs(UnmanagedType.AnsiBStr)]
      public static extern string libvlc_media_get_mrl(IntPtr p_md);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_duplicate(IntPtr p_md);

      [DllImport("libvlc")]
      [return: MarshalAs(UnmanagedType.AnsiBStr)]
      public static extern string libvlc_media_get_meta(IntPtr p_md, libvlc_meta_t e_meta);

      [DllImport("libvlc")]
      public static extern void libvlc_media_set_meta(IntPtr p_md, libvlc_meta_t e_meta, [MarshalAs(UnmanagedType.LPStr)] string psz_value);

      [DllImport("libvlc")]
      public static extern int libvlc_media_save_meta(IntPtr p_md);

      [DllImport("libvlc")]
      public static extern libvlc_state_t libvlc_media_get_state(IntPtr p_meta_desc);

      [DllImport("libvlc")]
      public static extern int libvlc_media_get_stats(IntPtr p_md, out libvlc_media_stats_t p_stats);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_event_manager(IntPtr p_md);

      [DllImport("libvlc")]
      public static extern Int64 libvlc_media_get_duration(IntPtr p_md);

      [DllImport("libvlc")]
      public static extern void libvlc_media_parse(IntPtr media);

      [DllImport("libvlc")]
      public static extern void libvlc_media_parse_async(IntPtr media);

      [DllImport("libvlc")]
      public static extern int libvlc_media_is_parsed(IntPtr p_md);

      [DllImport("libvlc")]
      public static extern void libvlc_media_set_user_data(IntPtr p_md, IntPtr p_new_user_data);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_get_user_data(IntPtr p_md);

      [DllImport("libvlc")]
      public static extern int libvlc_media_get_tracks_info(IntPtr media, out IntPtr tracks); //libvlc_media_track_info_t

      #endregion

      #region libvlc_media_discoverer.h

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_discoverer_new_from_name(IntPtr p_inst, [MarshalAs(UnmanagedType.LPStr)] string psz_name );

      [DllImport("libvlc")]
      public static extern void libvlc_media_discoverer_release(IntPtr p_mdis );

      [DllImport("libvlc")]
      [return : MarshalAs(UnmanagedType.AnsiBStr)]
      public static extern string libvlc_media_discoverer_localized_name(IntPtr p_mdis );

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_discoverer_media_list(IntPtr p_mdis );

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_discoverer_event_manager(IntPtr p_mdis );

      [DllImport("libvlc")]
      public static extern int libvlc_media_discoverer_is_running(IntPtr p_mdis );


      #endregion

      #region libvlc_media_library.h

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_library_new(IntPtr p_instance);

      [DllImport("libvlc")]
      public static extern void libvlc_media_library_release(IntPtr p_mlib);

      [DllImport("libvlc")]
      public static extern void libvlc_media_library_retain(IntPtr p_mlib);

      [DllImport("libvlc")]
      public static extern int libvlc_media_library_load(IntPtr p_mlib);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_library_media_list(IntPtr p_mlib ); 

      #endregion

      #region libvlc_media_player.h

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_player_new(IntPtr p_libvlc_instance);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_player_new_from_media(IntPtr libvlc_media);

      [DllImport("libvlc")]
      public static extern void libvlc_media_player_release(IntPtr libvlc_mediaplayer);

      [DllImport("libvlc")]
      public static extern void libvlc_media_player_retain(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_player_get_media(IntPtr libvlc_mediaplayer);

      [DllImport("libvlc")]
      public static extern void libvlc_media_player_set_media(IntPtr libvlc_media_player_t, IntPtr libvlc_media_t);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_player_event_manager(IntPtr libvlc_media_player_t);

      [DllImport("libvlc")]
      public static extern int libvlc_media_player_is_playing(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern void libvlc_media_player_play(IntPtr libvlc_mediaplayer);

      [DllImport("libvlc")]
      public static extern void libvlc_media_player_set_pause(IntPtr mp, int do_pause);

      [DllImport("libvlc")]
      public static extern void libvlc_media_player_pause(IntPtr libvlc_mediaplayer);

      [DllImport("libvlc")]
      public static extern void libvlc_media_player_stop(IntPtr libvlc_mediaplayer);

      [DllImport("libvlc")]
      public static extern void libvlc_video_set_callbacks(IntPtr mp, IntPtr lockk, IntPtr unlock, IntPtr display, IntPtr opaque); 

      [DllImport("libvlc")]
      public static extern void libvlc_video_set_format(IntPtr mp, [MarshalAs(UnmanagedType.LPStr)] string chroma, int width, int height, int pitch);

      [DllImport("libvlc")]
      public static extern void libvlc_media_player_set_hwnd(IntPtr libvlc_mediaplayer, IntPtr libvlc_drawable);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_player_get_hwnd(IntPtr libvlc_mediaplayer);

      [DllImport("libvlc")]
      public static extern Int64 libvlc_media_player_get_length(IntPtr libvlc_mediaplayer);

      [DllImport("libvlc")]
      public static extern Int64 libvlc_media_player_get_time(IntPtr libvlc_mediaplayer);

      [DllImport("libvlc")]
      public static extern void libvlc_media_player_set_time(IntPtr libvlc_mediaplayer, Int64 time);

      [DllImport("libvlc")]
      public static extern float libvlc_media_player_get_position(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern void libvlc_media_player_set_position(IntPtr p_mi, float f_pos);

      [DllImport("libvlc")]
      public static extern void libvlc_media_player_set_chapter(IntPtr p_mi, int i_chapter);

      [DllImport("libvlc")]
      public static extern int libvlc_media_player_get_chapter(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_media_player_get_chapter_count(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_media_player_will_play(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_media_player_get_chapter_count_for_title(IntPtr p_mi, int i_title);

      [DllImport("libvlc")]
      public static extern void libvlc_media_player_set_title(IntPtr p_mi, int i_title);

      [DllImport("libvlc")]
      public static extern int libvlc_media_player_get_title(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_media_player_get_title_count(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern void libvlc_media_player_previous_chapter(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern void libvlc_media_player_next_chapter(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern float libvlc_media_player_get_rate(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_media_player_set_rate(IntPtr p_mi, float rate);

      /* ft */
      [DllImport("libvlc")]
      public static extern UInt32 libvlc_media_player_get_agl(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern void libvlc_media_player_set_agl(IntPtr p_mi, UInt32 drawable);
	
      
      [DllImport("libvlc")]
      public static extern libvlc_state_t libvlc_media_player_get_state(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern float libvlc_media_player_get_fps(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_media_player_has_vout(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_media_player_is_seekable(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_media_player_can_pause(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern void libvlc_media_player_next_frame(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern void libvlc_track_description_release(IntPtr p_track_description);

      [DllImport("libvlc")]
      public static extern void libvlc_toggle_fullscreen(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern void libvlc_set_fullscreen(IntPtr p_mi, int b_fullscreen);

      [DllImport("libvlc")]
      public static extern int libvlc_get_fullscreen(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern void libvlc_video_set_key_input(IntPtr p_mi, int on);

      [DllImport("libvlc")]
      public static extern void libvlc_video_set_mouse_input(IntPtr p_mi, int on);

      [DllImport("libvlc")]
      public static extern int libvlc_video_get_size(IntPtr p_mi, int num, out int px, out int py);

      [DllImport("libvlc")]
      public static extern int libvlc_video_get_height(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_video_get_width(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_video_get_cursor(IntPtr p_mi, int num, out int px, out int py);

      [DllImport("libvlc")]
      public static extern float libvlc_video_get_scale(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern void libvlc_video_set_scale(IntPtr p_mi, float f_factor);

      [DllImport("libvlc")]
      [return: MarshalAs(UnmanagedType.AnsiBStr)]
      public static extern string libvlc_video_get_aspect_ratio(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern void libvlc_video_set_aspect_ratio(IntPtr p_mi, [MarshalAs(UnmanagedType.LPStr)] string psz_aspect);

      [DllImport("libvlc")]
      public static extern int libvlc_video_get_spu(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_video_get_spu_count(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_video_get_spu_description(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_video_set_spu(IntPtr p_mi, int i_spu);

      [DllImport("libvlc")]
      public static extern int libvlc_video_set_subtitle_file(IntPtr p_mi, [MarshalAs(UnmanagedType.LPStr)] string psz_subtitle);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_video_get_title_description(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_video_get_chapter_description(IntPtr p_mi, int i_title);

      [DllImport("libvlc")]
      [return: MarshalAs(UnmanagedType.AnsiBStr)]
      public static extern string libvlc_video_get_crop_geometry(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern void libvlc_video_set_crop_geometry(IntPtr p_mi, [MarshalAs(UnmanagedType.LPStr)] string psz_geometry);

      [DllImport("libvlc")]
      public static extern int libvlc_video_get_teletext(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern void libvlc_video_set_teletext(IntPtr p_mi, int i_page);

      [DllImport("libvlc")]
      public static extern void libvlc_toggle_teletext(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_video_get_track_count(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_video_get_track_description(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_video_get_track(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_video_set_track(IntPtr p_mi, int i_track);

      [DllImport("libvlc")]
      public static extern void libvlc_video_take_snapshot(IntPtr libvlc_media_player_t, [MarshalAs(UnmanagedType.LPStr)] string filePath, UInt32 i_width, UInt32 i_height);

      [DllImport("libvlc")]
      public static extern void libvlc_video_set_deinterlace(IntPtr p_mi, [MarshalAs(UnmanagedType.LPStr)] string psz_mode);

      [DllImport("libvlc")]
      public static extern int libvlc_video_get_marquee_int(IntPtr p_mi, int option);

      [DllImport("libvlc")]
      [return: MarshalAs(UnmanagedType.AnsiBStr)]
      public static extern string libvlc_video_get_marquee_string(IntPtr p_mi, int option);

      [DllImport("libvlc")]
      public static extern void libvlc_video_set_marquee_int(IntPtr p_mi, int option, int i_val);

      [DllImport("libvlc")]
      public static extern void libvlc_video_set_marquee_string(IntPtr p_mi, int option, [MarshalAs(UnmanagedType.LPStr)] string psz_text);

      [DllImport("libvlc")]
      public static extern int libvlc_video_get_logo_int(IntPtr p_mi, libvlc_video_logo_option_t option);

      [DllImport("libvlc")]
      public static extern void libvlc_video_set_logo_int(IntPtr p_mi, libvlc_video_logo_option_t option, int value);

      [DllImport("libvlc")]
      public static extern void libvlc_video_set_logo_string(IntPtr p_mi, libvlc_video_logo_option_t option, [MarshalAs(UnmanagedType.LPStr)] string psz_value);

      [DllImport("libvlc")]
      public static extern int libvlc_video_get_adjust_int(IntPtr p_mi, libvlc_video_adjust_option_t option);

      [DllImport("libvlc")]
      public static extern void libvlc_video_set_adjust_int(IntPtr p_mi, libvlc_video_adjust_option_t option, int value);

      [DllImport("libvlc")]
      public static extern float libvlc_video_get_adjust_float(IntPtr p_mi, libvlc_video_adjust_option_t option);

      [DllImport("libvlc")]
      public static extern void libvlc_video_set_adjust_float(IntPtr p_mi, libvlc_video_adjust_option_t option, float value);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_audio_output_list_get(IntPtr p_instance);

      [DllImport("libvlc")]
      public static extern void libvlc_audio_output_list_release(IntPtr p_list);

      [DllImport("libvlc")]
      public static extern int libvlc_audio_output_set(IntPtr p_mi, [MarshalAs(UnmanagedType.LPStr)] string psz_name);

      [DllImport("libvlc")]
      public static extern int libvlc_audio_output_device_count(IntPtr p_instance, [MarshalAs(UnmanagedType.LPStr)] string psz_audio_output);

      [DllImport("libvlc")]
      [return: MarshalAs(UnmanagedType.AnsiBStr)]
      public static extern string libvlc_audio_output_device_longname(IntPtr p_instance, [MarshalAs(UnmanagedType.LPStr)] string psz_audio_output, int i_device);

      [DllImport("libvlc")]
      [return: MarshalAs(UnmanagedType.AnsiBStr)]
      public static extern string libvlc_audio_output_device_id(IntPtr p_instance, [MarshalAs(UnmanagedType.LPStr)] string psz_audio_output, int i_device);

      [DllImport("libvlc")]
      public static extern void libvlc_audio_output_device_set(IntPtr p_mi, [MarshalAs(UnmanagedType.LPStr)] string psz_audio_output, [MarshalAs(UnmanagedType.LPStr)] string psz_device_id);

      [DllImport("libvlc")]
      public static extern libvlc_audio_output_device_types_t libvlc_audio_output_get_device_type(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern void libvlc_audio_output_set_device_type(IntPtr p_mi, libvlc_audio_output_device_types_t device_type);

      [DllImport("libvlc")]
      public static extern void libvlc_audio_toggle_mute(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_audio_get_volume(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_audio_set_volume(IntPtr p_mi, int volume);

      [DllImport("libvlc")]
      public static extern void libvlc_audio_set_mute(IntPtr p_mi, bool mute);

      [DllImport("libvlc")]
      public static extern bool libvlc_audio_get_mute(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_audio_get_track_count(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_audio_get_track_description(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_audio_get_track(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_audio_set_track(IntPtr p_mi, int i_track);

      [DllImport("libvlc")]
      public static extern libvlc_audio_output_channel_t libvlc_audio_get_channel(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_audio_set_channel(IntPtr p_mi, libvlc_audio_output_channel_t channel);

      [DllImport("libvlc")]
      public static extern Int64 libvlc_audio_get_delay(IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern int libvlc_audio_set_delay(IntPtr p_mi, Int64 i_delay);

      #endregion

      #region libvlc_media_list.h

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_list_new(IntPtr p_instance);

      [DllImport("libvlc")]
      public static extern void libvlc_media_list_release(IntPtr p_ml);

      [DllImport("libvlc")]
      public static extern void libvlc_media_list_retain(IntPtr p_ml);

      [DllImport("libvlc")]
      public static extern void libvlc_media_list_set_media(IntPtr p_ml, IntPtr p_md);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_list_media(IntPtr p_ml);

      [DllImport("libvlc")]
      public static extern int libvlc_media_list_add_media(IntPtr p_ml, IntPtr p_md);

      [DllImport("libvlc")]
      public static extern int libvlc_media_list_insert_media(IntPtr p_ml, IntPtr p_md, int i_pos);

      [DllImport("libvlc")]
      public static extern int libvlc_media_list_remove_index(IntPtr p_ml, int i_pos);

      [DllImport("libvlc")]
      public static extern int libvlc_media_list_count(IntPtr p_ml);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_list_item_at_index(IntPtr p_ml, int i_pos);

      [DllImport("libvlc")]
      public static extern int libvlc_media_list_index_of_item(IntPtr p_ml, IntPtr p_md);

      [DllImport("libvlc")]
      public static extern int libvlc_media_list_is_readonly(IntPtr p_ml);

      [DllImport("libvlc")]
      public static extern void libvlc_media_list_lock(IntPtr p_ml);

      [DllImport("libvlc")]
      public static extern void libvlc_media_list_unlock(IntPtr p_ml);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_list_event_manager(IntPtr p_ml);

      #endregion

      #region libvlc_media_list_player.h

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_list_player_new(IntPtr p_instance);

      [DllImport("libvlc")]
      public static extern void libvlc_media_list_player_release(IntPtr p_mlp);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_media_list_player_event_manager(IntPtr p_mlp);

      [DllImport("libvlc")]
      public static extern void libvlc_media_list_player_set_media_player(IntPtr p_mlp, IntPtr p_mi);

      [DllImport("libvlc")]
      public static extern void libvlc_media_list_player_set_media_list(IntPtr p_mlp, IntPtr p_mlist);

      [DllImport("libvlc")]
      public static extern void libvlc_media_list_player_play(IntPtr p_mlp);

      [DllImport("libvlc")]
      public static extern void libvlc_media_list_player_pause(IntPtr p_mlp);

      [DllImport("libvlc")]
      public static extern int libvlc_media_list_player_is_playing(IntPtr p_mlp);

      [DllImport("libvlc")]
      public static extern libvlc_state_t libvlc_media_list_player_get_state(IntPtr p_mlp);

      [DllImport("libvlc")]
      public static extern int libvlc_media_list_player_play_item_at_index(IntPtr p_mlp, int i_index);

      [DllImport("libvlc")]
      public static extern int libvlc_media_list_player_play_item(IntPtr p_mlp, IntPtr p_md);

      [DllImport("libvlc")]
      public static extern void libvlc_media_list_player_stop(IntPtr p_mlp);

      [DllImport("libvlc")]
      public static extern int libvlc_media_list_player_next(IntPtr p_mlp);

      [DllImport("libvlc")]
      public static extern int libvlc_media_list_player_previous(IntPtr p_mlp);

      [DllImport("libvlc")]
      public static extern void libvlc_media_list_player_set_playback_mode(IntPtr p_mlp, libvlc_playback_mode_t e_mode);

      #endregion

      #region libvlc_vlm.h

      [DllImport("libvlc")]
      public static extern void libvlc_vlm_release(IntPtr p_instance);

      [DllImport("libvlc")]
      public static extern int libvlc_vlm_add_broadcast(IntPtr p_instance,
                                             [MarshalAs(UnmanagedType.LPStr)] string psz_name,
                                             [MarshalAs(UnmanagedType.LPStr)] string psz_input,
                                             [MarshalAs(UnmanagedType.LPStr)] string psz_output,
                                             int i_options,
                                             [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] ppsz_options,
                                             int b_enabled,
                                             int b_loop);

      [DllImport("libvlc")]
      public static extern int libvlc_vlm_add_vod(IntPtr p_instance,
                                       [MarshalAs(UnmanagedType.LPStr)] string psz_name,
                                       [MarshalAs(UnmanagedType.LPStr)] string psz_input,
                                       int i_options,
                                       [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] ppsz_options,
                                       int b_enabled,
                                       [MarshalAs(UnmanagedType.LPStr)] string psz_mux);

      [DllImport("libvlc")]
      public static extern int libvlc_vlm_del_media(IntPtr p_instance, [MarshalAs(UnmanagedType.LPStr)] string psz_name);

      [DllImport("libvlc")]
      public static extern int libvlc_vlm_set_enabled(IntPtr p_instance,
                                           [MarshalAs(UnmanagedType.LPStr)] string psz_name,
                                           int b_enabled);

      [DllImport("libvlc")]
      public static extern int libvlc_vlm_set_output(IntPtr p_instance,
                                          [MarshalAs(UnmanagedType.LPStr)] string psz_name,
                                          [MarshalAs(UnmanagedType.LPStr)] string psz_output);

      [DllImport("libvlc")]
      public static extern int libvlc_vlm_set_input(IntPtr p_instance,
                                         [MarshalAs(UnmanagedType.LPStr)] string psz_name,
                                         [MarshalAs(UnmanagedType.LPStr)] string psz_input);

      [DllImport("libvlc")]
      public static extern int libvlc_vlm_add_input(IntPtr p_instance,
                                         [MarshalAs(UnmanagedType.LPStr)] string psz_name,
                                         [MarshalAs(UnmanagedType.LPStr)] string psz_input);

      [DllImport("libvlc")]
      public static extern int libvlc_vlm_set_loop(IntPtr p_instance,
                                        [MarshalAs(UnmanagedType.LPStr)] string psz_name,
                                        int b_loop);

      [DllImport("libvlc")]
      public static extern int libvlc_vlm_set_mux(IntPtr p_instance,
                                       [MarshalAs(UnmanagedType.LPStr)] string psz_name,
                                       [MarshalAs(UnmanagedType.LPStr)] string psz_mux);


      [DllImport("libvlc")]
      public static extern int libvlc_vlm_change_media(IntPtr p_instance,
                                            [MarshalAs(UnmanagedType.LPStr)] string psz_name,
                                            [MarshalAs(UnmanagedType.LPStr)] string psz_input,
                                            [MarshalAs(UnmanagedType.LPStr)] string psz_output,
                                            int i_options,
                                            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] string[] ppsz_options,
                                            int b_enabled,
                                            int b_loop);

      [DllImport("libvlc")]
      public static extern int libvlc_vlm_play_media(IntPtr p_instance,
                                           [MarshalAs(UnmanagedType.LPStr)] string psz_name);

      [DllImport("libvlc")]
      public static extern int libvlc_vlm_stop_media(IntPtr p_instance,
                                           [MarshalAs(UnmanagedType.LPStr)] string psz_name);

      [DllImport("libvlc")]
      public static extern int libvlc_vlm_pause_media(IntPtr p_instance,
                                           [MarshalAs(UnmanagedType.LPStr)] string psz_name);

      [DllImport("libvlc")]
      public static extern int libvlc_vlm_seek_media(IntPtr p_instance,
                                          [MarshalAs(UnmanagedType.LPStr)] string psz_name,
                                          float f_percentage);

      [DllImport("libvlc")]
      [return: MarshalAs(UnmanagedType.LPStr)]
      public static extern string libvlc_vlm_show_media(IntPtr p_instance,
                                                 [MarshalAs(UnmanagedType.LPStr)] string psz_name);

      [DllImport("libvlc")]
      public static extern float libvlc_vlm_get_media_instance_position(IntPtr p_instance,
                                                             [MarshalAs(UnmanagedType.LPStr)] string psz_name,
                                                             int i_instance);

      [DllImport("libvlc")]
      public static extern int libvlc_vlm_get_media_instance_time(IntPtr p_instance,
                                                       [MarshalAs(UnmanagedType.LPStr)] string psz_name,
                                                       int i_instance);

      [DllImport("libvlc")]
      public static extern int libvlc_vlm_get_media_instance_length(IntPtr p_instance,
                                                         [MarshalAs(UnmanagedType.LPStr)] string psz_name,
                                                         int i_instance);

      [DllImport("libvlc")]
      public static extern int libvlc_vlm_get_media_instance_rate(IntPtr p_instance,
                                                       [MarshalAs(UnmanagedType.LPStr)] string psz_name,
                                                       int i_instance);

      [DllImport("libvlc")]
      public static extern int libvlc_vlm_get_media_instance_title(IntPtr p_instance,
                                                        [MarshalAs(UnmanagedType.LPStr)] string psz_name, int i_instance);

      [DllImport("libvlc")]
      public static extern int libvlc_vlm_get_media_instance_chapter(IntPtr p_instance,
                                                          [MarshalAs(UnmanagedType.LPStr)] string psz_name, int i_instance);

      [DllImport("libvlc")]
      public static extern int libvlc_vlm_get_media_instance_seekable(IntPtr p_instance,
                                                           [MarshalAs(UnmanagedType.LPStr)] string psz_name, int i_instance);

      [DllImport("libvlc")]
      public static extern IntPtr libvlc_vlm_get_event_manager(IntPtr p_instance);


      #endregion
   }
}