﻿using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using System;
using Kinect.Toolbox.Record;

namespace Kinect.Toolbox {
  public class ColorStreamManager : Notifier, IStreamManager {
    public WriteableBitmap Bitmap { get; private set; }

    /// <summary>
    /// Each pixel has 4 channels.
    /// </summary>
    public byte[] PixelData { get; private set; }
    int[] yuvTemp;

    static double Clamp(double value) {
      return Math.Max(0, Math.Min(value, 255));
    }

    static int ConvertFromYUV(byte y, byte u, byte v) {
      byte b = (byte)Clamp(1.164 * (y - 16) + 2.018 * (u - 128));

      byte g = (byte)Clamp(1.164 * (y - 16) - 0.813 * (v - 128) - 0.391 * (u - 128));

      byte r = (byte)Clamp(1.164 * (y - 16) + 1.596 * (v - 128));

      return (r << 16) + (g << 8) + b;
    }

    /// <summary>
    /// Updates pixel data and if necessary bitmap as well.
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="updateDisplay">Updates the bitmap if true.</param>
    public void Update(ReplayColorImageFrame frame, bool updateDisplay = true) {
      PixelData = new byte[frame.PixelDataLength];
 
      frame.CopyPixelDataTo(PixelData);

      if (!updateDisplay)
        return;

      if (Bitmap == null) {
        Bitmap = new WriteableBitmap(frame.Width, frame.Height,
                                     96, 96, PixelFormats.Bgr32, null);
      }

      int stride = Bitmap.PixelWidth * Bitmap.Format.BitsPerPixel / 8;
      Int32Rect dirtyRect = new Int32Rect(0, 0, Bitmap.PixelWidth, Bitmap.PixelHeight);

      if (frame.Format == ColorImageFormat.RawYuvResolution640x480Fps15) {
        if (yuvTemp == null)
          yuvTemp = new int[frame.Width * frame.Height];

        int current = 0;

        for (int uyvyIndex = 0; uyvyIndex < PixelData.Length; uyvyIndex += 4) {
          byte u = PixelData[uyvyIndex];
          byte y1 = PixelData[uyvyIndex + 1];
          byte v = PixelData[uyvyIndex + 2];
          byte y2 = PixelData[uyvyIndex + 3];

          yuvTemp[current++] = ConvertFromYUV(y1, u, v);
          yuvTemp[current++] = ConvertFromYUV(y2, u, v);
        }

        Bitmap.WritePixels(dirtyRect, yuvTemp, stride, 0);
      } else {
        Bitmap.WritePixels(dirtyRect, PixelData, stride, 0);
      }

      RaisePropertyChanged(() => Bitmap);
    }
  }
}
