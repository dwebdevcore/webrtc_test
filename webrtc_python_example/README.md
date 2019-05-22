# Data channel CLI

This example illustrates the establishment of a data channel and video stream using an RTCPeerConnection and a "copy and paste" signaling channel to exchange SDP. The offer and answer are also output as JSON files in the current directory.

To run the example, you will need instances of the `cli` example:

- The first takes on the role of the offerer. It generates an offer which you
  must copy-and-paste to the answerer.

```
python cli.py offer
```

- The second takes on the role of the answerer. When given an offer, it will
  generate an answer which you must copy-and-paste to the offerer.

```
python cli.py answer
```

# Installation

In addition to aiortc's Python dependencies you need a couple of libraries
installed on your system for media codecs.

On Debian/Ubuntu run:

```
apt install libopus-dev libvpx-dev
```

On OS X run:

```
brew install opus libvpx
```
