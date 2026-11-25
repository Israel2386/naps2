using System;
using NAPS2.Images.Bitwise;

namespace NAPS2.Images;

public static class CredentialMerger
{
    public static unsafe IMemoryImage Merge(IMemoryImage baseImage, IMemoryImage overlayImage, byte whiteThreshold = 150)
    {
        // 1. Clonar la hoja base conservando sus dimensiones y DPI exactos
        var canvas = baseImage.Clone();

        using var overlayLock = overlayImage.Lock(LockMode.ReadOnly, out var overlayData);
        using var canvasLock = canvas.Lock(LockMode.ReadWrite, out var canvasData);

        byte* pOverlay = overlayData.ptr;
        byte* pCanvas = canvasData.ptr;

        int overlayBpp = overlayData.bytesPerPixel;
        int canvasBpp = canvasData.bytesPerPixel;

        int limitH = Math.Min(overlayData.h, canvasData.h);
        int limitW = Math.Min(overlayData.w, canvasData.w);

        for (int y = 0; y < canvasData.h; y++)
        {
            int dstY = canvasData.invertY ? (canvasData.h - 1 - y) : y;
            byte* rowCanvas = pCanvas + (dstY * canvasData.stride);

            bool hasOverlayRow = y < limitH;
            byte* rowOverlay = hasOverlayRow
                ? pOverlay + ((overlayData.invertY ? (overlayData.h - 1 - y) : y) * overlayData.stride)
                : null;

            for (int x = 0; x < canvasData.w; x++)
            {
                byte* pxCanvas = rowCanvas + (x * canvasBpp);

                byte r = pxCanvas[canvasData.rOff];
                byte g = pxCanvas[canvasData.gOff];
                byte b = pxCanvas[canvasData.bOff];

                // Si existe un píxel con contenido en la segunda cara, lo estampamos
                if (hasOverlayRow && x < limitW)
                {
                    byte* pxOverlay = rowOverlay + (x * overlayBpp);
                    byte or = pxOverlay[overlayData.rOff];
                    byte og = pxOverlay[overlayData.gOff];
                    byte ob = pxOverlay[overlayData.bOff];

                    int overlayBrightness = (or * 299 + og * 587 + ob * 114) / 1000;
                    if (overlayBrightness < whiteThreshold)
                    {
                        r = or;
                        g = og;
                        b = ob;
                    }
                }

                // Limpiar toda zona invisible al ojo (fondo de tapa de escáner) a blanco puro (255, 255, 255)
                int finalBrightness = (r * 299 + g * 587 + b * 114) / 1000;
                if (finalBrightness >= whiteThreshold)
                {
                    pxCanvas[canvasData.rOff] = 255;
                    pxCanvas[canvasData.gOff] = 255;
                    pxCanvas[canvasData.bOff] = 255;
                }
                else
                {
                    pxCanvas[canvasData.rOff] = r;
                    pxCanvas[canvasData.gOff] = g;
                    pxCanvas[canvasData.bOff] = b;
                }

                if (canvasData.hasAlpha)
                {
                    pxCanvas[canvasData.aOff] = 0xFF;
                }
            }
        }

        return canvas;
    }

