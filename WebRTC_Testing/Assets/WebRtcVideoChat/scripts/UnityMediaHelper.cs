using UnityEngine;

namespace Byn.Media
{
    public class UnityMediaHelper
    {
        /// <summary>
        /// Will update an existing texture or recreate a texture based 
        /// on the raw frame given and format given.
        /// This call will destroy an existing texture object if the width, 
        /// height or format has changed.
        /// </summary>
        /// <param name="tex">A ref to a Texture2D. Will be created if null</param>
        /// <param name="frame">the raw frame to use to update the texture</param>
        /// <param name="format">Format of the raw frame</param>
        /// <returns>Returns true if a new texture object was created</returns>
        public static bool UpdateTexture(ref Texture2D tex, RawFrame frame, FramePixelFormat format)
        {
            bool newTextureCreated = false;
            //texture exists but has the wrong height /width? -> destroy it and set the value to null
            if (tex != null && (tex.width != frame.Width || tex.height != frame.Height))
            {
                Texture2D.Destroy(tex);
                tex = null;
            }
            //no texture? create a new one first
            if (tex == null)
            {
                newTextureCreated = true;
                //current default format for compatibility reasons
                if (format == FramePixelFormat.ABGR)
                {
                    tex = new Texture2D(frame.Width, frame.Height, TextureFormat.RGBA32, false);
                }
                tex.wrapMode = TextureWrapMode.Clamp;
            }
            //copy image data into the texture and apply
            tex.LoadRawTextureData(frame.Buffer);
            tex.Apply();
            return newTextureCreated;
        }
    }
}
