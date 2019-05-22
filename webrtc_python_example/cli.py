import argparse
import asyncio
import json
import logging

from aiortc import RTCPeerConnection, RTCSessionDescription
from aiortc.mediastreams import (AudioFrame, AudioStreamTrack, VideoFrame, VideoStreamTrack)

import cv2
import numpy as np


class BGRVideoStreamTrack(VideoStreamTrack):

    def __init__(self, width=320, height=240):
        self.width = width
        self.height = height

    async def recv_bgr(self):
        img_bgr = np.zeros((self.height, self.width, 3), np.uint8)
        img_bgr[:,:] = (0, 0, 0) # (B, G, R)
        return img_bgr

    async def recv_yuv(self):
        img_bgr = await self.recv_bgr()
        img_yuv = cv2.cvtColor(img_bgr, cv2.COLOR_BGR2YUV_YV12)
        return img_yuv

    async def recv(self):
        await asyncio.sleep(1)
        img_yuv = await self.recv_yuv()
        print(img_yuv.shape)
        img_yuv_bytes = img_yuv.tobytes()
        print(len(img_yuv_bytes))
        print(img_yuv_bytes)
        return VideoFrame(width=self.width, height=self.height, data=img_yuv_bytes)


class BlueVideoStreamTrack(BGRVideoStreamTrack):

    async def recv_bgr(self):
        img_bgr = np.zeros((self.height, self.width, 3), np.uint8)
        img_bgr[:,:] = (255, 0, 0) # (B, G, R)
        return img_bgr


class GreenVideoStreamTrack(BGRVideoStreamTrack):

    async def recv_bgr(self):
        img_bgr = np.zeros((self.height, self.width, 3), np.uint8)
        img_bgr[:,:] = (0, 255, 0) # (B, G, R)
        return img_bgr


class RedVideoStreamTrack(BGRVideoStreamTrack):

    async def recv_bgr(self):
        img_bgr = np.zeros((self.height, self.width, 3), np.uint8)
        img_bgr[:,:] = (0, 0, 255) # (B, G, R)
        return img_bgr


class CombinedVideoStreamTrack(BGRVideoStreamTrack):

    def __init__(self, tracks, width=320, height=240):
        for track in tracks:
            assert(track.width == width)
            assert(track.height == height)
        self.width = width * len(tracks)
        self.height = height
        self.tracks = tracks

    async def recv_bgr(self):
        img_bgrs = [await track.recv_bgr() for track in self.tracks]
        img_bgr = np.hstack(img_bgrs)
        return img_bgr


async def consume_audio(track):
    while True:
        await track.recv()


async def consume_video(track):
    while True:
        try:
            frame = await track.recv()
            num_expected_bytes = int(frame.width * frame.height * 3/2)
            img_yuv_bytes = frame.data[0:num_expected_bytes]
            img_yuv_flat = np.frombuffer(img_yuv_bytes, np.uint8)
            img_yuv = img_yuv_flat.reshape((int(frame.height*3/2), frame.width))
            img_bgr = cv2.cvtColor(img_yuv, cv2.COLOR_YUV2BGR_YV12)
            #cv2.imwrite('image.png',img_bgr)
        except Exception as e:
            print(e)

def channel_log(channel, t, message):
    print('channel(%s) %s %s' % (channel.label, t, message))


def channel_watch(channel):
    @channel.on('message')
    def on_message(message):
        channel_log(channel, '<', message)


def create_pc():
    pc = RTCPeerConnection()

    @pc.on('datachannel')
    def on_datachannel(channel):
        channel_log(channel, '-', 'created by remote party')
        channel_watch(channel)

    return pc


async def run_answer(pc):
    done = asyncio.Event()

    _consumers = []

    @pc.on('datachannel')
    def on_datachannel(channel):
        @channel.on('message')
        def on_message(message):
            # reply
            message = 'pong'
            channel_log(channel, '>', message)
            channel.send(message)

            # quit
            #done.set()

    @pc.on('track')
    def on_track(track):
        print("on_track")
        if track.kind == 'audio':
            _consumers.append(asyncio.ensure_future(consume_audio(track)))
        elif track.kind == 'video':
            _consumers.append(asyncio.ensure_future(consume_video(track)))

    # receive offer
    print('-- Please enter remote offer --')
    try:
        offer_json = json.loads(input())
    except:
        with open('offer.json', 'r') as f:
            offer_json = json.loads(f.read())
    await pc.setRemoteDescription(RTCSessionDescription(
        sdp=offer_json['sdp'],
        type=offer_json['type']))
    print()

    # send answer
    await pc.setLocalDescription(await pc.createAnswer())
    answer = pc.localDescription
    print('-- Your answer --')
    print(json.dumps({
        'sdp': answer.sdp,
        'type': answer.type
    }))
    print()
    with open('answer.json', 'w') as f:
        f.write(json.dumps({
            'sdp': answer.sdp,
            'type': answer.type
        }))

    await done.wait()

    for c in _consumers:
        c.cancel()


async def run_offer(pc):
    done = asyncio.Event()

    channel = pc.createDataChannel('chat')
    channel_log(channel, '-', 'created by local party')
    channel_watch(channel)

    # add video track
    local_video_red = RedVideoStreamTrack()
    local_video_green = GreenVideoStreamTrack()
    local_video_blue = BlueVideoStreamTrack()
    local_video = CombinedVideoStreamTrack([local_video_red, local_video_green, local_video_blue])
    pc.addTrack(local_video)

    @channel.on('message')
    def on_message(message):
        # quit
        #done.set()
        pass

    # send offer
    await pc.setLocalDescription(await pc.createOffer())
    offer = pc.localDescription
    print('-- Your offer --')
    print(json.dumps({
        'sdp': offer.sdp,
        'type': offer.type
    }))
    print()
    with open('offer.json', 'w') as f:
        f.write(json.dumps({
            'sdp': offer.sdp,
            'type': offer.type
        }))

    # receive answer
    print('-- Please enter remote answer --')
    try:
        answer_json = json.loads(input())
    except:
        with open('answer.json', 'r') as f:
            answer_json = json.loads(f.read())
    await pc.setRemoteDescription(RTCSessionDescription(
        sdp=answer_json['sdp'],
        type=answer_json['type']))
    print()

    #await done.wait()
    while True:
        # send message
        message = 'ping'
        channel_log(channel, '>', message)
        channel.send(message)
        # sleep
        await asyncio.sleep(1)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='Data channels with copy-and-paste signaling')
    parser.add_argument('role', choices=['offer', 'answer'])
    parser.add_argument('--verbose', '-v', action='count')
    args = parser.parse_args()

    if args.verbose:
        logging.basicConfig(level=logging.DEBUG)

    pc = create_pc()
    if args.role == 'offer':
        coro = run_offer(pc)
    else:
        coro = run_answer(pc)

    # run event loop
    loop = asyncio.get_event_loop()
    try:
        loop.run_until_complete(coro)
    except KeyboardInterrupt:
        pass
    finally:
        loop.run_until_complete(pc.close())
