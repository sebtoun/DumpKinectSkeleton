# Kinect Skeleton Joint Data Dump #

## Description

Dumps joints data from the first kinect skeleton found int the following format:

```Timestamp, JointID, Position.X, Position.Y, Position.Z, Orientation.X, Orientation.Y, Orientation.Z, Orientation.W, State```

`State` being:

* `0` if joint is not tracked
* `1` if joint is inferred
* `2` if joint is tracked

## Requirements

You will need .Net framework 4.5 (available here : http://www.microsoft.com/en-us/download/details.aspx?id=30653)

## Usage

First argument is output file, defaults to "kinect_output.csv"

Press `Q`, `X` or `Control+C` to quit application.

