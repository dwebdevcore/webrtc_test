#if UNITY_WEBGL
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Byn.Media.Browser
{
    public class BrowserCallFactory : ICallFactory
    {
        public bool CanSelectVideoDevice()
        {
            return false;
        }

        public ICall Create(NetworkConfig config = null)
        {
            return new BrowserWebRtcCall(config);
        }
        public IMediaNetwork CreateMediaNetwork(NetworkConfig config)
        {
            return new BrowserMediaNetwork(config);
        }

        public void Dispose()
        {

        }

        public string[] GetVideoDevices()
        {
            return null;
        }
    }
}
#endif