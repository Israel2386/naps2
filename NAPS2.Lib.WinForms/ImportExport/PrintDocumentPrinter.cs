using Eto.Forms;
using NAPS2.Images.Gdi;
using PdfSharpCore.Drawing;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Windows.Forms;
using DialogResult = System.Windows.Forms.DialogResult;
using PrintDialog = System.Windows.Forms.PrintDialog;
using PrintDocument = System.Drawing.Printing.PrintDocument;

namespace NAPS2.ImportExport;

public class PrintDocumentPrinter : IScannedImagePrinter
{
    public async Task<bool> PromptToPrint(
        Window parentWindow,
        IList<ProcessedImage> images,
        IList<ProcessedImage> selectedImages,
        IList<System.Drawing.RectangleF?>? allCropRegions = null,
        IList<System.Drawing.RectangleF?>? selectedCropRegions = null,
        IList<System.Drawing.PointF>? allContentOffsets = null,
        IList<System.Drawing.PointF>? selectedContentOffsets = null)
    {
        if (!images.Any())
        {
            return false;
        }

        var printDialog = new PrintDialog
        {
            AllowSomePages = true,
            AllowSelection = selectedImages.Any(),
            PrinterSettings =
            {
                MinimumPage = 1,
                MaximumPage = images.Count,
                FromPage = 1,
                ToPage = images.Count
            }
        };

        if (printDialog.ShowDialog(parentWindow.ToNative()) == DialogResult.OK)
        {
            return await Print(
                printDialog.PrinterSettings,
                images,
                selectedImages,
                allCropRegions,
                selectedCropRegions,
                allContentOffsets,
                selectedContentOffsets);
        }
        return false;
    }

