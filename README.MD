# Kinect Skeleton Joint Data Dump #

## Description

Dumps color camera stream and joints data from the first kinect skeleton found.

### Skeleton data

Skeleton data is outputed to `<PREFIX>_body.csv` in commas separated values in the following format:

```Timestamp, JointID, Position.X, Position.Y, Position.Z, Orientation.X, Orientation.Y, Orientation.Z, Orientation.W, State```

`State` being:

* `0` if joint is not tracked
* `1` if joint is inferred
* `2` if joint is tracked

### Video data

Video stream is outputed to `<PREFIX>_color.yuy2` in raw uncompressed YUYV422 (a.k.a. YUY2) format at a 1080p resolution. Beware that the video file grows very fast.


## Requirements

You will need .Net framework 4.5 (available here : http://www.microsoft.com/en-us/download/details.aspx?id=30653)

## Usage

```
Usage: DumpKinectSkeleton [--help] [-v|--video] [-s|--synchronize] [--prefix PREFIX]

  -v, --video          Dump color video stream data as a yuy2 raw format.

  -s, --synchronize    Synchronize streams.

  --prefix=PREFIX      (Default: output) Output files prefix.

  --help               Display this help screen.
```

Press `Q`, `X` or `Control+C` to quit application.
