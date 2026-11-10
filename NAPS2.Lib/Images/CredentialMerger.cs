using System;
using NAPS2.Images.Bitwise;

namespace NAPS2.Images;

public static class CredentialMerger
{
    public static unsafe IMemoryImage Merge(IMemoryImage baseImage, IMemoryImage overlayImage, byte whiteThreshold = 220)
    {
        // 1. Clonar la hoja sobre la que se soltó (queda debajo)
        var canvas = baseImage.Clone();

        // 2. Bloquear ambas para estampar la hoja soltada (overlay) encima
        using var overlayLock = overlayImage.Lock(LockMode.ReadOnly, out var overlayData);
        using var canvasLock = canvas.Lock(LockMode.ReadWrite, out var canvasData);

        byte* pOverlay = overlayData.ptr;
        byte* pCanvas = canvasData.ptr;

        int overlayBpp = overlayData.bytesPerPixel;
        int canvasBpp = canvasData.bytesPerPixel;

        int limitH = Math.Min(overlayData.h, canvasData.h);
        int limitW = Math.Min(overlayData.w, canvasData.w);

        for (int y = 0; y < limitH; y++)
        {
            int srcY = overlayData.invertY ? (overlayData.h - 1 - y) : y;
            int dstY = canvasData.invertY ? (canvasData.h - 1 - y) : y;

            byte* rowOverlay = pOverlay + (srcY * overlayData.stride);
            byte* rowCanvas = pCanvas + (dstY * canvasData.stride);

            for (int x = 0; x < limitW; x++)
            {
                byte* pxOverlay = rowOverlay + (x * overlayBpp);
                byte* pxCanvas = rowCanvas + (x * canvasBpp);

                byte r = pxOverlay[overlayData.rOff];
                byte g = pxOverlay[overlayData.gOff];
                byte b = pxOverlay[overlayData.bOff];

                int brightness = (r * 299 + g * 587 + b * 114) / 1000;

                // Todo pixel útil/oscuro de la hoja que se soltó sobrescribe
                // incondicionalmente lo que esté debajo en el lienzo base
                if (brightness < whiteThreshold)
                {
                    pxCanvas[canvasData.rOff] = r;
                    pxCanvas[canvasData.gOff] = g;
                    pxCanvas[canvasData.bOff] = b;

                    if (canvasData.hasAlpha)
                    {
                        pxCanvas[canvasData.aOff] = 0xFF;
                    }
                }
            }
        }

        return canvas;
    }

    private static unsafe void CopyOmitWhite(
    IMemoryImage src,
    IMemoryImage dst,
    int destX,
    int destY,
    byte threshold,
    int maxTargetY)
    {
        using var srcLock = src.Lock(LockMode.ReadOnly, out var srcData);
        using var dstLock = dst.Lock(LockMode.ReadWrite, out var dstData);

        byte* pSrc = srcData.ptr;
        byte* pDst = dstData.ptr;

        int srcBpp = srcData.bytesPerPixel;
        int dstBpp = dstData.bytesPerPixel;

        for (int y = 0; y < srcData.h; y++)
        {
            int srcY = srcData.invertY ? (srcData.h - 1 - y) : y;
            int dstRowIndex = destY + y;

            // Si sobrepasa la mitad asignada o el fondo del lienzo, detenemos
            if (dstRowIndex >= maxTargetY || dstRowIndex >= dstData.h)
            {
                break;
            }

            byte* rowSrc = pSrc + (srcY * srcData.stride);
            byte* rowDst = pDst + (dstRowIndex * dstData.stride) + (destX * dstBpp);

            for (int x = 0; x < srcData.w; x++)
            {
                if ((destX + x) >= dstData.w)
                {
                    break;
                }

                byte* pixelSrc = rowSrc + (x * srcBpp);
                byte* pixelDst = rowDst + (x * dstBpp);

                byte r = pixelSrc[srcData.rOff];
                byte g = pixelSrc[srcData.gOff];
                byte b = pixelSrc[srcData.bOff];

                int brightness = (r * 299 + g * 587 + b * 114) / 1000;

                // Omisión de píxeles claros/blancos
                if (brightness < threshold)
                {
                    pixelDst[dstData.rOff] = r;
                    pixelDst[dstData.gOff] = g;
                    pixelDst[dstData.bOff] = b;

                    if (dstData.hasAlpha)
                    {
                        pixelDst[dstData.aOff] = 0xFF;
                    }
                }
            }
        }
    }
}