    public static unsafe IMemoryImage CreateDebugBoundsImage(IMemoryImage sourceImage)
    {
        var debugImg = sourceImage.Clone();
        using var lockData = debugImg.Lock(LockMode.ReadWrite, out var data);

        int w = data.w;
        int h = data.h;
        byte* ptr = data.ptr;
        int bpp = data.bytesPerPixel;

        int marginX = Math.Min(25, w / 10);
        int marginY = Math.Min(25, h / 10);
        int minDarkPixelsInRow = Math.Max(80, w / 6);
        const byte darknessThreshold = 215;

        int minY = -1, maxY = -1;

        // 1. Buscar borde superior y marcar en AZUL los píxeles que activan la condición
        for (int y = marginY; y < h - marginY; y++)
        {
            int srcY = data.invertY ? (h - 1 - y) : y;
            byte* row = ptr + (srcY * data.stride);
            int darkCount = 0;

            for (int x = marginX; x < w - marginX; x++)
            {
                byte* px = row + (x * bpp);
                byte r = px[data.rOff];
                byte g = px[data.gOff];
                byte b = px[data.bOff];

                if (r < darknessThreshold && g < darknessThreshold && b < darknessThreshold)
                {
                    darkCount++;
                    if (minY == -1)
                    {
                        // Pintar de azul el píxel culpable en las filas superiores
                        px[data.rOff] = 0;
                        px[data.gOff] = 100;
                        px[data.bOff] = 255;
                    }
                    if (darkCount >= minDarkPixelsInRow && minY == -1)
                    {
                        minY = y;
                    }
                }
            }
            if (minY != -1) break;
        }

        // 2. Buscar borde inferior y marcar en VERDE los píxeles que activan la condición
        if (minY != -1)
        {
            for (int y = h - 1 - marginY; y >= minY; y--)
            {
                int srcY = data.invertY ? (h - 1 - y) : y;
                byte* row = ptr + (srcY * data.stride);
                int darkCount = 0;

                for (int x = marginX; x < w - marginX; x++)
                {
                    byte* px = row + (x * bpp);
                    byte r = px[data.rOff];
                    byte g = px[data.gOff];
                    byte b = px[data.bOff];

                    if (r < darknessThreshold && g < darknessThreshold && b < darknessThreshold)
                    {
                        darkCount++;
                        if (maxY == -1)
                        {
                            // Pintar de verde el píxel culpable en las filas inferiores
                            px[data.rOff] = 0;
                            px[data.gOff] = 220;
                            px[data.bOff] = 0;
                        }
                        if (darkCount >= minDarkPixelsInRow && maxY == -1)
                        {
                            maxY = y;
                        }
                    }
                }
                if (maxY != -1) break;
            }
        }

        // 3. Dibujar líneas horizontales ROJAS gruesas (5 px) en minY - 8 y maxY + 8
        void DrawHorizontalRedLine(int targetY)
        {
            for (int offset = -2; offset <= 2; offset++)
            {
                int drawY = Math.Clamp(targetY + offset, 0, h - 1);
                int actualY = data.invertY ? (h - 1 - drawY) : drawY;
                byte* row = ptr + (actualY * data.stride);

                for (int x = 0; x < w; x++)
                {
                    byte* px = row + (x * bpp);
                    px[data.rOff] = 255;
                    px[data.gOff] = 0;
                    px[data.bOff] = 0;
                }
            }
        }

        if (minY != -1 && maxY != -1)
        {
            DrawHorizontalRedLine(Math.Max(0, minY - 8));
            DrawHorizontalRedLine(Math.Min(h - 1, maxY + 8));
        }

        return debugImg;
    }

    public static ProcessedImage CreateCleanTempCredentialImage(
        ProcessedImage srcImg,
        out string tempPath,
        System.Drawing.RectangleF? normalizedRegion = null)
    {
        using var rendered = srcImg.Render();
        using var cleanedMem = normalizedRegion.HasValue
            ? CleanOutsideNormalizedRect(rendered, normalizedRegion.Value)
            : CleanOutsideCredentialBounds(rendered);

        tempPath = Path.Combine(Path.GetTempPath(), $"naps2_print_{Guid.NewGuid():N}.png");
        using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            cleanedMem.Save(fs, ImageFileFormat.Png);
            fs.Flush(true);
        }

