using System;
using System.Runtime.InteropServices;

namespace Byn.Media.Ios
{
	public static class IosHelper
	{
		
		[DllImport("__Internal", EntryPoint="UnitySetAudioSessionActive")]
		private extern static void UnitySetAudioSessionActive(bool active);

		/// <summary>
		/// On iOS unity Audio is being turned off due to changes on
		/// the native
		/// [AVAudioSession sharedInstance]
		/// 
		/// This function can be used to turn unity audio on again. All sources
		/// will be set to IsPlaying = false and need to be started again after
		/// this call.
		/// 
		/// Only use this function after all voice calls ended. If not
		/// the microphone / audio might stop working.
		/// </summary>
		public static void UnitySetAudioSessionActive()
		{
			UnitySetAudioSessionActive (true);
		}
	}
}

