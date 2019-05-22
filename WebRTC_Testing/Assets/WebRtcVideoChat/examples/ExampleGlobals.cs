using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Byn.Unity.Examples
{
    /// <summary>
    /// Keeps the global urls / data used for the example applications and unit tests
    /// </summary>
    class ExampleGlobals
    {
        /// <summary>
        /// Signaling. ws by default. wss for webgl
        /// </summary>
        public static string Signaling
        {
            get
            {
                string protocol = "ws";
                if(Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    protocol = "wss";
                }
                return protocol + "://signaling.because-why-not.com/test";
            }
        }

        /// <summary>
        /// Signaling for shared addresses (conference calls)
        /// </summary>
        public static string SharedSignaling
        {
            get
            {
                string protocol = "ws";
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    protocol = "wss";
                }
                return protocol + "://signaling.because-why-not.com/testshared";
            }
        }

        /// <summary>
        /// Stun server
        /// </summary>
        public static readonly string StunUrl = "stun:stun.because-why-not.com:443";

        /// <summary>
        /// Turn server
        /// </summary>
        public static readonly string TurnUrl = "turn:turn.because-why-not.com:443";

        /// <summary>
        /// Turn server user (changed if overused)
        /// </summary>
        public static readonly string TurnUser = "testuser14";

        /// <summary>
        /// Turn server password (changed if userused)
        /// </summary>
        public static readonly string TurnPass = "pass14";

        /// <summary>
        /// Backup stun server to keep essentials running during server maintenance
        /// </summary>
        public static readonly string BackupStunUrl = "stun:stun.l.google.com:19302";

    }
}