    public async Task<bool> Print(
         PrinterSettings printerSettings,
         IList<ProcessedImage> images,
         IList<ProcessedImage> selectedImages,
         IList<System.Drawing.RectangleF?>? allCropRegions = null,
         IList<System.Drawing.RectangleF?>? selectedCropRegions = null,
         IList<System.Drawing.PointF>? allContentOffsets = null,
         IList<System.Drawing.PointF>? selectedContentOffsets = null)
    {
        var tempPaths = new List<string>();
        var disposables = new List<ProcessedImage>();

        try
        {
            var preparedAll = ApplyCropRegions(images, allCropRegions, allContentOffsets, tempPaths, disposables);
            var preparedSelected = ApplyCropRegions(selectedImages, selectedCropRegions, selectedContentOffsets, tempPaths, disposables);
            // ... el resto de Print queda idéntico ...

            IList<ProcessedImage> imagesToPrint;

            switch (printerSettings.PrintRange)
            {
                case PrintRange.AllPages:
                    imagesToPrint = preparedAll;
                    break;
                case PrintRange.Selection:
                    imagesToPrint = preparedSelected;
                    break;
                case PrintRange.SomePages:
                    int start = printerSettings.FromPage - 1;
                    int length = printerSettings.ToPage - start;
                    imagesToPrint = preparedAll.Skip(start).Take(length).ToList();
                    break;
                default:
                    imagesToPrint = [];
                    break;
            }
            if (imagesToPrint.Count == 0)
            {
                return false;
            }

            return await Task.Run(() =>
            {
                var printDocument = new PrintDocument();
                int i = 0;
                printDocument.PrintPage += (sender, e) =>
                {
                    var image = imagesToPrint[i].Render();
                    try
                    {
                        var pb = e.PageBounds;
                        if (Math.Sign(image.Width - image.Height) != Math.Sign(pb.Width - pb.Height))
                        {
                            // Flip portrait/landscape to match output
                            image = image.PerformTransform(new RotationTransform(90));
                        }

                        // Escalar manteniendo la relación de aspecto dentro de la hoja completa
                        int drawW, drawH;
                        if (image.Width / (double)pb.Width < image.Height / (double)pb.Height)
                        {
                            drawH = pb.Height;
                            drawW = (int)Math.Round(image.Width * (pb.Height / (double)image.Height));
                        }
                        else
                        {
                            drawW = pb.Width;
                            drawH = (int)Math.Round(image.Height * (pb.Width / (double)image.Width));
                        }

                        // Centrar en la hoja física y compensar el margen físico no imprimible de la impresora
                        float hardMarginX = e.PageSettings.HardMarginX;
                        float hardMarginY = e.PageSettings.HardMarginY;

                        float drawX = pb.Left + (pb.Width - drawW) / 2f - hardMarginX;
                        float drawY = pb.Top + (pb.Height - drawH) / 2f - hardMarginY;

                        e.Graphics!.DrawImage(image.AsBitmap(), drawX, drawY, drawW, drawH);
                    }
                    finally
                    {
                        image.Dispose();
                    }

                    e.HasMorePages = (++i < imagesToPrint.Count);
                };
                printDocument.PrinterSettings = printerSettings;
                printDocument.Print();

                Log.Event(EventType.Print, new EventParams
                {
                    Name = MiscResources.Print,
                    Pages = images.Count,
                    DeviceName = printDocument.PrinterSettings.PrinterName
                });

                return true;
            });
        }
        finally
        {
            foreach (var img in disposables)
            {
                img.Dispose();
            }
            foreach (var path in tempPaths)
            {
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            }
        }
    }
    public async Task<bool> PrintSilent(
    IList<ProcessedImage> images,
    bool highQuality = false,
    IList<System.Drawing.RectangleF?>? cropRegions = null,
    IList<System.Drawing.PointF>? contentOffsets = null,
    IList<System.Drawing.PointF>? selectedContentOffsets = null)
    {
        if (!images.Any())
        {
            return false;
        }

        var settings = new PrinterSettings();

        if (highQuality)
        {
            PrinterResolution? highestRes = settings.PrinterResolutions
                .Cast<PrinterResolution>()
                .FirstOrDefault(r => r.Kind == PrinterResolutionKind.High)
                ?? settings.PrinterResolutions
                    .Cast<PrinterResolution>()
                    .OrderByDescending(r => r.X * r.Y)
                    .FirstOrDefault();

            if (highestRes != null)
            {
                settings.DefaultPageSettings.PrinterResolution = highestRes;
            }

            PaperSource? plainSource = settings.PaperSources
                .Cast<PaperSource>()
                .FirstOrDefault(s =>
                    s.Kind == PaperSourceKind.AutomaticFeed ||
                    s.SourceName.Contains("Normal", StringComparison.OrdinalIgnoreCase) ||
                    s.SourceName.Contains("Plain", StringComparison.OrdinalIgnoreCase));

            if (plainSource != null)
            {
                settings.DefaultPageSettings.PaperSource = plainSource;
            }
        }

        var tempItems = await Task.Run(() =>
        {
            var list = new List<(ProcessedImage Image, string TempPath)>(images.Count);
            for (int i = 0; i < images.Count; i++)
            {
                var rawRegion = (cropRegions != null && i < cropRegions.Count) ? cropRegions[i] : null;
                var effectiveRegion = CredentialMerger.IsFullPageRegion(rawRegion) ? null : rawRegion;
                var offset = (contentOffsets != null && i < contentOffsets.Count) ? contentOffsets[i] : System.Drawing.PointF.Empty;

                var cleanImg = CredentialMerger.ApplyOffsetAndCropTempImage(
                    images[i],
                    offset,
                    effectiveRegion,
                    autoCleanCredential: true,
                    out string tempPath);
                list.Add((cleanImg, tempPath));
            }
            return list;
        });

        var preparedImages = tempItems.Select(x => x.Image).ToList();

        try
        {
            return await Print(settings, preparedImages, preparedImages);
        }
        finally
        {
            foreach (var item in tempItems)
            {
                item.Image.Dispose();
                if (File.Exists(item.TempPath))
                {
                    try { File.Delete(item.TempPath); } catch { }
                }
            }
        }
    }


    private static IList<ProcessedImage> ApplyCropRegions(
        IList<ProcessedImage> sourceImages,
        IList<System.Drawing.RectangleF?>? cropRegions,
        IList<System.Drawing.PointF>? contentOffsets,
        List<string> tempPaths,
        List<ProcessedImage> disposables)
    {
        if (cropRegions == null && contentOffsets == null) return sourceImages;

        var result = new List<ProcessedImage>(sourceImages.Count);
        for (int i = 0; i < sourceImages.Count; i++)
        {
            var rawRegion = (cropRegions != null && i < cropRegions.Count) ? cropRegions[i] : null;
            var effectiveRegion = CredentialMerger.IsFullPageRegion(rawRegion) ? null : rawRegion;
            var offset = (contentOffsets != null && i < contentOffsets.Count) ? contentOffsets[i] : System.Drawing.PointF.Empty;

            if (effectiveRegion.HasValue || offset != System.Drawing.PointF.Empty)
            {
                var transformed = CredentialMerger.ApplyOffsetAndCropTempImage(
                    sourceImages[i],
                    offset,
                    effectiveRegion,
                    autoCleanCredential: false,
                    out var path);
                tempPaths.Add(path);
                disposables.Add(transformed);
                result.Add(transformed);
            }
            else
            {
                result.Add(sourceImages[i]);
            }
        }
        return result;
    }

}