        var fileStorage = new ImageFileStorage(tempPath);
        return new ProcessedImage(
            cleanedMem.ImageContext,
            fileStorage,
            srcImg.Metadata,
            new PostProcessingData(),
            TransformState.Empty,
            new RefCount(fileStorage)
        );
    }

    public static unsafe IMemoryImage CleanOutsideCredentialBounds(IMemoryImage sourceImage)
    {
        var cleanImg = sourceImage.Clone();
        using var lockData = cleanImg.Lock(LockMode.ReadWrite, out var data);

        int w = data.w;
        int h = data.h;
        byte* ptr = data.ptr;
        int bpp = data.bytesPerPixel;

        int marginX = Math.Min(25, w / 10);
        int marginY = Math.Min(25, h / 10);
        int minDarkPixelsInRow = Math.Max(20, w / 25);
        const byte darknessThreshold = 220;

        int minY = -1, maxY = -1;

        // 1. Escaneo normal (de arriba hacia abajo)
        for (int y = marginY; y < h - marginY; y++)
        {
            int srcY = data.invertY ? (h - 1 - y) : y;
            byte* row = ptr + (srcY * data.stride);
            int darkCount = 0;

            for (int x = marginX; x < w - marginX; x++)
            {
                byte* px = row + (x * bpp);
                if (px[data.rOff] < darknessThreshold &&
                    px[data.gOff] < darknessThreshold &&
                    px[data.bOff] < darknessThreshold)
                {
                    darkCount++;
                    if (darkCount >= minDarkPixelsInRow)
                    {
                        minY = y;
                        break;
                    }
                }
            }
            if (minY != -1) break;
        }

        if (minY == -1) return cleanImg;

        // 2. Escaneo inverso (de abajo hacia arriba)
        for (int y = h - 1 - marginY; y >= minY; y--)
        {
            int srcY = data.invertY ? (h - 1 - y) : y;
            byte* row = ptr + (srcY * data.stride);
            int darkCount = 0;

            for (int x = marginX; x < w - marginX; x++)
            {
                byte* px = row + (x * bpp);
                if (px[data.rOff] < darknessThreshold &&
                    px[data.gOff] < darknessThreshold &&
                    px[data.bOff] < darknessThreshold)
                {
                    darkCount++;
                    if (darkCount >= minDarkPixelsInRow)
                    {
                        maxY = y;
                        break;
                    }
                }
            }
            if (maxY != -1) break;
        }

        if (maxY == -1) return cleanImg;

        int spanY = Math.Max(1, maxY - minY);
        int minDarkPixelsInCol = Math.Max(15, spanY / 25);
        int minX = -1, maxX = -1;

        // 3. Escaneo de izquierda a derecha (dentro del rango [minY, maxY])
        for (int x = marginX; x < w - marginX; x++)
        {
            int darkCount = 0;
            for (int y = minY; y <= maxY; y++)
            {
                int srcY = data.invertY ? (h - 1 - y) : y;
                byte* px = ptr + (srcY * data.stride) + (x * bpp);
                if (px[data.rOff] < darknessThreshold &&
                    px[data.gOff] < darknessThreshold &&
                    px[data.bOff] < darknessThreshold)
                {
                    darkCount++;
                    if (darkCount >= minDarkPixelsInCol)
                    {
                        minX = x;
                        break;
                    }
                }
            }
            if (minX != -1) break;
        }

        if (minX == -1) minX = marginX;

        // 4. Escaneo de derecha a izquierda (dentro del rango [minY, maxY])
        for (int x = w - 1 - marginX; x >= minX; x--)
        {
            int darkCount = 0;
            for (int y = minY; y <= maxY; y++)
            {
                int srcY = data.invertY ? (h - 1 - y) : y;
                byte* px = ptr + (srcY * data.stride) + (x * bpp);
                if (px[data.rOff] < darknessThreshold &&
                    px[data.gOff] < darknessThreshold &&
                    px[data.bOff] < darknessThreshold)
                {
                    darkCount++;
                    if (darkCount >= minDarkPixelsInCol)
                    {
                        maxX = x;
                        break;
                    }
                }
            }
            if (maxX != -1) break;
        }

        if (maxX == -1) maxX = w - 1 - marginX;

        // 5. Tolerancias amplias arriba, abajo, izquierda y derecha (35 px)
        const int toleranceY = 35;
        const int toleranceX = 35;

        int keepTopY = Math.Max(0, minY - toleranceY);
        int keepBottomY = Math.Min(h - 1, maxY + toleranceY);
        int keepLeftX = Math.Max(0, minX - toleranceX);
        int keepRightX = Math.Min(w - 1, maxX + toleranceX);

        // 6. Blanquear a blanco puro RGB(255,255,255) todo lo que está fuera del rectángulo
        for (int y = 0; y < h; y++)
        {
            int actualY = data.invertY ? (h - 1 - y) : y;
            byte* row = ptr + (actualY * data.stride);

            for (int x = 0; x < w; x++)
            {
                if (y >= keepTopY && y <= keepBottomY && x >= keepLeftX && x <= keepRightX)
                {
                    continue;
                }

                byte* px = row + (x * bpp);
                px[data.rOff] = 255;
                px[data.gOff] = 255;
                px[data.bOff] = 255;
                if (data.hasAlpha)
                {
                    px[data.aOff] = 255;
                }
            }
        }

        return cleanImg;
    }

    public static unsafe (IMemoryImage LeftWithRightPad, IMemoryImage RightWithLeftPad) SplitVerticalWithWhitePadding(IMemoryImage source)
    {
        int w = source.Width;
        int h = source.Height;
        int halfW = w / 2;

        var leftCanvas = source.ImageContext.Create(w, h, source.PixelFormat);
        var rightCanvas = source.ImageContext.Create(w, h, source.PixelFormat);

        using var srcLock = source.Lock(LockMode.ReadOnly, out var srcData);
        using var leftLock = leftCanvas.Lock(LockMode.WriteOnly, out var leftData);
        using var rightLock = rightCanvas.Lock(LockMode.WriteOnly, out var rightData);

        int bpp = srcData.bytesPerPixel;

        for (int y = 0; y < h; y++)
        {
            int srcY = srcData.invertY ? (h - 1 - y) : y;
            int lDstY = leftData.invertY ? (h - 1 - y) : y;
            int rDstY = rightData.invertY ? (h - 1 - y) : y;

            byte* srcRow = srcData.ptr + (srcY * srcData.stride);
            byte* leftRow = leftData.ptr + (lDstY * leftData.stride);
            byte* rightRow = rightData.ptr + (rDstY * rightData.stride);

            // 1. Mitad izquierda -> se copia en la izquierda de leftCanvas, y se rellena en blanco en rightCanvas
            for (int x = 0; x < halfW; x++)
            {
                byte* sPx = srcRow + (x * bpp);
                byte* lPx = leftRow + (x * bpp);
                byte* rPx = rightRow + (x * bpp);

                lPx[leftData.rOff] = sPx[srcData.rOff];
                lPx[leftData.gOff] = sPx[srcData.gOff];
                lPx[leftData.bOff] = sPx[srcData.bOff];
                if (leftData.hasAlpha) lPx[leftData.aOff] = srcData.hasAlpha ? sPx[srcData.aOff] : (byte)255;

                // Cachete izquierdo en blanco total para la mitad derecha
                rPx[rightData.rOff] = 255;
                rPx[rightData.gOff] = 255;
                rPx[rightData.bOff] = 255;
                if (rightData.hasAlpha) rPx[rightData.aOff] = 255;
            }

            // 2. Mitad derecha -> se rellena en blanco en leftCanvas, y se copia en la derecha de rightCanvas
            for (int x = halfW; x < w; x++)
            {
                byte* sPx = srcRow + (x * bpp);
                byte* lPx = leftRow + (x * bpp);
                byte* rPx = rightRow + (x * bpp);

                // Cachete derecho en blanco total para la mitad izquierda
                lPx[leftData.rOff] = 255;
                lPx[leftData.gOff] = 255;
                lPx[leftData.bOff] = 255;
                if (leftData.hasAlpha) lPx[leftData.aOff] = 255;

                rPx[rightData.rOff] = sPx[srcData.rOff];
                rPx[rightData.gOff] = sPx[srcData.gOff];
                rPx[rightData.bOff] = sPx[srcData.bOff];
                if (rightData.hasAlpha) rPx[rightData.aOff] = srcData.hasAlpha ? sPx[srcData.aOff] : (byte)255;
            }
        }

        return (leftCanvas, rightCanvas);
    }

    public static bool IsFullPageRegion(System.Drawing.RectangleF? region)
    {
        if (!region.HasValue) return true;
        var r = region.Value;
        return r.X <= 0.005f && r.Y <= 0.005f && r.Width >= 0.99f && r.Height >= 0.99f;
    }

    public static ProcessedImage CreateMaskedPageTempImage(
        ProcessedImage srcImg,
        System.Drawing.RectangleF normRect,
        out string tempPath)
    {
        using var rendered = srcImg.Render();
        using var maskedMem = CleanOutsideNormalizedRect(rendered, normRect);

        tempPath = Path.Combine(Path.GetTempPath(), $"naps2_crop_print_{Guid.NewGuid():N}.png");
        using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            maskedMem.Save(fs, ImageFileFormat.Png);
            fs.Flush(true);
        }

        var fileStorage = new ImageFileStorage(tempPath);
        return new ProcessedImage(
            maskedMem.ImageContext,
            fileStorage,
            srcImg.Metadata,
            new PostProcessingData(),
            TransformState.Empty,
            new RefCount(fileStorage)
        );
    }

    public static unsafe IMemoryImage CleanOutsideNormalizedRect(IMemoryImage sourceImage, System.Drawing.RectangleF normRect)
    {
        var cleanImg = sourceImage.Clone();
        using var lockData = cleanImg.Lock(LockMode.ReadWrite, out var data);

        int w = data.w;
        int h = data.h;
        byte* ptr = data.ptr;
        int bpp = data.bytesPerPixel;

        int keepLeft = Math.Clamp((int)Math.Round(normRect.Left * w), 0, w - 1);
        int keepRight = Math.Clamp((int)Math.Round(normRect.Right * w), 0, w - 1);
        int keepTop = Math.Clamp((int)Math.Round(normRect.Top * h), 0, h - 1);
        int keepBottom = Math.Clamp((int)Math.Round(normRect.Bottom * h), 0, h - 1);

        for (int y = 0; y < h; y++)
        {
            int actualY = data.invertY ? (h - 1 - y) : y;
            byte* row = ptr + (actualY * data.stride);

            for (int x = 0; x < w; x++)
            {
                if (y >= keepTop && y <= keepBottom && x >= keepLeft && x <= keepRight)
                {
                    continue;
                }

                byte* px = row + (x * bpp);
                px[data.rOff] = 255;
                px[data.gOff] = 255;
                px[data.bOff] = 255;
                if (data.hasAlpha)
                {
                    px[data.aOff] = 255;
                }
            }
        }

        return cleanImg;
    }

    public static unsafe ProcessedImage ApplyOffsetAndCropTempImage(
    ProcessedImage srcImg,
    System.Drawing.PointF contentOffset,
    System.Drawing.RectangleF? normalizedRegion,
    bool autoCleanCredential,
    out string tempPath)
    {
        using var rendered = srcImg.Render();
        IMemoryImage canvasMem;

        if (contentOffset == System.Drawing.PointF.Empty)
        {
            canvasMem = rendered.Clone();
        }
        else
        {
            canvasMem = rendered.ImageContext.Create(rendered.Width, rendered.Height, rendered.PixelFormat);
            canvasMem.SetResolution(rendered.HorizontalResolution, rendered.VerticalResolution);

            using var srcLock = rendered.Lock(LockMode.ReadOnly, out var srcData);
            using var dstLock = canvasMem.Lock(LockMode.WriteOnly, out var dstData);

            int w = dstData.w;
            int h = dstData.h;
            int bpp = dstData.bytesPerPixel;
            int offsetX = (int)Math.Round(contentOffset.X * w);
            int offsetY = (int)Math.Round(contentOffset.Y * h);

            for (int y = 0; y < h; y++)
            {
                int dstActualY = dstData.invertY ? (h - 1 - y) : y;
                byte* dstRow = dstData.ptr + (dstActualY * dstData.stride);

                int srcY = y - offsetY;
                bool validY = srcY >= 0 && srcY < h;
                byte* srcRow = validY
                    ? srcData.ptr + ((srcData.invertY ? (h - 1 - srcY) : srcY) * srcData.stride)
                    : null;

                for (int x = 0; x < w; x++)
                {
                    byte* dstPx = dstRow + (x * bpp);
                    int srcX = x - offsetX;

                    if (validY && srcX >= 0 && srcX < w)
                    {
                        byte* srcPx = srcRow + (srcX * srcData.bytesPerPixel);
                        dstPx[dstData.rOff] = srcPx[srcData.rOff];
                        dstPx[dstData.gOff] = srcPx[srcData.gOff];
                        dstPx[dstData.bOff] = srcPx[srcData.bOff];
                        if (dstData.hasAlpha)
                        {
                            dstPx[dstData.aOff] = srcData.hasAlpha ? srcPx[srcData.aOff] : (byte)255;
                        }
                    }
                    else
                    {
                        dstPx[dstData.rOff] = 255;
                        dstPx[dstData.gOff] = 255;
                        dstPx[dstData.bOff] = 255;
                        if (dstData.hasAlpha)
                        {
                            dstPx[dstData.aOff] = 255;
                        }
                    }
                }
            }
        }

        using (canvasMem)
        {
            using var cleanedMem = normalizedRegion.HasValue
                ? CleanOutsideNormalizedRect(canvasMem, normalizedRegion.Value)
                : (autoCleanCredential ? CleanOutsideCredentialBounds(canvasMem) : canvasMem.Clone());

            tempPath = Path.Combine(Path.GetTempPath(), $"naps2_pan_print_{Guid.NewGuid():N}.png");
            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                cleanedMem.Save(fs, ImageFileFormat.Png);
                fs.Flush(true);
            }

            var fileStorage = new ImageFileStorage(tempPath);
            return new ProcessedImage(
                cleanedMem.ImageContext,
                fileStorage,
                srcImg.Metadata,
                new PostProcessingData(),
                TransformState.Empty,
                new RefCount(fileStorage)
            );
        }
    }

}