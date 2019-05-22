/* 
 * Copyright (C) 2015 Christoph Kutza
 * 
 * Please refer to the LICENSE file for license information
 */

var UnityMediaNetwork =
{
    //function CAPIMediaNetwork_IsAvailable(): boolean
    UnityMediaNetwork_IsAvailable: function () {
        if (typeof CAPIMediaNetwork_IsAvailable === 'function') {
            return CAPIMediaNetwork_IsAvailable();
        }
        return false;
    },
    //function CAPIMediaNetwork_Create(lJsonConfiguration):number
    UnityMediaNetwork_Create: function (lJsonConfiguration) {
        return CAPIMediaNetwork_Create(Pointer_stringify(lJsonConfiguration));
    },
    //function CAPIMediaNetwork_Configure(lIndex:number, audio: boolean, video: boolean, minWidth: number, minHeight: number, maxWidth: number, maxHeight: number, idealWidth: number, idealHeight: number)
    UnityMediaNetwork_Configure: function (lIndex, audio, video,
        minWidth, minHeight,
        maxWidth, maxHeight,
        idealWidth, idealHeight) {
        CAPIMediaNetwork_Configure(lIndex, audio, video, minWidth, minHeight, maxWidth, maxHeight, idealWidth, idealHeight);
    },
    //function CAPIMediaNetwork_GetConfigurationState(lIndex: number): number
    UnityMediaNetwork_GetConfigurationState: function (lIndex) {
        return CAPIMediaNetwork_GetConfigurationState(lIndex);
    },
    //function CAPIMediaNetwork_GetConfigurationError(lIndex: number): string
    UnityMediaNetwork_GetConfigurationError: function (lIndex) {
        //TODO:
        return CAPIMediaNetwork_GetConfigurationError(lIndex);
    },
    //function CAPIMediaNetwork_ResetConfiguration(lIndex: number) : void 
    UnityMediaNetwork_ResetConfiguration: function (lIndex) {
        CAPIMediaNetwork_ResetConfiguration(lIndex);
    },
    //function CAPIMediaNetwork_TryGetFrame(lIndex: number, lConnectionId: number, lWidthInt32Array: Int32Array, lWidthIntArrayIndex: number, lHeightInt32Array: Int32Array, lHeightIntArrayIndex: number, lBufferUint8Array: Uint8Array, lBufferUint8ArrayOffset: number, lBufferUint8ArrayLength: number): boolean
    UnityMediaNetwork_TryGetFrame: function (lIndex, lConnectionId, lWidthInt32ArrayPtr, lHeightInt32ArrayPtr, lBufferUint8ArrayPtr, lBufferUint8ArrayOffset, lBufferUint8ArrayLength) {
        return CAPIMediaNetwork_TryGetFrame(lIndex, lConnectionId,
                                        HEAP32, lWidthInt32ArrayPtr >> 2,
                                        HEAP32, lHeightInt32ArrayPtr >> 2,
                                        HEAPU8, lBufferUint8ArrayPtr + lBufferUint8ArrayOffset, lBufferUint8ArrayLength);
    },
    //function CAPIMediaNetwork_TryGetFrameDataLength(lIndex: number, connectionId: number) : number
    UnityMediaNetwork_TryGetFrameDataLength: function (lIndex, connectionId) {
        return CAPIMediaNetwork_TryGetFrameDataLength(lIndex, connectionId);
    },
    UnityMediaNetwork_TryGetFrameDataLength: function (lIndex, connectionId) {
        return CAPIMediaNetwork_TryGetFrameDataLength(lIndex, connectionId);
    },
    UnityMediaNetwork_SetVolume: function(lIndex, volume, connectionId) {
        CAPIMediaNetwork_SetVolume(lIndex, volume, connectionId);
    },
    UnityMediaNetwork_HasAudioTrack: function(lIndex, connectionId) {
        return CAPIMediaNetwork_HasAudioTrack(lIndex, connectionId);
    },
    UnityMediaNetwork_HasVideoTrack: function(lIndex, connectionId) {
        return CAPIMediaNetwork_HasVideoTrack(lIndex, connectionId);
    }
}
mergeInto(LibraryManager.library, UnityMediaNetwork);
